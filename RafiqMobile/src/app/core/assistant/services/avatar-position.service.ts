/**
 * @file avatar-position.service.ts
 * @description Positions the tour speech bubble against a live anchor element (or a virtual
 * center point for unanchored steps) using @floating-ui/dom, and derives which corner of the
 * bubble the mascot chip should attach to from floating-ui's own flip() decision.
 * Responsibility: computePosition() only — never independently positions the mascot.
 */

import { Injectable, signal, computed, NgZone, inject } from '@angular/core';
import { Router, NavigationEnd } from '@angular/router';
import { filter } from 'rxjs/operators';
import { computePosition, offset, flip, shift, limitShift, type Placement } from '@floating-ui/dom';

/** Legacy placement type kept for scenario-model compatibility; no longer drives positioning math. */
export type ElementPlacement = 'left' | 'right' | 'top' | 'bottom';

export type MascotCorner = 'top-left' | 'top-right' | 'bottom-left' | 'bottom-right';

type FloatingReference = HTMLElement | { getBoundingClientRect(): DOMRect };

@Injectable({
  providedIn: 'root',
})
export class AvatarPositionService {
  private readonly ngZone = inject(NgZone);
  private readonly router = inject(Router, { optional: true });

  constructor() {
    // Route changes (e.g. in-page navigation triggered by app UI while a step is active)
    // must reposition the bubble against the same cached reference — never re-resolve the anchor.
    this.router?.events
      .pipe(filter((e): e is NavigationEnd => e instanceof NavigationEnd))
      .subscribe(() => {
        if (this.referenceEl) {
          void this.recompute();
        }
      });
  }

  // ── Signals ────────────────────────────────────────────────────────
  private readonly _bubbleLeft = signal<number>(0);
  private readonly _bubbleTop = signal<number>(0);
  private readonly _placement = signal<Placement>('right-start');
  private readonly _ready = signal<boolean>(false);

  readonly bubbleLeft = this._bubbleLeft.asReadonly();
  readonly bubbleTop = this._bubbleTop.asReadonly();
  readonly placement = this._placement.asReadonly();
  readonly ready = this._ready.asReadonly();

  /** Which corner of the bubble the mascot chip should attach to, derived from the resolved placement. */
  readonly mascotCorner = computed<MascotCorner>(() => this.cornerForPlacement(this._placement()));

  private floatingEl: HTMLElement | null = null;
  private referenceEl: FloatingReference | null = null;
  private listenersBound = false;
  /** When set, this side is tried first (e.g. 'top' to force the bubble to sit above a fixed point). */
  private preferredSide: 'top' | 'bottom' | 'left' | 'right' | null = null;

  /** Registers the bubble DOM element that floating-ui measures against. Called once by the component. */
  setFloatingElement(el: HTMLElement | null): void {
    this.floatingEl = el;
    if (el && this.referenceEl) {
      void this.recompute();
    }
  }

  /**
   * Positions the bubble relative to a resolved anchor element.
   * The caller is responsible for resolving the anchor once and reusing it for the spotlight too.
   */
  async positionToAnchor(target: HTMLElement): Promise<void> {
    this.referenceEl = target;
    this.preferredSide = null;
    this.bindListeners();
    await this.recompute();
  }

  /** Positions the bubble relative to a virtual point (used for the unanchored intro step). */
  async positionToCenter(): Promise<void> {
    const vw = typeof window !== 'undefined' ? window.innerWidth : 0;
    this.referenceEl = {
      getBoundingClientRect: () =>
        ({
          width: 0,
          height: 0,
          x: vw / 2,
          y: 140,
          top: 140,
          left: vw / 2,
          right: vw / 2,
          bottom: 140,
        }) as DOMRect,
    };
    this.preferredSide = null;
    this.bindListeners();
    await this.recompute();
  }

  /**
   * Positions the bubble against a fixed, constant virtual point (not tied to any page element).
   * Used by scenarios that want the mascot+bubble to stay put in the same spot on every step
   * (e.g. onboarding), instead of chasing an anchor around the page.
   * @param preferredSide Tried before the standard fallback order — e.g. 'top' makes the bubble
   * sit above the point every time (so the mascot chip attaches below it), instead of whichever
   * side flip() would otherwise pick first.
   */
  async positionFixed(x: number, y: number, preferredSide: 'top' | 'bottom' | 'left' | 'right' = 'top'): Promise<void> {
    this.referenceEl = {
      getBoundingClientRect: () =>
        ({
          width: 0,
          height: 0,
          x,
          y,
          top: y,
          left: x,
          right: x,
          bottom: y,
        }) as DOMRect,
    };
    this.preferredSide = preferredSide;
    this.bindListeners();
    await this.recompute();
  }

