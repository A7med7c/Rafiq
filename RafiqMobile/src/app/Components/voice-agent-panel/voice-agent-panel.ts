import {
  Component, OnDestroy, OnInit, inject, signal, effect, untracked,
  input, output,
} from '@angular/core';
import { CommonModule } from '@angular/common';
import { Router } from '@angular/router';
import { LocalizationService } from '../../Services/localization.service';
import { AiChatService } from '../../Services/ai-chat.service';
import { NotificationService } from '../../Services/notification.service';
import { VoiceAgentService } from '../../Services/voice-agent.service';
import { VoiceCaptureService } from '../../Services/voice-capture.service';
import { VoiceSynthesisService } from '../../Services/voice-synthesis.service';
import { SignalRService, VoiceAgentResponsePayload } from '../../Services/signalr.service';
import { ConversationSummaryDto } from '../../Modles/ai-chat.models';

type VoiceState = 'idle' | 'listening' | 'processing' | 'speaking' | 'error';

// After the first exchange, 8 s of cumulative silence ends the session.
// Short enough to avoid leaving the mic open indefinitely; long enough that
// users checking their calendar or recalling a medication name are not cut off.
const INACTIVITY_MS = 8_000;

// Maximum time to wait for the AI SignalR response before showing a timeout error.
// Covers slow AI processing with multiple tool-call hops (up to 8 iterations).
const PROCESSING_TIMEOUT_MS = 90_000;

@Component({
  selector: 'app-voice-agent-panel',
  standalone: true,
  imports: [CommonModule],
  templateUrl: './voice-agent-panel.html',
  styleUrl: './voice-agent-panel.css',
})
export class VoiceAgentPanel implements OnInit, OnDestroy {
  private readonly voiceAgentSvc  = inject(VoiceAgentService);
  private readonly voiceCapture   = inject(VoiceCaptureService);
  private readonly voiceSynthesis = inject(VoiceSynthesisService);
  private readonly signalr        = inject(SignalRService);
  private readonly aiChatSvc      = inject(AiChatService);
  private readonly notifSvc       = inject(NotificationService);
  private readonly router         = inject(Router);
  protected readonly l10n         = inject(LocalizationService);
  protected readonly t            = this.l10n.t;

  // ── Inputs from parent ──────────────────────────────────────────────────────
  readonly conversationId = input<string | null>(null);
  readonly profileId      = input<string | null>(null);

  // ── Outputs to parent ───────────────────────────────────────────────────────
  readonly conversationCreated = output<ConversationSummaryDto>();
  readonly messageStarted      = output<string>();   // spoken text → parent adds optimistic msg
  readonly responseReceived    = output<string>();   // conversationId → parent reloads history

  // ── Internal state ──────────────────────────────────────────────────────────
  readonly state           = signal<VoiceState>('idle');
  readonly toolHint        = signal<string | null>(null);
  readonly errorMessage    = signal<string | null>(null);
  readonly isSessionActive = signal(false);

  readonly captureSupported   = this.voiceCapture.isSupported;
  readonly synthesisSupported = this.voiceSynthesis.isSupported;

  private _activeConversationId: string | null = null;

  // Inactivity tracking — timer only activates after the first completed exchange
  // so the initial "waiting for the user to speak" phase has no timeout.
  private _hasHadExchange   = false;
  private _inactivityTimer:  ReturnType<typeof setTimeout> | null = null;
  // Safety net: if the SignalR response never arrives (network drop, hub failure),
  // this timer fires and surfaces an error instead of leaving the UI stuck.
  private _processingTimer: ReturnType<typeof setTimeout> | null = null;

