import { Component, inject, signal } from '@angular/core';
import { CommonModule } from '@angular/common';
import { FormsModule } from '@angular/forms';
import { ReviewService } from '../../Services/review.service';
import { ReviewTrackingService } from '../../Services/review-tracking.service';
import { LocalizationService } from '../../Services/localization.service';

@Component({
  selector: 'app-rating-popup',
  standalone: true,
  imports: [CommonModule, FormsModule],
  templateUrl: './rating-popup.html',
  styleUrl: './rating-popup.css',
})
export class RatingPopup {
  private readonly reviewService = inject(ReviewService);
  readonly tracking = inject(ReviewTrackingService);
  private readonly l10n = inject(LocalizationService);
  readonly t = this.l10n.t;

  readonly hovered = signal(0);
  readonly selected = signal(0);
  readonly comment = signal('');
  readonly submitting = signal(false);
  readonly submitted = signal(false);
  readonly error = signal('');

  readonly stars = [1, 2, 3, 4, 5];

  get activeStars(): number {
    return this.hovered() || this.selected();
  }

  select(star: number): void {
    this.selected.set(star);
    this.error.set('');
  }

  submit(): void {
    if (this.selected() === 0) {
      this.error.set(this.t().ratingPopup.selectStarError);
      return;
    }
    this.submitting.set(true);
    this.error.set('');

    this.reviewService.submit(this.selected(), this.comment()).subscribe({
      next: res => {
        this.submitting.set(false);
        if (res.success) {
          this.submitted.set(true);
          setTimeout(() => this.tracking.markSubmitted(), 2000);
        } else {
          this.error.set(res.message ?? this.t().ratingPopup.submitError);
        }
      },
      error: (err) => {
        this.submitting.set(false);
        const msg = err?.error?.message ?? err?.error?.title ?? null;
        this.error.set(msg ?? this.t().ratingPopup.submitError);
      },
    });
  }

  dismiss(): void {
    this.tracking.dismiss();
  }
}
