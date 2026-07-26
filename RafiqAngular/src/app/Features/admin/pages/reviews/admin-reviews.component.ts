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
import { FormsModule } from '@angular/forms';
import { takeUntilDestroyed } from '@angular/core/rxjs-interop';
import { LocalizationService } from '../../../../Services/localization.service';
import { adminCopy } from '../../admin-copy';
import {
  AdminReview,
  AdminReviewQuery,
  ReviewCategory,
  ReviewOverview,
  ReviewStatus,
  ReviewTrendPoint
} from '../../models/admin.models';
import { AdminService } from '../../services/admin.service';

type StatusTab = '' | ReviewStatus;
type SortOption = 'newest' | 'oldest' | 'stars_asc' | 'stars_desc';

@Component({
  selector: 'app-admin-reviews',
  standalone: true,
  imports: [CommonModule, FormsModule],
  templateUrl: './admin-reviews.component.html',
  styleUrl: './admin-reviews.component.css',
  changeDetection: ChangeDetectionStrategy.OnPush
})
export class AdminReviewsComponent implements OnInit {
  private readonly svc = inject(AdminService);
  private readonly destroyRef = inject(DestroyRef);
  protected readonly l10n = inject(LocalizationService);

  readonly copy = computed(() => adminCopy[this.l10n.lang()].reviews);
  readonly isRtl = computed(() => this.l10n.lang() === 'ar');

  // ── List state ─────────────────────────────────────────────────────────────
  readonly loading = signal(true);
  readonly error = signal(false);
  readonly reviews = signal<AdminReview[]>([]);
  readonly total = signal(0);
  readonly page = signal(1);
  readonly pageSize = 10;
  readonly totalPages = computed(() => Math.max(1, Math.ceil(this.total() / this.pageSize)));

  // ── Filters ────────────────────────────────────────────────────────────────
  readonly statusTab = signal<StatusTab>('');
  readonly categoryFilter = signal<ReviewCategory | ''>('');
  readonly starFilter = signal<number | null>(null);
  readonly sortBy = signal<SortOption>('newest');

  // ── Overview & trends ──────────────────────────────────────────────────────
  readonly overview = signal<ReviewOverview | null>(null);
  readonly trends = signal<ReviewTrendPoint[]>([]);

  // ── Detail panel ───────────────────────────────────────────────────────────
  readonly activeReview = signal<AdminReview | null>(null);
  readonly panelNotes = signal('');
  readonly panelReply = signal('');
  readonly savingNotes = signal(false);
  readonly savingReply = signal(false);
  readonly savedNotes = signal(false);

  // ── In-table actions ───────────────────────────────────────────────────────
  readonly togglingId = signal<string | null>(null);
  readonly updatingStatusId = signal<string | null>(null);
  readonly deleteTarget = signal<AdminReview | null>(null);
  readonly deletingId = signal<string | null>(null);

  // ── Category / status maps ─────────────────────────────────────────────────
  readonly categories: ReviewCategory[] = ['General', 'BugReport', 'FeatureRequest', 'Performance', 'UiUx', 'ContentQuality'];
  readonly statuses: ReviewStatus[] = ['Pending', 'Reviewed', 'Resolved', 'Archived'];

  ngOnInit(): void {
    this.loadOverview();
    this.loadTrends();
    this.load();
  }

  // ── Data loading ───────────────────────────────────────────────────────────

  private buildQuery(): AdminReviewQuery {
    return {
      page: this.page(),
      pageSize: this.pageSize,
      status: this.statusTab() || undefined,
      category: this.categoryFilter() || undefined,
      minStars: this.starFilter() ?? undefined,
      maxStars: this.starFilter() ?? undefined,
      sortBy: this.sortBy() === 'newest' ? undefined : this.sortBy()
    };
  }

  load(): void {
    this.loading.set(true);
    this.error.set(false);
    this.svc.getAdminReviews(this.buildQuery())
      .pipe(takeUntilDestroyed(this.destroyRef))
      .subscribe({
        next: data => {
          this.reviews.set(data.items);
          this.total.set(data.total);
          this.loading.set(false);
        },
        error: () => { this.error.set(true); this.loading.set(false); }
      });
  }

