/**
 * @file tour-engine.service.ts
 * @description Central Guided Tour Engine for the Rafiq Assistant.
 * Responsibility: Coordinates AvatarPositionService, AvatarService, SpeechService, NavigateTool, HighlightTool, and Driver.js spotlighting.
 * Drives interactive step-by-step guided scenarios while keeping the Rafiq Assistant robot floating avatar as the tour leader.
 * Supports registering new scenarios at runtime without modifying the engine.
 *
 * Step lifecycle is a strict state machine (see processStepElements / beginStepExit):
 *   stepEnter → resolve anchor ONCE → spotlight fades in → bubble/mascot position alongside it → speech plays
 *   active    → highlight + bubble stay fixed but reactive to resize/scroll
 *   stepExit  → highlight + bubble/mascot fade out together, THEN the next stepEnter begins
 */

import { Injectable, signal, computed, inject, NgZone, effect } from '@angular/core';
import { Router } from '@angular/router';
import { driver, Driver } from 'driver.js';

import { TourScenario, TourStepScenario, TourState, TourStepVariant } from '../models/tour-scenario';
import { DEFAULT_TOURS } from '../scenarios/default-tours';
import { AvatarPositionService } from './avatar-position.service';
import { AvatarService } from './avatar.service';
import { SpeechService } from './speech.service';
import { AssistantAnchorRegistryService } from './assistant-anchor-registry.service';
import { HighlightTool } from '../tools/highlight.tool';
import { NavigateTool } from '../tools/navigate.tool';
import { LocalizationService } from '../../../Services/localization.service';

import { AuthService } from '../../../Services/auth-service';

/** Effective (variant-resolved) content for the step currently being executed. */
interface EffectiveStepContent {
  anchor?: string;
  speech?: string;
  speechParams?: Record<string, any>;
}

/** Time budget for the spotlight to fade in before the bubble/mascot appear alongside it. */
const STEP_ENTER_HIGHLIGHT_MS = 180;
/** Time budget for the spotlight + bubble/mascot to fade out before the next step begins. */
const STEP_EXIT_FADE_MS = 180;

@Injectable({
  providedIn: 'root',
})
export class TourEngineService {
  private readonly router = inject(Router);
  private readonly ngZone = inject(NgZone);
  private readonly positionService = inject(AvatarPositionService);
  private readonly avatarService = inject(AvatarService);
  private readonly speechService = inject(SpeechService);
  private readonly anchorRegistry = inject(AssistantAnchorRegistryService);
  private readonly highlightTool = inject(HighlightTool);
  private readonly navigateTool = inject(NavigateTool);
  private readonly authService = inject(AuthService, { optional: true });
  private readonly l10n = inject(LocalizationService, { optional: true });

  /** Scenario registry mapping scenario ID -> TourScenario */
  private readonly registry = new Map<string, TourScenario>();

  /** Active driver.js instance used exclusively for visual spotlight rendering */
  private activeDriver: Driver | null = null;

  /** Active speech subscription cleanup handle */
  private activeSpeechSub: any = null;

  /** Step execution auto-advance timer handle */
  private autoAdvanceTimer: any = null;

  /** Step-enter / step-exit choreography timer handles */
  private enterTimer: any = null;
  private exitTimer: any = null;

  // ── Signals ────────────────────────────────────────────────────────
  private readonly _currentScenario = signal<TourScenario | null>(null);
  private readonly _currentStepIndex = signal<number>(0);
  private readonly _isPlaying = signal<boolean>(false);
  private readonly _isPaused = signal<boolean>(false);
  private readonly _isCompleted = signal<boolean>(false);
  private readonly _isWaitingForUser = signal<boolean>(false);
  private readonly _isSpeaking = signal<boolean>(false);
  /** True once the current step's highlight+bubble/mascot have fully entered; false while exiting. */
  private readonly _stepVisible = signal<boolean>(false);
  /** Variant-resolved content for the step currently on screen (drives the template). */
  private readonly _effectiveStep = signal<EffectiveStepContent | null>(null);