  constructor() {
    // Keep local conversation id in sync with parent selection.
    effect(() => {
      const id = this.conversationId();
      if (id) this._activeConversationId = id;
    });

    // Thinking event → update tool-call hint label
    effect(() => {
      const events = this.signalr.voiceThinkingEvents();
      if (!events.length) return;
      const drained = this.signalr.drainVoiceThinkingEvents();
      if (untracked(() => this.state()) === 'processing') {
        const latest = drained.at(-1);
        if (latest) this.toolHint.set(latest.toolName);
      }
    });

    // Response event → speak then decide whether to restart the loop
    effect(() => {
      const events = this.signalr.voiceResponseEvents();
      if (!events.length) return;
      const drained = this.signalr.drainVoiceResponseEvents();
      if (untracked(() => this.state()) === 'processing') {
        const latest = drained.at(-1);
        if (latest) void this.onAgentResponse(latest);
      }
    });

    // Error event → surface error and end session
    effect(() => {
      const events = this.signalr.voiceErrorEvents();
      if (!events.length) return;
      const drained = this.signalr.drainVoiceErrorEvents();
      if (untracked(() => this.state()) === 'processing') {
        const msg = drained.at(-1)?.message ?? untracked(() => this.t().voiceAgent.sessionError);
        this.clearProcessingTimer();
        this.clearInactivityTimer();
        this._hasHadExchange = false;
        this.errorMessage.set(msg);
        this.isSessionActive.set(false);
        this.state.set('error');
      }
    });
  }

  ngOnInit(): void {
    // Automatically start the voice session when the panel opens
    setTimeout(() => {
      if (!this.isSessionActive()) {
        this.startSession();
      }
    }, 300); // small delay to let UI animations finish
  }

  ngOnDestroy(): void {
    this.clearInactivityTimer();
    this.clearProcessingTimer();
    this.isSessionActive.set(false);
    this.voiceCapture.stop();
    this.voiceSynthesis.stop();
  }

  // ── Session control ─────────────────────────────────────────────────────────

  startSession(): void {
    if (!this.captureSupported) return;
    this._hasHadExchange = false;
    this.isSessionActive.set(true);
    void this.startListening();
  }

  stopSession(): void {
    this._hasHadExchange = false;
    this.clearInactivityTimer();
    this.isSessionActive.set(false);
    this.voiceCapture.stop();
    this.voiceSynthesis.stop();
    this.state.set('idle');
  }

  // ── Listening loop ──────────────────────────────────────────────────────────

  async startListening(): Promise<void> {
    if (!this.captureSupported || !this.isSessionActive()) return;

    this.state.set('listening');
    this.toolHint.set(null);
    this.errorMessage.set(null);

    // Begin counting inactivity only after the first exchange has completed.
    // The timer is NOT reset on consecutive no-speech retries — only when a
    // new exchange succeeds (see onAgentResponse). This means 90 s of cumulative
    // silence ends the session, regardless of how many silent cycles occur.
    this.armInactivityTimer();

    let text: string;
    try {
      text = await this.voiceCapture.captureOnce(this.l10n.lang());
    } catch (err: any) {
      if (err.message === 'not-allowed') {
        this.clearInactivityTimer();
        this._hasHadExchange = false;
        this.errorMessage.set(this.t().voiceAgent.micDenied);
        this.isSessionActive.set(false);
        this.state.set('error');
      } else if (err.message === 'aborted') {
        // Triggered by stopSession() → voiceCapture.stop(). isSessionActive is
        // already false, so just land in idle.
        if (!this.isSessionActive()) {
          this.state.set('idle');
        }
      } else {
        // 'no-speech' or any transient error — keep the inactivity timer running
        // and retry after a brief pause. If the timer fires during retries, it
        // will call stopSession() on the next tick.
        if (this.isSessionActive()) {
          await new Promise<void>(r => setTimeout(r, 300));
          void this.startListening();
        } else {
          this.state.set('idle');
        }
      }
      return;
    }

    // Speech was captured — the user is active. Cancel the inactivity timer so
    // it does not fire while the AI is processing or speaking the response.
    this.clearInactivityTimer();

    if (!text) {
      // Empty transcript (browser quirk) — restart if session is still running.
      if (this.isSessionActive()) {
        void this.startListening();
      } else {
        this.state.set('idle');
      }
      return;
    }

    this.state.set('processing');
    this.messageStarted.emit(text);
    this.armProcessingTimer();

    const convId = await this.ensureConversation(text);
    if (!convId) {
      this.clearProcessingTimer();
      this.errorMessage.set(this.t().voiceAgent.sessionError);
      this._hasHadExchange = false;
      this.isSessionActive.set(false);
      this.state.set('error');
      return;
    }

    this.voiceAgentSvc.sendMessageStream(convId, text, this.l10n.lang()).subscribe({
      error: () => {
        this.clearProcessingTimer();
        this.clearInactivityTimer();
        this._hasHadExchange = false;
        this.errorMessage.set(this.t().voiceAgent.sessionError);
        this.isSessionActive.set(false);
        this.state.set('error');
      },
    });
  }