  private loadOverview(): void {
    this.svc.getReviewOverview()
      .pipe(takeUntilDestroyed(this.destroyRef))
      .subscribe({ next: o => this.overview.set(o) });
  }

  private loadTrends(): void {
    this.svc.getReviewTrends(6)
      .pipe(takeUntilDestroyed(this.destroyRef))
      .subscribe({ next: t => this.trends.set(t) });
  }

  // ── Filters ────────────────────────────────────────────────────────────────

  setStatusTab(s: StatusTab): void {
    this.statusTab.set(s);
    this.page.set(1);
    this.load();
  }

  setCategoryFilter(c: ReviewCategory | ''): void {
    this.categoryFilter.set(c);
    this.page.set(1);
    this.load();
  }

  setStarFilter(n: number | null): void {
    this.starFilter.set(n);
    this.page.set(1);
    this.load();
  }

  setSortBy(s: SortOption): void {
    this.sortBy.set(s);
    this.page.set(1);
    this.load();
  }

  goToPage(p: number): void {
    if (p < 1 || p > this.totalPages()) return;
    this.page.set(p);
    this.load();
  }

  // ── In-table quick actions ────────────────────────────────────────────────

  quickStatus(review: AdminReview, status: ReviewStatus): void {
    this.updatingStatusId.set(review.id);
    this.svc.updateReviewStatus(review.id, status)
      .pipe(takeUntilDestroyed(this.destroyRef))
      .subscribe({
        next: () => { this.updatingStatusId.set(null); this.load(); this.loadOverview(); },
        error: () => this.updatingStatusId.set(null)
      });
  }

  toggleVisibility(review: AdminReview): void {
    this.togglingId.set(review.id);
    this.svc.toggleReviewVisibility(review.id, !review.isVisible)
      .pipe(takeUntilDestroyed(this.destroyRef))
      .subscribe({
        next: () => { this.togglingId.set(null); this.load(); this.loadOverview(); },
        error: () => this.togglingId.set(null)
      });
  }

  confirmDelete(review: AdminReview): void { this.deleteTarget.set(review); }
  cancelDelete(): void { this.deleteTarget.set(null); }

  doDelete(): void {
    const target = this.deleteTarget();
    if (!target) return;
    this.deletingId.set(target.id);
    this.deleteTarget.set(null);
    this.svc.deleteReview(target.id)
      .pipe(takeUntilDestroyed(this.destroyRef))
      .subscribe({
        next: () => {
          this.deletingId.set(null);
          if (this.reviews().length === 1 && this.page() > 1) this.page.update(p => p - 1);
          this.load();
          this.loadOverview();
        },
        error: () => this.deletingId.set(null)
      });
  }

  // ── Detail panel ──────────────────────────────────────────────────────────

  openDetail(review: AdminReview): void {
    this.activeReview.set(review);
    this.panelNotes.set(review.adminNotes ?? '');
    this.panelReply.set(review.adminReply ?? '');
    this.savedNotes.set(false);
  }

  closeDetail(): void { this.activeReview.set(null); }

  saveNotes(): void {
    const r = this.activeReview();
    if (!r) return;
    this.savingNotes.set(true);
    this.svc.updateAdminNotes(r.id, this.panelNotes() || null)
      .pipe(takeUntilDestroyed(this.destroyRef))
      .subscribe({
        next: () => {
          this.savingNotes.set(false);
          this.savedNotes.set(true);
          setTimeout(() => this.savedNotes.set(false), 2000);
          this.load();
        },
        error: () => this.savingNotes.set(false)
      });
  }

