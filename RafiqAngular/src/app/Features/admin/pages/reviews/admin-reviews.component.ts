import { CommonModule } from '@angular/common';
import {
  ChangeDetectionStrategy,
  Component,
  DestroyRef,
  OnInit,
  computed,
  inject,
  signal
} from '@angular/core';
import { RouterLink } from '@angular/router';
import { takeUntilDestroyed } from '@angular/core/rxjs-interop';
import { LocalizationService } from '../../../../Services/localization.service';
import { adminCopy } from '../../admin-copy';
import { AdminReview, AdminReviewsPage, ReviewStats } from '../../models/admin.models';
import { AdminService } from '../../services/admin.service';

@Component({
  selector: 'app-admin-reviews',
  standalone: true,
  imports: [CommonModule, RouterLink],
  templateUrl: './admin-reviews.component.html',
  styleUrl: './admin-reviews.component.css',
  changeDetection: ChangeDetectionStrategy.OnPush
})
export class AdminReviewsComponent implements OnInit {
  private readonly adminService = inject(AdminService);
  private readonly destroyRef = inject(DestroyRef);
  protected readonly l10n = inject(LocalizationService);

  readonly copy = computed(() => adminCopy[this.l10n.lang()].reviews);

  readonly loading = signal(true);
  readonly error = signal<string | null>(null);
  readonly result = signal<AdminReviewsPage | null>(null);
  readonly stats = signal<ReviewStats | null>(null);

  readonly page = signal(1);
  readonly pageSize = 15;

  readonly deleteTarget = signal<AdminReview | null>(null);
  readonly togglingId = signal<string | null>(null);
  readonly deletingId = signal<string | null>(null);

  readonly totalPages = computed(() => {
    const r = this.result();
    return r ? Math.max(1, Math.ceil(r.total / this.pageSize)) : 1;
  });

  ngOnInit(): void {
    this.load();
    this.loadStats();
  }

  private load(): void {
    this.loading.set(true);
    this.error.set(null);
    this.adminService.getAdminReviews(this.page(), this.pageSize)
      .pipe(takeUntilDestroyed(this.destroyRef))
      .subscribe({
        next: data => { this.result.set(data); this.loading.set(false); },
        error: () => { this.error.set('load'); this.loading.set(false); }
      });
  }

  private loadStats(): void {
    this.adminService.getReviewStats()
      .pipe(takeUntilDestroyed(this.destroyRef))
      .subscribe({ next: s => this.stats.set(s) });
  }

  goToPage(p: number): void {
    if (p < 1 || p > this.totalPages()) return;
    this.page.set(p);
    this.load();
  }

  toggleVisibility(review: AdminReview): void {
    this.togglingId.set(review.id);
    this.adminService.toggleReviewVisibility(review.id, !review.isVisible)
      .pipe(takeUntilDestroyed(this.destroyRef))
      .subscribe({
        next: () => {
          this.togglingId.set(null);
          this.load();
          this.loadStats();
        },
        error: () => this.togglingId.set(null)
      });
  }

  confirmDelete(review: AdminReview): void {
    this.deleteTarget.set(review);
  }

  cancelDelete(): void {
    this.deleteTarget.set(null);
  }

  doDelete(): void {
    const target = this.deleteTarget();
    if (!target) return;
    this.deletingId.set(target.id);
    this.deleteTarget.set(null);
    this.adminService.deleteReview(target.id)
      .pipe(takeUntilDestroyed(this.destroyRef))
      .subscribe({
        next: () => {
          this.deletingId.set(null);
          if (this.result()?.items.length === 1 && this.page() > 1) {
            this.page.update(p => p - 1);
          }
          this.load();
          this.loadStats();
        },
        error: () => this.deletingId.set(null)
      });
  }

  starsArray(n: number): number[] {
    return Array.from({ length: n });
  }
}