  /**
   * Positions the bubble against a fixed point relative to the bottom of the viewport.
   * Dynamically re-evaluates y = window.innerHeight - yFromBottom on resize/recompute.
   */
  async positionFixedFromBottom(x?: number, yFromBottom: number = 140, preferredSide: 'top' | 'bottom' | 'left' | 'right' = 'top'): Promise<void> {
    this.referenceEl = {
      getBoundingClientRect: () => {
        const vw = typeof window !== 'undefined' ? window.innerWidth : 390;
        const vh = typeof window !== 'undefined' ? window.innerHeight : 840;
        const targetX = x ?? (vw / 2);
        const y = vh - yFromBottom;
        return {
          width: 0,
          height: 0,
          x: targetX,
          y,
          top: y,
          left: targetX,
          right: targetX,
          bottom: y,
        } as DOMRect;
      },
    };
    this.preferredSide = preferredSide;
    this.bindListeners();
    await this.recompute();
  }

  /**
   * Recomputes the bubble position against the CURRENTLY cached reference — used on
   * resize/scroll/route-change while a step is active. Never re-resolves the anchor.
   */
  async recompute(): Promise<void> {
    if (!this.referenceEl || !this.floatingEl || typeof window === 'undefined') return;

    const rtl = typeof document !== 'undefined' && document.documentElement.dir === 'rtl';
    const standardOrder: Placement[] = rtl
      ? ['left-start', 'top-start', 'bottom-start', 'right-start']
      : ['right-start', 'top-start', 'bottom-start', 'left-start'];

    // A preferredSide (fixedPosition steps, e.g. onboarding) means "always this side, full stop" —
    // no flip fallback. The whole point is a deterministic, unchanging layout; letting flip jump to
    // a different side on a tight-but-workable fit (which limitShift would handle fine) would defeat
    // that. Anchored steps keep the normal beside → above → below → opposite flip chain.
    const preferred = this.preferredSide ? (`${this.preferredSide}-start` as Placement) : null;
    const fallbackPlacements: Placement[] = preferred ? [preferred] : standardOrder;
    const basePlacement: Placement = fallbackPlacements[0];

    try {
      const { x, y, placement } = await computePosition(this.referenceEl, this.floatingEl, {
        placement: basePlacement,
        middleware: [
          offset(12),
          flip({ fallbackPlacements }),
          // On large reference elements (e.g. onboarding's full-width form cards) there may be
          // no placement that fits fully outside the reference within the viewport. Without a
          // limiter, shift() will happily slide the bubble back OVER the reference/its buttons
          // to stay inbounds. limitShift() caps that slide so the bubble stays pinned near its
          // flip()-chosen side instead of being dragged back on top of the content it's describing.
          shift({ padding: 16, limiter: limitShift() }),
        ],
        strategy: 'fixed',
      });

      this._bubbleLeft.set(Math.round(x));
      this._bubbleTop.set(Math.round(y));
      this._placement.set(placement);
      this._ready.set(true);
    } catch {
      // Reference element may have been detached mid-flight (route change); ignore.
    }
  }

  /** Clears the active reference and hides the bubble; called on tour stop/complete. */
  returnHome(): void {
    this.unbindListeners();
    this.referenceEl = null;
    this.preferredSide = null;
    this._ready.set(false);
  }

  private cornerForPlacement(placement: Placement): MascotCorner {
    const [side, align] = placement.split('-') as [string, string | undefined];
    const opposite: Record<string, string> = { top: 'bottom', bottom: 'top', left: 'right', right: 'left' };
    const chipSide = opposite[side] ?? 'left';
    const rtl = typeof document !== 'undefined' && document.documentElement.dir === 'rtl';

    if (chipSide === 'left' || chipSide === 'right') {
      const vertical = align === 'end' ? 'bottom' : 'top';
      return `${vertical}-${chipSide}` as MascotCorner;
    }

    const horizontal = align === 'end' ? (rtl ? 'left' : 'right') : rtl ? 'right' : 'left';
    return `${chipSide}-${horizontal}` as MascotCorner;
  }

  private bindListeners(): void {
    if (this.listenersBound || typeof window === 'undefined') return;
    this.listenersBound = true;
    this.ngZone.runOutsideAngular(() => {
      window.addEventListener('resize', this.onViewportChange, { passive: true });
      window.addEventListener('scroll', this.onViewportChange, { passive: true, capture: true });
    });
  }

  private unbindListeners(): void {
    if (!this.listenersBound || typeof window === 'undefined') return;
    this.listenersBound = false;
    window.removeEventListener('resize', this.onViewportChange);
    window.removeEventListener('scroll', this.onViewportChange, true);
  }

  private readonly onViewportChange = (): void => {
    this.ngZone.run(() => {
      void this.recompute();
    });
  };
}
