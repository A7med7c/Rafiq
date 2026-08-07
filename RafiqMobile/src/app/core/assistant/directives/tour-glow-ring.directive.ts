/**
 * @file tour-glow-ring.directive.ts
 * @description Mobile tour highlight directive.
 * Replaces driver.js spotlight on mobile with a CSS glow ring applied directly
 * to the registered anchor element. No overlay, no blocking UI — just a
 * pulsing ring that draws the eye without covering the content.
 *
 * Usage: place `tourGlowRing` on the root host element (e.g. the app shell div).
 * The directive watches TourEngineService signals and attaches/detaches the
 * `tour-glow-ring` CSS class from whichever element the current step anchors to.
 */

import { Directive, OnDestroy, effect, inject } from '@angular/core';
import { TourEngineService } from '../services/tour-engine.service';
import { AssistantAnchorRegistryService } from '../services/assistant-anchor-registry.service';

@Directive({
  selector: '[tourGlowRing]',
  standalone: true,
})
export class TourGlowRingDirective implements OnDestroy {
  private readonly tourEngine = inject(TourEngineService);
  private readonly registry = inject(AssistantAnchorRegistryService);

  /** The element that currently has the glow ring class applied. */
  private highlightedEl: HTMLElement | null = null;

  constructor() {
    effect(() => {
      const playing = this.tourEngine.isPlaying();
      const visible = this.tourEngine.stepVisible();
      const hasAnchor = this.tourEngine.currentStepHasAnchor();

      if (!playing || !visible || !hasAnchor) {
        this.clearGlow();
        return;
      }

      const step = this.tourEngine.currentStep() as any;
      if (!step) {
        this.clearGlow();
        return;
      }

      const anchorName: string | undefined =
        step.anchor ??
        step.variants?.populated?.anchor ??
        step.variants?.empty?.anchor;

      if (!anchorName) {
        this.clearGlow();
        return;
      }

      const el = this.registry.getAnchorElement(anchorName);
      if (!el) {
        this.clearGlow();
        return;
      }

      // Move glow to the new element
      if (this.highlightedEl !== el) {
        this.clearGlow();
        this.highlightedEl = el;
        el.classList.add('tour-glow-ring');
        el.scrollIntoView({ behavior: 'smooth', block: 'nearest' });
      }
    });
  }

  ngOnDestroy(): void {
    this.clearGlow();
  }

  private clearGlow(): void {
    if (this.highlightedEl) {
      this.highlightedEl.classList.remove('tour-glow-ring');
      this.highlightedEl = null;
    }
    // Belt-and-suspenders: clear any orphaned rings in the whole document
    document.querySelectorAll('.tour-glow-ring').forEach(el => {
      el.classList.remove('tour-glow-ring');
    });
  }
}
