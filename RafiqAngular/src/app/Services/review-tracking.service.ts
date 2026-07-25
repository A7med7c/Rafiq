import { Injectable, inject, signal } from '@angular/core';
import { AuthService } from './auth-service';

const STORAGE_KEY = 'rafiq_action_count';
const DISMISSED_KEY = 'rafiq_review_dismissed';
const SUBMITTED_KEY = 'rafiq_review_submitted';
const THRESHOLD = 5;

@Injectable({ providedIn: 'root' })
export class ReviewTrackingService {
  private readonly auth = inject(AuthService);

  readonly showPopup = signal(false);

  trackAction(): void {
    if (!this.auth.isLoggedIn) return;
    if (this.hasSubmitted() || this.hasDismissed()) return;

    const count = this.getCount() + 1;
    this.setCount(count);

    if (count >= THRESHOLD) {
      this.showPopup.set(true);
    }
  }

  dismiss(): void {
    localStorage.setItem(DISMISSED_KEY, '1');
    this.showPopup.set(false);
  }

  markSubmitted(): void {
    localStorage.setItem(SUBMITTED_KEY, '1');
    this.showPopup.set(false);
  }

  private getCount(): number {
    return parseInt(localStorage.getItem(STORAGE_KEY) ?? '0', 10);
  }

  private setCount(n: number): void {
    localStorage.setItem(STORAGE_KEY, String(n));
  }

  private hasSubmitted(): boolean {
    return localStorage.getItem(SUBMITTED_KEY) === '1';
  }

  private hasDismissed(): boolean {
    return localStorage.getItem(DISMISSED_KEY) === '1';
  }
}
