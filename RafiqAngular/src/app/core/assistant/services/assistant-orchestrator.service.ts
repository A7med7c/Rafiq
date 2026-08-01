/**
 * @file assistant-orchestrator.service.ts
 * @description The single coordinator between the UI, Speech, Avatar, Workflow, and future AI layers.
 * Responsibility:
 *   - Exposes a reactive state signal consumed by the UI component (rafiq-assistant).
 *   - Accepts user events from the UI (microphoneToggled, cardClosed, stateSet).
 *   - Delegates speech recognition to SpeechService.
 *   - Delegates command execution to CommandService.
 *   - Delegates workflow execution to WorkflowService.
 *   - Controls avatar animation via AvatarService.
 *   - AI integration will be added here when ready (plug-in point: processUserIntent()).
 */

import { Injectable, inject, signal, computed } from '@angular/core';
import { Subscription } from 'rxjs';
import { AssistantState, AssistantStateContext } from '../models/assistant-state';
import { ToolContext } from '../models/assistant-tool';
import { SpeechService } from './speech.service';
import { AvatarService } from './avatar.service';
import { CommandService } from './command.service';
import { WorkflowService } from './workflow.service';
import { TourEngineService } from './tour-engine.service';
import { AnimationItem } from 'lottie-web';

@Injectable({
  providedIn: 'root',
})
export class AssistantOrchestratorService {
  private readonly speechService = inject(SpeechService);
  readonly avatarService = inject(AvatarService);
  private readonly commandService = inject(CommandService);
  private readonly workflowService = inject(WorkflowService);
  readonly tourEngine = inject(TourEngineService);

  // ── Internal state signals ────────────────────────────────────────────────

  private readonly _state = signal<AssistantState>('idle');
  private readonly _isOpen = signal<boolean>(true);
  private readonly _errorMessage = signal<string | null>(null);
  private readonly _lastRecognizedText = signal<string | null>(null);
  private readonly _activeProfileId = signal<string | null>(null);
  private readonly _activeConversationId = signal<string | null>(null);

  // ── Public read-only signals (consumed by UI) ─────────────────────────────

  readonly state = this._state.asReadonly();
  readonly isOpen = this._isOpen.asReadonly();
  readonly errorMessage = this._errorMessage.asReadonly();
  readonly lastRecognizedText = this._lastRecognizedText.asReadonly();

  /** Derived subtitle text based on current state. */
  readonly subtitleText = computed<string>(() => {
    switch (this._state()) {
      case 'listening': return 'Listening...';
      case 'speaking': return 'Speaking...';
      case 'processing': return 'Processing...';
      case 'error': return this._errorMessage() ?? 'An error occurred.';
      case 'idle':
      default: return 'Idle — Ready to help';
    }
  });

  /** Full context snapshot for the UI to consume. */
  readonly stateContext = computed<AssistantStateContext>(() => ({
    state: this._state(),
    avatarState: this.avatarService.state(),
    title: 'Rafiq Assistant',
    subtitle: this.subtitleText(),
    isOpen: this._isOpen(),
    activeProfileId: this._activeProfileId(),
    activeConversationId: this._activeConversationId(),
    errorMessage: this._errorMessage(),
    lastRecognizedText: this._lastRecognizedText(),
  }));

  private listeningSubscription: Subscription | null = null;
  private animationItem?: AnimationItem;

  // ── Avatar integration ────────────────────────────────────────────────────

  /** Called by the UI component when the Lottie animation is ready. */
  onAnimationCreated(anim: AnimationItem): void {
    this.animationItem = anim;
    this.avatarService.setAssistantState(this._state());
  }

  // ── UI event handlers ─────────────────────────────────────────────────────

  /** Called when the user clicks the microphone button. Toggles listening on/off. */
  onMicrophoneToggled(language: string = 'ar-EG'): void {
    const current = this._state();
    console.log(`[AssistantOrchestrator] [1] Microphone toggled. Current state: "${current}", Language: "${language}"`);

    if (current === 'listening') {
      this.stopListening();
    } else {
      this.startListening(language);
    }
  }

  /** Called when the user closes the assistant card. */
  onCardClosed(): void {
    this.stopListening();
    this.speechService.stopSpeaking();
    this._isOpen.set(false);
  }

  /** Called when the user opens the assistant card. */
  onCardOpened(): void {
    this._isOpen.set(true);
    this._errorMessage.set(null);
    this.transitionTo('idle');
  }

  /** Directly sets the assistant state (for testing or external integrations). */
  setState(state: AssistantState): void {
    this.transitionTo(state);
  }

  /** Sets the active health profile context for tool executions. */
  setActiveProfile(profileId: string | null): void {
    this._activeProfileId.set(profileId);
  }

  /** Sets the active conversation context for AI integrations. */
  setActiveConversation(conversationId: string | null): void {
    this._activeConversationId.set(conversationId);
  }

  // ── Listening lifecycle ───────────────────────────────────────────────────

  private startListening(language: string): void {
    this.transitionTo('listening');
    this._errorMessage.set(null);

    this.listeningSubscription?.unsubscribe();
    this.listeningSubscription = this.speechService.startListening(language).subscribe({
      next: recognizedText => {
        this._lastRecognizedText.set(recognizedText);
        // Delegate recognized speech to the command pipeline
        this.processUserIntent(recognizedText);
      },
      error: err => {
        this._errorMessage.set(err?.message || 'Speech recognition failed.');
        this.transitionTo('error');
      },
      complete: () => {
        if (this._state() === 'listening') {
          this.transitionTo('idle');
        }
      },
    });
  }

  private stopListening(): void {
    this.listeningSubscription?.unsubscribe();
    this.listeningSubscription = null;
    this.speechService.stopListening();
    this.transitionTo('idle');
  }

  // ── State transition ──────────────────────────────────────────────────────

  private transitionTo(state: AssistantState): void {
    this._state.set(state);
    this.avatarService.setAssistantState(state);
  }

  // ── Intent Processing & Command Pipeline Dispatcher ────────────────────────

  /**
   * Forwards recognized text into the CommandService pipeline for intent parsing and tool execution.
   */
  private processUserIntent(recognizedText: string): void {
    if (!recognizedText || !recognizedText.trim()) {
      this.transitionTo('idle');
      return;
    }

    this.transitionTo('processing');

    const context = this.buildToolContext();
    this.commandService.processUserText(recognizedText, context).subscribe({
      next: () => {
        this.transitionTo('idle');
      },
      error: err => {
        console.error('[AssistantOrchestrator] Error processing user intent:', err);
        this.transitionTo('error');
      },
    });
  }

  /** Starts an interactive guided tour by scenario ID. */
  startTour(tourId: string): boolean {
    this.onCardOpened();
    return this.tourEngine.startTour(tourId);
  }

  // ── Tool context builder ──────────────────────────────────────────────────

  /** Builds the ToolContext from current assistant state for tool executions. */
  private buildToolContext(): ToolContext {
    return {
      activeProfileId: this._activeProfileId(),
      activeConversationId: this._activeConversationId(),
      currentRoute: typeof window !== 'undefined' ? window.location.pathname : undefined,
    };
  }
}