  readonly currentScenario = this._currentScenario.asReadonly();
  readonly currentStepIndex = this._currentStepIndex.asReadonly();
  readonly isPlaying = this._isPlaying.asReadonly();
  readonly isPaused = this._isPaused.asReadonly();
  readonly isCompleted = this._isCompleted.asReadonly();
  readonly isWaitingForUser = this._isWaitingForUser.asReadonly();
  readonly isSpeaking = this._isSpeaking.asReadonly();
  readonly stepVisible = this._stepVisible.asReadonly();

  readonly totalSteps = computed<number>(() => {
    return this._currentScenario()?.steps.length || 0;
  });

  readonly currentStep = computed<TourStepScenario | null>(() => {
    const scenario = this._currentScenario();
    const index = this._currentStepIndex();
    if (!scenario || index < 0 || index >= scenario.steps.length) {
      return null;
    }
    return scenario.steps[index];
  });

  /** Whether the step currently on screen is anchored to a real element (vs. the centered intro). */
  readonly currentStepHasAnchor = computed<boolean>(() => !!this._effectiveStep()?.anchor);

  /**
   * Returns the current step's speech text with all {{placeholders}} resolved, using the
   * variant-resolved (populated/empty) content actually being spoken for this step.
   * Use this in templates instead of currentStep()?.speech to show the real userName etc.
   */
  readonly currentStepSpeechResolved = computed<string>(() => {
    const effective = this._effectiveStep();
    if (!effective?.speech) return '';
    return this.resolveLocalizedText(effective.speech, effective.speechParams);
  });

  readonly state = computed<TourState>(() => ({
    scenario: this._currentScenario(),
    currentStepIndex: this._currentStepIndex(),
    currentStep: this.currentStep(),
    totalSteps: this.totalSteps(),
    isPlaying: this._isPlaying(),
    isPaused: this._isPaused(),
    isCompleted: this._isCompleted(),
    isWaitingForUser: this._isWaitingForUser(),
  }));

  constructor() {
    // Auto-register built-in default tours
    DEFAULT_TOURS.forEach(scenario => this.registerScenario(scenario));

    // Nice-to-have: lets global CSS pulse the Driver.js spotlight in sync with the mascot's
    // speaking state, so the user's eye is drawn to both the highlight and the bubble at once.
    effect(() => {
      const speaking = this._isSpeaking();
      if (typeof document === 'undefined') return;
      document.body.classList.toggle('rafiq-tour-speaking', speaking);
    });
  }

  // ── Scenario Registry Methods ───────────────────────────────────────

  /** Registers a new tour scenario into the engine. */
  registerScenario(scenario: TourScenario): void {
    if (!scenario || !scenario.id) {
      console.warn('[TourEngineService] Attempted to register invalid scenario');
      return;
    }
    this.registry.set(scenario.id, scenario);
  }

  /** Unregisters a tour scenario by ID. */
  unregisterScenario(scenarioId: string): void {
    this.registry.delete(scenarioId);
  }

  /** Returns a registered scenario by ID. */
  getScenario(scenarioId: string): TourScenario | undefined {
    return this.registry.get(scenarioId);
  }

  /** Returns all registered tour scenarios. */
  getRegisteredScenarios(): TourScenario[] {
    return Array.from(this.registry.values());
  }

  // ── Tour Execution Lifecycle Methods ───────────────────────────────

