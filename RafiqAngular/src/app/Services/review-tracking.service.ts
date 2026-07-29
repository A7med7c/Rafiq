import { Injectable, inject, signal } from '@angular/core';
import { AuthService } from './auth-service';

const S = {
  TOTAL_ACTIONS:  'rafiq_total_actions',
  NEXT_PROMPT_AT: 'rafiq_next_prompt_at',
  LAST_SUBMITTED: 'rafiq_last_submitted_at',
  DISMISS_COUNT:  'rafiq_dismiss_count',
} as const;

const FIRST_PROMPT_AT      = 5;
const FIRST_SNOOZE_ACTIONS = 20;
const REPEAT_SNOOZE_ACTIONS = 40;
const POST_SUBMIT_COOLDOWN_DAYS = 30;

@Injectable({ providedIn: 'root' })
export class ReviewTrackingService {
  private readonly auth = inject(AuthService);

  readonly showPopup = signal(false);

  trackAction(): void {
    if (!this.auth.isLoggedIn) return;
    if (this.showPopup()) return;
    if (this.inSubmitCooldown()) return;

    const total = this.getTotal() + 1;
    this.setTotal(total);

    if (total >= this.getNextPromptAt()) {
      this.showPopup.set(true);
    }
  }

  /** Called when the user clicks "Not now" or closes the popup. */
  dismiss(): void {
    const dismissCount = this.getDismissCount() + 1;
    this.setDismissCount(dismissCount);
    const snooze = dismissCount === 1 ? FIRST_SNOOZE_ACTIONS : REPEAT_SNOOZE_ACTIONS;
    this.setNextPromptAt(this.getTotal() + snooze);
    this.showPopup.set(false);
  }

  /** Called after a successful submission. Suppresses auto-popup for 30 days. */
  markSubmitted(): void {
    localStorage.setItem(S.LAST_SUBMITTED, new Date().toISOString());
    // Schedule next auto-prompt after the cooldown would have lifted
    this.setNextPromptAt(this.getTotal() + REPEAT_SNOOZE_ACTIONS);
    this.showPopup.set(false);
  }

  /** Always opens the popup regardless of cooldown (manual invocation). */
  openManually(): void {
    this.showPopup.set(true);
  }

  // ── Private helpers ───────────────────────────────────────────────────────

  private inSubmitCooldown(): boolean {
    const raw = localStorage.getItem(S.LAST_SUBMITTED);
    if (!raw) return false;
    const submitted = new Date(raw).getTime();
    const cooldownMs = POST_SUBMIT_COOLDOWN_DAYS * 24 * 60 * 60 * 1000;
    return Date.now() - submitted < cooldownMs;
  }

  private getTotal(): number {
    return parseInt(localStorage.getItem(S.TOTAL_ACTIONS) ?? '0', 10);
  }

  private setTotal(n: number): void {
    localStorage.setItem(S.TOTAL_ACTIONS, String(n));
  }

  private getNextPromptAt(): number {
    const raw = localStorage.getItem(S.NEXT_PROMPT_AT);
    return raw !== null ? parseInt(raw, 10) : FIRST_PROMPT_AT;
  }

  private setNextPromptAt(n: number): void {
    localStorage.setItem(S.NEXT_PROMPT_AT, String(n));
  }

  private getDismissCount(): number {
    return parseInt(localStorage.getItem(S.DISMISS_COUNT) ?? '0', 10);
  }

  private setDismissCount(n: number): void {
    localStorage.setItem(S.DISMISS_COUNT, String(n));
  }
}
