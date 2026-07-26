import { CommonModule } from '@angular/common';
import {
  ChangeDetectionStrategy, Component, HostListener,
  OnInit, computed, inject, signal
} from '@angular/core';
import { FormsModule } from '@angular/forms';
import { LocalizationService } from '../../../../Services/localization.service';
import { adminCopy } from '../../admin-copy';
import {
  AuditLogEntry, AuditLogSummary, AuditModule, AuditSeverity
} from '../../models/admin.models';
import { AdminService } from '../../services/admin.service';

@Component({
  selector: 'app-admin-audit-logs',
  standalone: true,
  imports: [CommonModule, FormsModule],
  templateUrl: './admin-audit-logs.component.html',
  styleUrl: './admin-audit-logs.component.css',
  changeDetection: ChangeDetectionStrategy.OnPush
})
export class AdminAuditLogsComponent implements OnInit {
  protected readonly l10n    = inject(LocalizationService);
  private  readonly adminSvc = inject(AdminService);
  readonly copy    = computed(() => adminCopy[this.l10n.lang()].auditLogs);
  readonly isRtl   = computed(() => this.l10n.lang() === 'ar');

  // ── Filters ─────────────────────────────────────────────────────────────
  readonly search         = signal('');
  readonly moduleFilter   = signal<AuditModule | ''>('');
  readonly severityFilter = signal<AuditSeverity | ''>('');
  readonly dateFrom       = signal('');
  readonly dateTo         = signal('');

  // ── Pagination ───────────────────────────────────────────────────────────
  readonly page      = signal(1);
  readonly pageSize  = 10;
  readonly totalCount = signal(0);
  readonly totalPages = computed(() =>
    Math.max(1, Math.ceil(this.totalCount() / this.pageSize))
  );

  // ── Drawer ───────────────────────────────────────────────────────────────
  readonly activeLog = signal<AuditLogEntry | null>(null);

  // ── Data ─────────────────────────────────────────────────────────────────
  readonly pagedLogs = signal<AuditLogEntry[]>([]);
  readonly loading   = signal(false);
  readonly error     = signal(false);

  readonly summary = signal<AuditLogSummary>({
    total: 0, today: 0, critical: 0, adminActions: 0,
    infoCount: 0, successCount: 0, warningCount: 0, criticalCount: 0
  });

  readonly modules:    AuditModule[]   = ['Users', 'Reviews', 'AI Operations', 'Settings', 'System'];
  readonly severities: AuditSeverity[] = ['Info', 'Success', 'Warning'];

  ngOnInit(): void { this.loadLogs(); }

  loadLogs(): void {
    this.loading.set(true);
    this.error.set(false);
    this.adminSvc.getAuditLogs({
      search:   this.search()         || undefined,
      module:   this.moduleFilter()   || undefined,
      severity: this.severityFilter() || undefined,
      dateFrom: this.dateFrom()       || undefined,
      dateTo:   this.dateTo()         || undefined,
      page:     this.page(),
      pageSize: this.pageSize,
    }).subscribe({
      next: result => {
        this.pagedLogs.set(result.items as AuditLogEntry[]);
        this.totalCount.set(result.totalCount);
        this.updateSummary(result.items as AuditLogEntry[]);
        this.loading.set(false);
      },
      error: () => { this.error.set(true); this.loading.set(false); }
    });
  }

  private updateSummary(logs: AuditLogEntry[]): void {
    const today = new Date(); today.setHours(0, 0, 0, 0);
    this.summary.set({
      total:         this.totalCount(),
      today:         logs.filter(l => new Date(l.timestamp) >= today).length,
      critical:      logs.filter(l => l.severity === 'Critical').length,
      adminActions:  this.totalCount(),
      infoCount:     logs.filter(l => l.severity === 'Info').length,
      successCount:  logs.filter(l => l.severity === 'Success').length,
      warningCount:  logs.filter(l => l.severity === 'Warning').length,
      criticalCount: logs.filter(l => l.severity === 'Critical').length,
    });
  }