  /**
   * Starts executing a tour scenario by ID or TourScenario object.
   * @param scenarioOrId TourScenario object or registered scenario ID string
   */
  startTour(scenarioOrId: string | TourScenario): boolean {
    const scenario = typeof scenarioOrId === 'string'
      ? this.getScenario(scenarioOrId)
      : scenarioOrId;

    if (!scenario || !scenario.steps || scenario.steps.length === 0) {
      console.warn(`[TourEngineService] Tour scenario not found or has no steps: "${scenarioOrId}"`);
      return false;
    }

    // Claim the playing flag FIRST so isPlaying() is never false during the
    // teardown-and-restart sequence. This closes the window where external
    // guards (e.g. VoiceSynthesisService) would let a second TTS engine fire.
    this._isPlaying.set(true);
    this._isPaused.set(false);
    this._isCompleted.set(false);
    this._isWaitingForUser.set(false);
    this._isSpeaking.set(false);
    this._stepVisible.set(false);
    this._effectiveStep.set(null);

    if (typeof document !== 'undefined') {
      document.body.classList.add('rafiq-tour-active');
    }

    // Now clean up any in-progress tour resources (synthesizer, timers, driver).
    // clearStepResources() calls stopSpeaking() which kills browser + Azure TTS.
    this.clearAllTimers();
    this.clearStepResources();
    this.clearDriverHighlight();

    this._currentScenario.set(scenario);
    this._currentStepIndex.set(0);

    this.positionService.returnHome();
    this.avatarService.setState('idle');

    if (scenario.onStart) {
      try {
        scenario.onStart();
      } catch (err) {
        console.error('[TourEngineService] Error in scenario onStart:', err);
      }
    }

    this.executeCurrentStep();
    return true;
  }

  /** Advances to the next step in the active scenario. */
  nextStep(): void {
    if (!this._isPlaying() || this._isPaused()) return;

    this.beginStepExit(() => {
      const scenario = this._currentScenario();
      const nextIndex = this._currentStepIndex() + 1;

      if (scenario && nextIndex < scenario.steps.length) {
        this._currentStepIndex.set(nextIndex);
        this.executeCurrentStep();
      } else {
        this.completeTour();
      }
    });
  }

  /** Navigates back to the previous step in the active scenario. */
  previousStep(): void {
    if (!this._isPlaying() || this._isPaused()) return;

    const prevIndex = this._currentStepIndex() - 1;
    if (prevIndex >= 0) {
      this.beginStepExit(() => {
        this._currentStepIndex.set(prevIndex);
        this.executeCurrentStep();
      });
    }
  }

  /** Pauses the currently playing tour. */
  pauseTour(): void {
    if (!this._isPlaying()) return;
    this._isPaused.set(true);
    this.speechService.stopSpeaking();
  }

  /** Resumes a paused tour. */
  resumeTour(): void {
    if (!this._isPlaying() || !this._isPaused()) return;
    this._isPaused.set(false);
    this.executeCurrentStep();
  }

  /**
   * Stops and cancels the active tour.
   * @param triggerCancelCallback Whether to invoke scenario.onCancel
   */
  stopTour(triggerCancelCallback: boolean = true): void {
    const scenario = this._currentScenario();
    const wasVisible = this._stepVisible();

    this._stepVisible.set(false);
    this.clearAllTimers();
    this.clearStepResources();

    const finish = () => {
      this.clearDriverHighlight();

      this._isPlaying.set(false);
      this._isPaused.set(false);
      this._isWaitingForUser.set(false);
      this._isSpeaking.set(false);
      this._currentScenario.set(null);
      this._currentStepIndex.set(0);
      this._effectiveStep.set(null);

      if (typeof document !== 'undefined') {
        document.body.classList.remove('rafiq-tour-active');
      }

      // Return assistant to home position and set idle avatar state
      this.positionService.returnHome();
      this.avatarService.setState('idle');

      if (triggerCancelCallback && scenario?.onCancel) {
        try {
          scenario.onCancel();
        } catch (err) {
          console.error('[TourEngineService] Error in scenario onCancel:', err);
        }
      }
    };

    if (wasVisible) {
      this.exitTimer = setTimeout(finish, STEP_EXIT_FADE_MS);
    } else {
      finish();
    }
  }

  /** Called when user completes waiting step condition or manual interaction */
  onUserInteracted(): void {
    if (this._isWaitingForUser()) {
      this._isWaitingForUser.set(false);
      this.nextStep();
    }
  }

  // ── Step Execution Logic ───────────────────────────────────────────

