/**
 * @file avatar-position.service.ts
 * @description Positions the tour speech bubble adaptively against a live anchor element
 * (or virtual points for unanchored steps) using @floating-ui/dom.
 * Implements rule-based spatial positioning to ensure the mascot/bubble never obstructs
 * the highlighted spotlight element, header, bottom nav, FAB, or active keyboard.
 */

import { Injectable, signal, computed, NgZone, inject } from '@angular/core';
import { Router, NavigationEnd } from '@angular/router';
import { filter } from 'rxjs/operators';
import { computePosition, offset, flip, shift, limitShift, type Placement } from '@floating-ui/dom';

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
  private readonly _placement = signal<Placement>('bottom-start');
  private readonly _ready = signal<boolean>(false);

  readonly bubbleLeft = this._bubbleLeft.asReadonly();
  readonly bubbleTop = this._bubbleTop.asReadonly();
  readonly placement = this._placement.asReadonly();
  readonly ready = this._ready.asReadonly();

  /** Which corner of the bubble the mascot chip should attach to. */
  readonly mascotCorner = computed<MascotCorner>(() => this.cornerForPlacement(this._placement()));

  private floatingEl: HTMLElement | null = null;
  private referenceEl: FloatingReference | null = null;
  private listenersBound = false;
  private preferredSide: 'top' | 'bottom' | 'left' | 'right' | null = null;

  setFloatingElement(el: HTMLElement | null): void {
    this.floatingEl = el;
    if (el && this.referenceEl) {
      void this.recompute();
    }
  }

  /**
   * Positions the bubble relative to a resolved anchor element using adaptive rules:
   * Rule 1 (Top 40%): Target in upper 40% of viewport -> Mascot/bubble near bottom ('bottom').
   * Rule 2 (Bottom 40%): Target in lower 40% of viewport -> Mascot/bubble above target ('top').
   * Rule 3 (Center): Target in middle -> Places mascot/bubble in the region with maximum free space.
   */
  async positionToAnchor(target: HTMLElement): Promise<void> {
    this.referenceEl = target;
    this.preferredSide = this.calculateAdaptiveSide(target);
    this.bindListeners();
    await this.recompute();
  }

  /** Calculates the safest, non-obstructive side for the mascot relative to the target element */
  private calculateAdaptiveSide(target: HTMLElement): 'top' | 'bottom' | 'left' | 'right' {
    if (typeof window === 'undefined') return 'bottom';
    const rect = target.getBoundingClientRect();
    const vh = window.innerHeight;
    const centerY = rect.top + rect.height / 2;

    // Rule 1: Upper 40% of screen -> Place mascot near bottom ('bottom')
    if (centerY < vh * 0.40) {
      return 'bottom';
    }

    // Rule 2: Lower 40% of screen -> Place mascot near top ('top')
    if (centerY > vh * 0.60) {
      return 'top';
    }

    // Rule 3: Center -> Calculate available space (top vs bottom vs left vs right)
    const spaceTop = rect.top;
    const spaceBottom = vh - rect.bottom;

    if (spaceBottom >= spaceTop) {
      return 'bottom';
    } else {
      return 'top';
    }
  }

  /** Positions the bubble relative to a virtual point (used for unanchored intro steps). */
  async positionToCenter(): Promise<void> {
    const vw = typeof window !== 'undefined' ? window.innerWidth : 390;
    const vh = typeof window !== 'undefined' ? window.innerHeight : 840;
    this.referenceEl = {
      getBoundingClientRect: () =>
        ({
          width: 0,
          height: 0,
          x: vw / 2,
          y: vh * 0.25,
          top: vh * 0.25,
          left: vw / 2,
          right: vw / 2,
          bottom: vh * 0.25,
        }) as DOMRect,
    };
    this.preferredSide = 'bottom';
    this.bindListeners();
    await this.recompute();
  }

  /** Fixed position relative to top/x. */
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

  /** Fixed position relative to bottom of viewport. */
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
   * Recomputes the bubble position against the CURRENTLY cached reference using floating-ui.
   */
  async recompute(): Promise<void> {
    if (!this.referenceEl || !this.floatingEl || typeof window === 'undefined') return;

    const rtl = typeof document !== 'undefined' && document.documentElement.dir === 'rtl';

    // Order priority based on preferred side + RTL/LTR alignment
    let fallbackPlacements: Placement[] = [];
    if (this.preferredSide === 'bottom') {
      fallbackPlacements = rtl
        ? ['bottom-start', 'bottom', 'top-start', 'top', 'left-start', 'right-start']
        : ['bottom-start', 'bottom', 'top-start', 'top', 'right-start', 'left-start'];
    } else if (this.preferredSide === 'top') {
      fallbackPlacements = rtl
        ? ['top-start', 'top', 'bottom-start', 'bottom', 'left-start', 'right-start']
        : ['top-start', 'top', 'bottom-start', 'bottom', 'right-start', 'left-start'];
    } else {
      fallbackPlacements = rtl
        ? ['bottom-start', 'top-start', 'left-start', 'right-start']
        : ['bottom-start', 'top-start', 'right-start', 'left-start'];
    }

    const basePlacement: Placement = fallbackPlacements[0];

    try {
      const { x, y, placement } = await computePosition(this.referenceEl, this.floatingEl, {
        placement: basePlacement,
        middleware: [
          offset(12),
          flip({ fallbackPlacements }),
          // Exclude App Header (top 65px) & Bottom Nav Bar (bottom 85px) from collision
          shift({
            padding: { top: 65, bottom: 85, left: 12, right: 12 },
            limiter: limitShift()
          }),
        ],
        strategy: 'fixed',
      });

      this._bubbleLeft.set(Math.round(x));
      this._bubbleTop.set(Math.round(y));
      this._placement.set(placement);
      this._ready.set(true);
    } catch {
      // Reference element may have been detached mid-flight; ignore.
    }
  }

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