  // ── Filter actions ───────────────────────────────────────────────────────
  setSearch(v: string):   void { this.search.set(v);                               this.page.set(1); this.loadLogs(); }
  setModule(v: string):   void { this.moduleFilter.set(v as AuditModule | '');     this.page.set(1); this.loadLogs(); }
  setSeverity(v: string): void { this.severityFilter.set(v as AuditSeverity | ''); this.page.set(1); this.loadLogs(); }
  setDateFrom(v: string): void { this.dateFrom.set(v);                             this.page.set(1); this.loadLogs(); }
  setDateTo(v: string):   void { this.dateTo.set(v);                               this.page.set(1); this.loadLogs(); }

  clearFilters(): void {
    this.search.set(''); this.moduleFilter.set(''); this.severityFilter.set('');
    this.dateFrom.set(''); this.dateTo.set(''); this.page.set(1);
    this.loadLogs();
  }

  hasActiveFilters(): boolean {
    return !!(this.search() || this.moduleFilter() || this.severityFilter() || this.dateFrom() || this.dateTo());
  }

  goToPage(p: number): void {
    if (p < 1 || p > this.totalPages()) return;
    this.page.set(p);
    this.loadLogs();
  }

  // ── Drawer ───────────────────────────────────────────────────────────────
  openDrawer(log: AuditLogEntry): void { this.activeLog.set(log); }
  closeDrawer(): void                  { this.activeLog.set(null); }

  @HostListener('document:keydown.escape')
  onEscape(): void { this.closeDrawer(); }

  // ── Label helpers ────────────────────────────────────────────────────────
  moduleLabel(m: AuditModule): string {
    const c = this.copy();
    return ({ Users: c.moduleUsers, Reviews: c.moduleReviews,
              'AI Operations': c.moduleAiOps, Settings: c.moduleSettings,
              System: c.moduleSystem } as Record<AuditModule, string>)[m] ?? m;
  }

  severityLabel(s: AuditSeverity): string {
    const c = this.copy();
    return ({ Info: c.severityInfo, Success: c.severitySuccess,
              Warning: c.severityWarning, Critical: c.severityCritical
            } as Record<AuditSeverity, string>)[s] ?? s;
  }

  actionLabel(action: string): string {
    const c = this.copy();
    const map: Record<string, string> = {
      UserActivated:      c.actionUserActivated,
      UserSuspended:      c.actionUserSuspended,
      RoleChanged:        c.actionRoleChanged,
      FeedbackReviewed:   c.actionFeedbackReviewed,
      FeedbackResolved:   c.actionFeedbackResolved,
      ReviewHidden:       c.actionReviewHidden,
      ReviewPublished:    c.actionReviewPublished,
      MaintenanceEnabled: c.actionMaintenanceEnabled,
      ConfigChanged:      c.actionConfigChanged,
    };
    return map[action] ?? action;
  }

  moduleIcon(m: AuditModule): string {
    return ({ Users: 'fa-users', Reviews: 'fa-star',
              'AI Operations': 'fa-robot', Settings: 'fa-gear',
              System: 'fa-server' } as Record<AuditModule, string>)[m] ?? 'fa-circle';
  }

  formatTime(iso: string): string {
    return new Intl.DateTimeFormat(
      this.l10n.lang() === 'ar' ? 'ar-EG' : 'en-US',
      { month: 'short', day: 'numeric', hour: '2-digit', minute: '2-digit' }
    ).format(new Date(iso));
  }

  formatFullDate(iso: string): string {
    return new Intl.DateTimeFormat(
      this.l10n.lang() === 'ar' ? 'ar-EG' : 'en-US',
      { month: 'long', day: 'numeric', year: 'numeric',
        hour: '2-digit', minute: '2-digit', second: '2-digit' }
    ).format(new Date(iso));
  }
}