  private executeCurrentStep(): void {
    const step = this.currentStep();
    if (!step) return;

    const runStepLogic = () => {
      // 1. Optional Route Navigation
      if (step.route && typeof window !== 'undefined') {
        const currentUrl = this.router.url.split('?')[0];
        if (currentUrl !== step.route) {
          this.router.navigateByUrl(step.route).then(() => {
            // Give layout a brief moment to render before positioning
            setTimeout(() => this.processStepElements(step), 250);
          });
          return;
        }
      }

      this.processStepElements(step);
    };

    if (step.delayBeforeMs && step.delayBeforeMs > 0) {
      setTimeout(runStepLogic, step.delayBeforeMs);
    } else {
      runStepLogic();
    }
  }

  /**
   * Resolves which content variant applies for this step. If the step declares `variants`,
   * checks whether `populated.anchor` currently exists in the DOM (i.e. the data-driven element
   * is actually rendered) and picks `populated` if so, otherwise falls back to `empty`.
   * Both variants flow through the identical anchor-resolution/highlight/position path below —
   * only WHICH anchor id gets requested changes.
   */
  private resolveEffectiveStep(step: TourStepScenario): EffectiveStepContent {
    if (!step.variants) {
      return { anchor: step.anchor, speech: step.speech, speechParams: step.speechParams };
    }

    const populated: TourStepVariant = step.variants.populated;
    const empty: TourStepVariant = step.variants.empty;
    const populatedExists = !!populated.anchor && !!this.resolveAnchorElement(populated.anchor);
    const variant = populatedExists ? populated : empty;

    return { anchor: variant.anchor, speech: variant.speech, speechParams: variant.speechParams };
  }

  private processStepElements(step: TourStepScenario): void {
    if (!this._isPlaying() || this._isPaused()) return;

    // Ensure any residual audio from the previous step is silenced before we
    // start positioning and speaking for this one.
    this.speechService.stopSpeaking();

    // 2. Custom Step Action
    if (step.action) {
      try {
        step.action();
      } catch (err) {
        console.error('[TourEngineService] Error in step action:', err);
      }
    }

    // 3. Resolve state-aware content (populated/empty) THEN resolve the anchor exactly once.
    // Both the spotlight and the bubble/mascot group share this single resolved element.
    const effective = this.resolveEffectiveStep(step);
    this._effectiveStep.set(effective);

    // A fixedPosition step is always unanchored: no element is resolved, no spotlight is ever
    // drawn, and the bubble+mascot pin to the same constant viewport point every time.
    const targetEl = (step.fixedPosition || step.fixedPositionFromBottom) ? null : effective.anchor ? this.resolveAnchorElement(effective.anchor) : null;

    if (targetEl && typeof window !== 'undefined') {
      try {
        targetEl.scrollIntoView({ behavior: 'smooth', block: 'center', inline: 'nearest' });
      } catch {
        // Ignore scroll errors
      }
    }

    const avatarState = step.avatarState || (effective.speech ? 'speaking' : 'idle');

    // ── stepEnter choreography ──────────────────────────────────────
    // 1) spotlight fades in (small settle window also covers the scrollIntoView animation)
    // 2) bubble+mascot position alongside it (never before it)
    // 3) THEN speech starts
    if (this.enterTimer) {
      clearTimeout(this.enterTimer);
      this.enterTimer = null;
    }

    const enterDelay = targetEl ? STEP_ENTER_HIGHLIGHT_MS : 100;
    this.enterTimer = setTimeout(() => {
      if (!this._isPlaying() || this._isPaused()) return;

      if (targetEl && step.highlight && !step.fixedPosition && !step.fixedPositionFromBottom) {
        const config = typeof step.highlight === 'object' ? step.highlight : undefined;
        this.highlightElementWithDriver(targetEl, config);
      } else {
        this.clearDriverHighlight();
      }

      const positioned = step.fixedPositionFromBottom
        ? this.positionService.positionFixedFromBottom(step.fixedPositionFromBottom.x, step.fixedPositionFromBottom.yFromBottom)
        : step.fixedPosition
          ? this.positionService.positionFixed(step.fixedPosition.x, step.fixedPosition.y)
          : targetEl
            ? this.positionService.positionToAnchor(targetEl)
            : this.positionService.positionToCenter();

      Promise.resolve(positioned).then(() => {
        if (!this._isPlaying() || this._isPaused()) return;
        this._stepVisible.set(true);
        this.avatarService.setState(avatarState);
        this.speakEffectiveStep(step, effective);
      });
    }, enterDelay);
  }