  retrySession(): void {
    this._hasHadExchange = false;
    this.clearInactivityTimer();
    this.toolHint.set(null);
    this.errorMessage.set(null);
    this.isSessionActive.set(false);
    this.state.set('idle');
  }

  // ── Private helpers ─────────────────────────────────────────────────────────

  // Start the inactivity timer only if an exchange has already occurred and no
  // timer is already running. Calling this on every startListening() retry is
  // intentionally a no-op so the timer accumulates across silent cycles.
  private armInactivityTimer(): void {
    if (!this._hasHadExchange || this._inactivityTimer != null) return;
    this._inactivityTimer = setTimeout(() => {
      this._inactivityTimer = null;
      if (this.isSessionActive()) {
        this.stopSession();
      }
    }, INACTIVITY_MS);
  }

  private clearInactivityTimer(): void {
    if (this._inactivityTimer != null) {
      clearTimeout(this._inactivityTimer);
      this._inactivityTimer = null;
    }
  }

  private armProcessingTimer(): void {
    this.clearProcessingTimer();
    this._processingTimer = setTimeout(() => {
      this._processingTimer = null;
      if (this.state() === 'processing') {
        this.clearInactivityTimer();
        this._hasHadExchange = false;
        this.errorMessage.set(this.t().voiceAgent.sessionError);
        this.isSessionActive.set(false);
        this.state.set('error');
      }
    }, PROCESSING_TIMEOUT_MS);
  }

  private clearProcessingTimer(): void {
    if (this._processingTimer != null) {
      clearTimeout(this._processingTimer);
      this._processingTimer = null;
    }
  }

  private async ensureConversation(firstMessage: string): Promise<string | null> {
    const parentId = this.conversationId();
    if (parentId) {
      this._activeConversationId = parentId;
      return parentId;
    }

    if (this._activeConversationId) {
      return this._activeConversationId;
    }

    const profileId = this.profileId();
    if (!profileId) return null;

    const raw   = firstMessage.trim();
    const title = raw.length > 40 ? `${raw.slice(0, 40)}…` : raw;

    return new Promise<string | null>(resolve => {
      this.aiChatSvc.createConversation({ userHealthProfileId: profileId, title }).subscribe({
        next: res => {
          const newId = res.data;
          if (!newId) { resolve(null); return; }

          this._activeConversationId = newId;
          this.conversationCreated.emit({
            id: newId,
            userHealthProfileId: profileId,
            title,
            lastMessageAt: null,
            createdAt: new Date().toISOString(),
          });
          resolve(newId);
        },
        error: () => resolve(null),
      });
    });
  }

  private async onAgentResponse(payload: VoiceAgentResponsePayload): Promise<void> {
    this.clearProcessingTimer();
    this.toolHint.set(null);
    this.state.set('speaking');

    this.notifSvc.notifyAppointmentChanged();
    this.notifSvc.notifyReminderChanged();

    if (payload.navigateTo) {
      void this.router.navigateByUrl(payload.navigateTo);
    }

    const convId = this._activeConversationId ?? this.conversationId();
    if (convId) {
      this.responseReceived.emit(convId);
    }

    await this.voiceSynthesis.speak(payload.text);

    // Mark that at least one exchange has now completed. The inactivity timer
    // will arm itself on the next startListening() call.
    this._hasHadExchange = true;
    // Clear any leftover timer so the fresh 90 s window starts cleanly from
    // the moment listening resumes — not from mid-TTS.
    this.clearInactivityTimer();

    if (this.isSessionActive()) {
      void this.startListening();
    } else {
      this.state.set('idle');
    }
  }
}
