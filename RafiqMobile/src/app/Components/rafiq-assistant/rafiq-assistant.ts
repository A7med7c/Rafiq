import { CommonModule } from '@angular/common';
import { Component, ElementRef, OnDestroy, computed, effect, inject, signal, viewChild } from '@angular/core';
import { Router, NavigationEnd } from '@angular/router';
import { toSignal } from '@angular/core/rxjs-interop';
import { filter, map } from 'rxjs/operators';
import { AvatarEngineComponent } from '../avatar-engine/avatar-engine';
import { AvatarPositionService } from '../../core/assistant/services/avatar-position.service';
import { TourEngineService } from '../../core/assistant/services/tour-engine.service';
import { LocalizationService } from '../../Services/localization.service';

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
  protected readonly l10n = inject(LocalizationService, { optional: true });
  private readonly router = inject(Router);
  private readonly keyboardOffset = signal(0);

  readonly bubbleEl = viewChild<ElementRef<HTMLElement>>('bubbleEl');

  private readonly currentUrl = toSignal(
    this.router.events.pipe(
      filter(e => e instanceof NavigationEnd),
      map(e => (e as NavigationEnd).urlAfterRedirects)
    ),
    { initialValue: this.router.url }
  );

  /** True while the user is on any /onboarding/ page. */
  readonly isOnboardingPage = computed(() =>
    this.currentUrl().includes('/onboarding/')
  );

  readonly stepIndexes = computed(() =>
    Array.from({ length: this.tourEngine.totalSteps() }, (_, i) => i)
  );

  readonly isOnboardingTour = computed(() =>
    this.tourEngine.currentScenario()?.id === 'onboarding-tour' ||
    this.tourEngine.currentScenario()?.id === 'onboarding-tour-en'
  );

  readonly onboardingGuideBottom = computed(() => {
    const keyboard = this.keyboardOffset();
    return keyboard > 0 ? keyboard + 18 : 96;
  });

  readonly isKeyboardOpen = computed(() => this.keyboardOffset() > 0);

  previousLabel(): string {
    return this.l10n?.lang() === 'ar' ? 'السابق' : 'Previous';
  }

  progressLabel(stepIndex: number): string {
    return `${stepIndex + 1} / ${this.tourEngine.totalSteps()}`;
  }

  constructor() {
    effect(() => {
      const ref = this.bubbleEl();
      this.positionService.setFloatingElement(ref?.nativeElement ?? null);
    });

    if (typeof window !== 'undefined') {
      this.bindKeyboardTracking();
    }
  }

  ngOnDestroy(): void {
    this.positionService.setFloatingElement(null);
    if (typeof window !== 'undefined') {
      this.unbindKeyboardTracking();
    }
  }

  private bindKeyboardTracking(): void {
    window.visualViewport?.addEventListener('resize', this.updateKeyboardOffset, { passive: true });
    window.visualViewport?.addEventListener('scroll', this.updateKeyboardOffset, { passive: true });
    window.addEventListener('resize', this.updateKeyboardOffset, { passive: true });
    window.addEventListener('focusin', this.updateKeyboardOffset);
    window.addEventListener('focusout', this.updateKeyboardOffset);
    this.updateKeyboardOffset();
  }

  private unbindKeyboardTracking(): void {
    window.visualViewport?.removeEventListener('resize', this.updateKeyboardOffset);
    window.visualViewport?.removeEventListener('scroll', this.updateKeyboardOffset);
    window.removeEventListener('resize', this.updateKeyboardOffset);
    window.removeEventListener('focusin', this.updateKeyboardOffset);
    window.removeEventListener('focusout', this.updateKeyboardOffset);
  }

  private readonly updateKeyboardOffset = (): void => {
    const viewport = window.visualViewport;
    if (!viewport) {
      this.keyboardOffset.set(0);
      return;
    }

    const hiddenHeight = Math.max(0, window.innerHeight - viewport.height - viewport.offsetTop);
    this.keyboardOffset.set(hiddenHeight > 80 ? Math.round(hiddenHeight) : 0);
  };
}