  /**
   * Plays the resolved speech for this step. Visuals (highlight/bubble/mascot) are already on
   * screen by the time this runs, so a speech failure or blocked autoplay never hides them —
   * only the auto-advance behavior below is gated on speech/user interaction.
   */
  private speakEffectiveStep(step: TourStepScenario, effective: EffectiveStepContent): void {
    if (!effective.speech) {
      this.handleStepCompletion(step);
      return;
    }

    const textToSpeak = this.resolveLocalizedText(effective.speech, effective.speechParams);
    const currentLang = this.l10n ? this.l10n.lang() : 'en';
    const lang = step.speechLanguage || (currentLang === 'ar' ? 'ar-EG' : 'en-US');

    // Cancel any previously active subscription
    if (this.activeSpeechSub) {
      this.activeSpeechSub.unsubscribe();
      this.activeSpeechSub = null;
    }

    this._isSpeaking.set(true);
    this.activeSpeechSub = this.speechService.speak(textToSpeak, lang).subscribe({
      next: () => {
        this._isSpeaking.set(false);
        this.avatarService.setState('idle');
        this.handleStepCompletion(step);
      },
      error: (err) => {
        this._isSpeaking.set(false);
        console.warn('[TourEngineService] Speech synthesis error:', err);
        this.avatarService.setState('idle');
        this.handleStepCompletion(step);
      },
    });
  }

  private handleStepCompletion(step: TourStepScenario): void {
    if (!this._isPlaying() || this._isPaused()) return;

    if (step.waitForUser) {
      this._isWaitingForUser.set(true);

      // Handle user timeout if configured
      if (typeof step.waitForUser === 'object' && step.waitForUser.timeoutMs) {
        this.autoAdvanceTimer = setTimeout(() => {
          if (this._isWaitingForUser()) {
            this.onUserInteracted();
          }
        }, step.waitForUser.timeoutMs);
      }
    } else {
      const delay = step.delayAfterMs ?? 1500;
      this.autoAdvanceTimer = setTimeout(() => {
        if (this._isPlaying() && !this._isPaused()) {
          this.nextStep();
        }
      }, delay);
    }
  }

  /**
   * stepExit: fades the highlight and bubble/mascot out together, then runs `after()`
   * (advance/rewind/complete) — never overlaps two steps' visuals.
   */
  private beginStepExit(after: () => void): void {
    if (this.enterTimer) {
      clearTimeout(this.enterTimer);
      this.enterTimer = null;
    }

    const wasVisible = this._stepVisible();
    this._stepVisible.set(false);
    this.clearStepResources();

    if (this.exitTimer) {
      clearTimeout(this.exitTimer);
      this.exitTimer = null;
    }

    if (wasVisible) {
      this.exitTimer = setTimeout(() => {
        this.clearDriverHighlight();
        after();
      }, STEP_EXIT_FADE_MS);
    } else {
      this.clearDriverHighlight();
      after();
    }
  }