  sendReply(): void {
    const r = this.activeReview();
    if (!r) return;
    this.savingReply.set(true);
    this.svc.replyToReview(r.id, this.panelReply() || null)
      .pipe(takeUntilDestroyed(this.destroyRef))
      .subscribe({
        next: () => {
          this.savingReply.set(false);
          this.load();
          this.loadOverview();
          // Refresh active review from updated list
          this.svc.getAdminReviews({ page: this.page(), pageSize: this.pageSize })
            .pipe(takeUntilDestroyed(this.destroyRef))
            .subscribe(data => {
              const updated = data.items.find(i => i.id === r.id);
              if (updated) this.activeReview.set(updated);
            });
        },
        error: () => this.savingReply.set(false)
      });
  }

  updatePanelCategory(r: AdminReview, cat: ReviewCategory): void {
    this.svc.updateReviewCategory(r.id, cat)
      .pipe(takeUntilDestroyed(this.destroyRef))
      .subscribe({ next: () => this.load() });
  }

  updatePanelStatus(r: AdminReview, status: ReviewStatus): void {
    this.svc.updateReviewStatus(r.id, status)
      .pipe(takeUntilDestroyed(this.destroyRef))
      .subscribe({
        next: () => { this.load(); this.loadOverview(); }
      });
  }

  useQuickReply(text: string): void { this.panelReply.set(text); }

  // ── Helpers ────────────────────────────────────────────────────────────────

  stars(n: number): number[] { return Array.from({ length: 5 }, (_, i) => i + 1); }

  catLabel(c: ReviewCategory): string {
    const m: Record<ReviewCategory, string> = {
      General: this.copy().catGeneral,
      BugReport: this.copy().catBugReport,
      FeatureRequest: this.copy().catFeatureRequest,
      Performance: this.copy().catPerformance,
      UiUx: this.copy().catUiUx,
      ContentQuality: this.copy().catContentQuality
    };
    return m[c] ?? c;
  }

  statusLabel(s: ReviewStatus): string {
    const m: Record<ReviewStatus, string> = {
      Pending: this.copy().statusPending,
      Reviewed: this.copy().statusReviewed,
      Resolved: this.copy().statusResolved,
      Archived: this.copy().statusArchived
    };
    return m[s] ?? s;
  }

  healthColor(score: number): string {
    if (score >= 80) return '#16875a';
    if (score >= 60) return '#c97716';
    return '#c2414b';
  }

  trendPath(points: ReviewTrendPoint[]): string {
    if (points.length === 0) return '';
    const W = 380, H = 80, PAD = 4;
    const vals = points.map(p => p.averageStars);
    const min = Math.min(...vals, 1);
    const max = Math.max(...vals, 5);
    if (points.length === 1) {
      const y = (H - PAD - ((vals[0] - min) / (max - min || 1)) * (H - PAD * 2)).toFixed(1);
      return `M0,${y} L${W},${y}`;
    }
    const xs = points.map((_, i) => (i / (points.length - 1)) * W);
    const ys = vals.map(v => H - PAD - ((v - min) / (max - min || 1)) * (H - PAD * 2));
    let d = `M${xs[0].toFixed(1)},${ys[0].toFixed(1)}`;
    for (let i = 1; i < xs.length; i++) {
      const cpx = (xs[i - 1] + xs[i]) / 2;
      d += ` C${cpx.toFixed(1)},${ys[i - 1].toFixed(1)} ${cpx.toFixed(1)},${ys[i].toFixed(1)} ${xs[i].toFixed(1)},${ys[i].toFixed(1)}`;
    }
    return d;
  }

  trendAreaPath(points: ReviewTrendPoint[]): string {
    const line = this.trendPath(points);
    if (!line) return '';
    const W = 380, H = 80;
    return `${line} L${W},${H} L0,${H} Z`;
  }

  formatDate(iso: string): string {
    return new Intl.DateTimeFormat(this.l10n.lang() === 'ar' ? 'ar-EG' : 'en-US', {
      month: 'short', day: 'numeric', year: 'numeric'
    }).format(new Date(iso));
  }

  catColors: Record<string, string> = {
    General: '#6366f1',
    BugReport: '#ef4444',
    FeatureRequest: '#3b82f6',
    Performance: '#f59e0b',
    UiUx: '#8b5cf6',
    ContentQuality: '#10b981'
  };
}
