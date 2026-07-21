import { Component, OnInit, OnDestroy, inject, signal, effect, untracked } from '@angular/core';
import { CommonModule } from '@angular/common';
import { Router } from '@angular/router';
import { catchError, of } from 'rxjs';
import { LocalizationService } from '../../Services/localization.service';
import { HealthProfileService } from '../../Services/health-profile.service';
import { NotificationService } from '../../Services/notification.service';
import { VoiceAgentService, VoiceAgentResponseDto } from '../../Services/voice-agent.service';
import { VoiceCaptureService } from '../../Services/voice-capture.service';
import { VoiceSynthesisService } from '../../Services/voice-synthesis.service';
import { SignalRService, VoiceAgentResponsePayload } from '../../Services/signalr.service';

type VoiceState = 'init' | 'idle' | 'listening' | 'processing' | 'speaking' | 'error';

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
  private readonly healthProfile  = inject(HealthProfileService);
  private readonly notifSvc       = inject(NotificationService);
  private readonly router         = inject(Router);
  protected readonly l10n         = inject(LocalizationService);
  protected readonly t            = this.l10n.t;

  readonly state        = signal<VoiceState>('init');
  readonly sessionId    = signal<string | null>(null);
  readonly transcript   = signal<string>('');
  readonly response     = signal<string>('');
  readonly toolHint     = signal<string | null>(null);
  readonly errorMessage = signal<string | null>(null);

  readonly captureSupported  = this.voiceCapture.isSupported;
  readonly synthesisSupported = this.voiceSynthesis.isSupported;

  constructor() {
    // Thinking event → update tool hint label
    effect(() => {
      const events = this.signalr.voiceThinkingEvents();
      if (!events.length) return;
      const drained = this.signalr.drainVoiceThinkingEvents();
      const currentState = untracked(() => this.state());
      if (currentState === 'processing') {
        const latest = drained.at(-1);
        if (latest) this.toolHint.set(latest.toolName);
      }
    });

    // Response event → speak and transition to idle
    effect(() => {
      const events = this.signalr.voiceResponseEvents();
      if (!events.length) return;
      const drained = this.signalr.drainVoiceResponseEvents();
      const currentState = untracked(() => this.state());
      if (currentState === 'processing') {
        const latest = drained.at(-1);
        if (latest) void this.onAgentResponse(latest);
      }
    });

    // Error event → show error state
    effect(() => {
      const events = this.signalr.voiceErrorEvents();
      if (!events.length) return;
      const drained = this.signalr.drainVoiceErrorEvents();
      const currentState = untracked(() => this.state());
      if (currentState === 'processing') {
        const msg = drained.at(-1)?.message ?? untracked(() => this.t().voiceAgent.sessionError);
        this.errorMessage.set(msg);
        this.state.set('error');
      }
    });
  }

  ngOnInit(): void {
    this.initSession();
  }

  ngOnDestroy(): void {
    this.voiceCapture.stop();
    this.voiceSynthesis.stop();
  }

  private initSession(): void {
    this.state.set('init');

    this.healthProfile.getMyProfile().pipe(catchError(() => of(null))).subscribe(res => {
      const profileId = res?.data?.id ?? null;
      if (!profileId) {
        this.errorMessage.set(this.t().voiceAgent.sessionError);
        this.state.set('error');
        return;
      }

      this.voiceAgentSvc.createSession(profileId, this.l10n.lang()).subscribe({
        next: r => {
          if (r.data) {
            this.sessionId.set(r.data);
            this.state.set('idle');
          } else {
            this.errorMessage.set(this.t().voiceAgent.sessionError);
            this.state.set('error');
          }
        },
        error: () => {
          this.errorMessage.set(this.t().voiceAgent.sessionError);
          this.state.set('error');
        },
      });
    });
  }

  async startListening(): Promise<void> {
    const sid = this.sessionId();
    if (!sid || !this.captureSupported) return;

    this.state.set('listening');
    this.transcript.set('');
    this.response.set('');
    this.toolHint.set(null);
    this.errorMessage.set(null);

    let text: string;
    try {
      text = await this.voiceCapture.captureOnce(this.l10n.lang());
    } catch (err: any) {
      if (err.message === 'not-allowed') {
        this.errorMessage.set(this.t().voiceAgent.micDenied);
      } else if (err.message === 'aborted') {
        // stopListening() was called by user — stay idle
        return;
      } else {
        // 'no-speech', network, etc. — just go back to idle
        this.state.set('idle');
        return;
      }
      this.state.set('error');
      return;
    }

    if (!text) {
      this.state.set('idle');
      return;
    }

    this.transcript.set(text);
    this.state.set('processing');

    // Fire the streaming call — result arrives via SignalR
    this.voiceAgentSvc.sendMessageStream(sid, text, this.l10n.lang()).subscribe({
      error: () => {
        this.errorMessage.set(this.t().voiceAgent.sessionError);
        this.state.set('error');
      },
    });
  }

  stopListening(): void {
    this.voiceCapture.stop();
    this.state.set('idle');
  }

  retrySession(): void {
    this.sessionId.set(null);
    this.transcript.set('');
    this.response.set('');
    this.toolHint.set(null);
    this.errorMessage.set(null);
    this.initSession();
  }

  private async onAgentResponse(payload: VoiceAgentResponsePayload): Promise<void> {
    this.response.set(payload.text);
    this.toolHint.set(null);
    this.state.set('speaking');

    // Refresh data pages so any changes (appointments, medications) are immediately visible.
    this.notifSvc.notifyAppointmentChanged();
    this.notifSvc.notifyReminderChanged();

    if (payload.navigateTo) {
      void this.router.navigateByUrl(payload.navigateTo);
    }

    // TTS locale is inferred from the response text itself — no lang param needed.
    await this.voiceSynthesis.speak(payload.text);
    this.state.set('idle');
  }
}