  private completeTour(): void {
    const scenario = this._currentScenario();
    const wasVisible = this._stepVisible();

    this._stepVisible.set(false);

    const finish = () => {
      this.clearDriverHighlight();

      this._isCompleted.set(true);
      this._isPlaying.set(false);
      this._isWaitingForUser.set(false);
      this._isSpeaking.set(false);
      this._effectiveStep.set(null);

      if (typeof document !== 'undefined') {
        document.body.classList.remove('rafiq-tour-active');
      }

      this.clearAllTimers();
      this.clearStepResources();

      this.avatarService.setState('celebrate');
      this.positionService.returnHome();

      if (scenario?.onComplete) {
        try {
          scenario.onComplete();
        } catch (err) {
          console.error('[TourEngineService] Error in scenario onComplete:', err);
        }
      }

      // Reset celebrate state back to idle after gesture completes
      setTimeout(() => {
        this.avatarService.setState('idle');
      }, 2800);
    };

    if (wasVisible) {
      this.exitTimer = setTimeout(finish, STEP_EXIT_FADE_MS);
    } else {
      finish();
    }
  }

  // ── Driver.js Spotlight Rendering ──────────────────────────────────

  private highlightElementWithDriver(element: HTMLElement, config?: any): void {
    this.clearDriverHighlight();

    try {
      this.activeDriver = driver({
        animate: true,
        allowClose: false,
        overlayColor: '#000000',
        overlayOpacity: 0.6,
        stagePadding: config?.stagePadding ?? 6,
        stageRadius: config?.stageRadius ?? 12,
        popoverClass: 'rafiq-tour-driver-popover-hidden',
      });

      this.activeDriver.highlight({
        element: element,
      });
    } catch (err) {
      console.warn('[TourEngineService] Failed to render Driver.js spotlight:', err);
    }
  }

  private clearDriverHighlight(): void {
    if (this.activeDriver) {
      try {
        this.activeDriver.destroy();
      } catch {
        // Suppress driver cleanup error
      }
      this.activeDriver = null;
    }
  }

  private clearStepResources(): void {
    this._isSpeaking.set(false);
    if (this.activeSpeechSub) {
      this.activeSpeechSub.unsubscribe();
      this.activeSpeechSub = null;
    }
    if (this.autoAdvanceTimer) {
      clearTimeout(this.autoAdvanceTimer);
      this.autoAdvanceTimer = null;
    }
    this.speechService.stopSpeaking();
  }

  private clearAllTimers(): void {
    if (this.enterTimer) {
      clearTimeout(this.enterTimer);
      this.enterTimer = null;
    }
    if (this.exitTimer) {
      clearTimeout(this.exitTimer);
      this.exitTimer = null;
    }
    if (this.autoAdvanceTimer) {
      clearTimeout(this.autoAdvanceTimer);
      this.autoAdvanceTimer = null;
    }
  }

  // ── Helper Utilities ───────────────────────────────────────────────

  private resolveAnchorElement(anchorName: string): HTMLElement | null {
    let el = this.anchorRegistry.getAnchorElement(anchorName);
    if (!el && typeof document !== 'undefined') {
      const selector = anchorName.startsWith('#') || anchorName.startsWith('.')
        ? anchorName
        : `#${anchorName}`;
      el = (document.querySelector(selector) || document.getElementById(anchorName)) as HTMLElement | null;
    }
    return el;
  }

  private resolveLocalizedText(textOrKey: string, params?: Record<string, any>): string {
    let result = textOrKey;

    if (this.l10n) {
      try {
        const translations = this.l10n.t();
        const parts = textOrKey.split('.');
        let current: any = translations;
        for (const part of parts) {
          if (current && typeof current === 'object' && part in current) {
            current = current[part];
          } else {
            current = null;
            break;
          }
        }
        if (typeof current === 'string') {
          result = current;
        }
      } catch {
        // Fallback to textOrKey
      }
    }

    const mergedParams: Record<string, any> = { ...params };
    if (!mergedParams['userName']) {
      const user = this.authService?.currentUser;
      mergedParams['userName'] = user?.firstName?.trim() || user?.email?.split('@')[0] || 'العزيز';
    }

    Object.keys(mergedParams).forEach(k => {
      const escapedValue = String(mergedParams[k]).replace(/\$/g, '$$$$');
      result = result.replace(new RegExp(`{{\\s*${k}\\s*}}`, 'g'), escapedValue);
    });

    return result;
  }
}
