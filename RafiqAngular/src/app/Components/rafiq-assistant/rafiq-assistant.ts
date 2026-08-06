import { CommonModule } from '@angular/common';
import { Component, ElementRef, OnDestroy, computed, effect, inject, viewChild } from '@angular/core';
import { Router, NavigationEnd } from '@angular/router';
import { toSignal } from '@angular/core/rxjs-interop';
import { filter, map } from 'rxjs/operators';
import { AvatarEngineComponent } from '../avatar-engine/avatar-engine';
import { AvatarPositionService } from '../../core/assistant/services/avatar-position.service';
import { TourEngineService } from '../../core/assistant/services/tour-engine.service';
import { LocalizationService } from '../../Services/localization.service';

import { SpeechService } from '../../core/assistant/services/speech.service';
import { signal } from '@angular/core';

/**
 * The travelling Rafiq mascot and its contextual walkthrough bubble.
 *
 * The mascot is NOT positioned independently: it renders as a small avatar chip nested inside
 * the speech bubble, attached to whichever corner AvatarPositionService's floating-ui flip()
 * decision resolved to. Moving/reorienting the bubble always moves the mascot with it.
 */
@Component({
  selector: 'app-rafiq-assistant',
  standalone: true,
  imports: [CommonModule, AvatarEngineComponent],
  templateUrl: './rafiq-assistant.html',
  styleUrl: './rafiq-assistant.css',
  host: {
    style: 'pointer-events: none;',
  },
})
export class RafiqAssistantComponent implements OnDestroy {
  readonly positionService = inject(AvatarPositionService);
  readonly tourEngine = inject(TourEngineService);
  private readonly speechService = inject(SpeechService);
  readonly l10n = inject(LocalizationService);
  private readonly router = inject(Router);
  readonly t = this.l10n.t;
  readonly dir = this.l10n.dir;

  readonly isMuted = this.speechService.isMuted;

  readonly bubbleEl = viewChild<ElementRef<HTMLElement>>('bubbleEl');

  private readonly currentUrl = toSignal(
    this.router.events.pipe(
      filter(e => e instanceof NavigationEnd),
      map(e => (e as NavigationEnd).urlAfterRedirects)
    ),
    { initialValue: this.router.url }
  );

  /** True if the user is currently on any onboarding page (robot is embedded in the form instead). */
  readonly isOnboardingPage = computed(() => {
    return this.currentUrl().includes('/onboarding/');
  });

  readonly stepIndexes = computed(() =>
    Array.from({ length: this.tourEngine.totalSteps() }, (_, i) => i)
  );

  toggleMute(): void {
    const isNowMuted = this.speechService.toggleMute();
    if (!isNowMuted) {
      this.replaySpeech();
    }
  }

  replaySpeech(): void {
    const text = this.tourEngine.currentStepSpeechResolved();
    if (!text) return;
    const lang = this.l10n.isRtl() ? 'ar-EG' : 'en-US';
    this.speechService.speak(text, lang).subscribe();
  }

  constructor() {
    // Register the bubble element with the positioning service as soon as it exists in the
    // DOM (it is created/destroyed by the @if in the template alongside tourEngine.isPlaying()).
    effect(() => {
      const ref = this.bubbleEl();
      this.positionService.setFloatingElement(ref?.nativeElement ?? null);
    });

    effect(() => {
      // If user has muted speech and tour engine tries to speak, stop speaking
      if (this.isMuted() && this.tourEngine.isSpeaking()) {
        this.speechService.stopSpeaking();
      }
    });
  }

  ngOnDestroy(): void {
    this.positionService.setFloatingElement(null);
  }
}
