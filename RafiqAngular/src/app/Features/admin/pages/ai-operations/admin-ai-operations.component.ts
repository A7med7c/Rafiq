import { CommonModule } from '@angular/common';
import { FormsModule } from '@angular/forms';
import {
  ChangeDetectionStrategy, Component, HostListener, OnInit,
  computed, inject, signal
} from '@angular/core';
import { LocalizationService } from '../../../../Services/localization.service';
import { adminCopy } from '../../admin-copy';
import {
  AiFeedbackItem, AiFeedbackQuery,
  AiInsights, AiOverview, AiPerformance,
  AiRequestDetail, AiRequestItem, AiRequestQuery,
  AdminDistributionItem,
  TakeUsageActionRequest,
  UpdateAiFeedbackRequest,
  UsageAttentionUser,
  UsageIntelligenceOverview,
  UsageUserDetail
} from '../../models/admin.models';
import { AdminService } from '../../services/admin.service';

type Tab = 'overview' | 'conversations' | 'feedback' | 'performance' | 'usageIntelligence';

// Predefined category values (must match backend validation)
const FEEDBACK_CATEGORIES = [
  'Hallucination', 'WrongMedicalAdvice', 'OcrIssue',
  'VoiceRecognition', 'Performance', 'Bug', 'FeatureRequest', 'Other'
] as const;
type FeedbackCategory = typeof FEEDBACK_CATEGORIES[number];

interface KpiCard {
  label: string;
  value: number | string;
  icon: string;
  color: string;
  bg: string;
  suffix?: string;
  highlight?: boolean;
}

@Component({
  selector: 'app-admin-ai-operations',
  standalone: true,
  imports: [CommonModule, FormsModule],
  templateUrl: './admin-ai-operations.component.html',
  styleUrl: './admin-ai-operations.component.css',
  changeDetection: ChangeDetectionStrategy.OnPush
})
export class AdminAiOperationsComponent implements OnInit {
  private readonly adminService = inject(AdminService);
  protected readonly l10n        = inject(LocalizationService);
  readonly copy = computed(() => adminCopy[this.l10n.lang()].aiOps);
  readonly Math = Math;
  readonly CATEGORIES = FEEDBACK_CATEGORIES;

  // ── Tab state ─────────────────────────────────────────────────────────────
  readonly activeTab  = signal<Tab>('overview');
  readonly loadedTabs = signal(new Set<Tab>());

  // ── Overview ──────────────────────────────────────────────────────────────
  readonly overview        = signal<AiOverview | null>(null);
  readonly overviewLoading = signal(false);
  readonly overviewError   = signal(false);

  readonly healthLabel = computed(() => {
    const score = this.overview()?.aiHealthScore ?? 0;
    const c = this.copy();
    if (score >= 85) return c.healthGood;
    if (score >= 70) return c.healthFair;
    return c.healthPoor;
  });

  readonly healthColor = computed(() => {
    const score = this.overview()?.aiHealthScore ?? 0;
    if (score >= 85) return '#10b981';
    if (score >= 70) return '#f59e0b';
    return '#ef4444';
  });

  readonly healthBg = computed(() => {
    const score = this.overview()?.aiHealthScore ?? 0;
    if (score >= 85) return '#ecfdf5';
    if (score >= 70) return '#fffbeb';
    return '#fef2f2';
  });

  // ── Insights ──────────────────────────────────────────────────────────────
  readonly insights        = signal<AiInsights | null>(null);
  readonly insightsLoading = signal(false);
  readonly insightsError   = signal(false);

  // ── Incidents — computed from overview thresholds ─────────────────────────
  readonly incidents = computed(() => {
    const o = this.overview();
    if (!o) return [];
    const list: { icon: string; title: string; desc: string; severity: 'critical' | 'warning' }[] = [];
    if (o.failedLast7Days > 20)
      list.push({ icon: 'fa-circle-xmark',          title: this.copy().incidentHighError,      desc: this.copy().incidentHighErrorDesc,      severity: 'critical' });
    if (o.positiveRatePercent < 60)
      list.push({ icon: 'fa-thumbs-down',            title: this.copy().incidentLowRating,       desc: this.copy().incidentLowRatingDesc,       severity: 'critical' });
    if (o.unreviewedNegative > 10)
      list.push({ icon: 'fa-bell',                   title: this.copy().incidentPendingReviews,  desc: this.copy().incidentPendingReviewsDesc,  severity: 'warning' });
    if (o.avgResponseTimeMs > 10000)
      list.push({ icon: 'fa-gauge-simple-high',      title: this.copy().incidentSlowResponse,    desc: this.copy().incidentSlowResponseDesc,    severity: 'warning' });
    return list;
  });

  readonly kpiCards = computed((): KpiCard[] => {
    const o = this.overview();
    const c = this.copy();
    if (!o) return [];
    return [
      { label: c.positiveRate,       value: o.positiveRatePercent,  icon: 'fa-thumbs-up',        color: '#10b981', bg: '#ecfdf5', suffix: '%' },
      { label: c.unreviewedNegative, value: o.unreviewedNegative,   icon: 'fa-thumbs-down',      color: '#ef4444', bg: '#fef2f2' },
      { label: c.failedLast7d,       value: o.failedLast7Days,      icon: 'fa-circle-xmark',     color: '#f97316', bg: '#fff7ed' },
      { label: c.avgResponseTime,    value: o.avgResponseTimeMs,    icon: 'fa-gauge',             color: '#3b82f6', bg: '#eff6ff', suffix: ' ms' },
      { label: c.requestsToday,      value: o.requestsToday,        icon: 'fa-calendar-day',     color: '#8b5cf6', bg: '#f5f3ff' },
      { label: c.chatConversations,  value: o.totalChatConversations, icon: 'fa-comments',        color: '#0ea5e9', bg: '#f0f9ff' },
      { label: c.voiceSessions,      value: o.totalVoiceSessions,   icon: 'fa-microphone',       color: '#a855f7', bg: '#faf5ff' },
    ];
  });

  // ── Conversations ─────────────────────────────────────────────────────────
  readonly requests        = signal<AiRequestItem[]>([]);
  readonly requestsTotal   = signal(0);
  readonly requestsPage    = signal(1);
  readonly requestsTotalPages = signal(0);
  readonly requestsLoading = signal(false);
  readonly requestsError   = signal(false);
  readonly requestsQuery   = signal<AiRequestQuery>({
    search: '', feature: '', status: '', sortBy: 'createdAt', sortDirection: 'desc', page: 1, pageSize: 20
  });

  // Drawer
  readonly drawerOpen    = signal(false);
  readonly drawerLoading = signal(false);
  readonly drawerError   = signal(false);
  readonly requestDetail = signal<AiRequestDetail | null>(null);

  // ── Feedback ──────────────────────────────────────────────────────────────
  readonly feedback        = signal<AiFeedbackItem[]>([]);
  readonly feedbackTotal   = signal(0);
  readonly feedbackPage    = signal(1);
  readonly feedbackTotalPages = signal(0);
  readonly feedbackLoading = signal(false);
  readonly feedbackError   = signal(false);
  readonly feedbackQuery   = signal<AiFeedbackQuery>({
    search: '', reaction: '', status: '', category: '', page: 1, pageSize: 20
  });
  readonly feedbackDateFrom  = signal('');
  readonly feedbackDateTo    = signal('');
  readonly feedbackModalOpen = signal(false);
  readonly feedbackModalItem = signal<AiFeedbackItem | null>(null);
  readonly savingFeedback   = signal<Record<string, boolean>>({});

  // Per-item triage state (local edits before save)
  readonly triageEdits = signal<Record<string, { status: string; category: string; notes: string }>>({});

  // ── Performance ───────────────────────────────────────────────────────────
  readonly performance        = signal<AiPerformance | null>(null);
  readonly performanceLoading = signal(false);
  readonly performanceError   = signal(false);

  // ── Usage Intelligence ────────────────────────────────────────────────────
  readonly uiOverview        = signal<UsageIntelligenceOverview | null>(null);
  readonly uiOverviewLoading = signal(false);
  readonly uiOverviewError   = signal(false);

  readonly uiQueue        = signal<UsageAttentionUser[]>([]);
  readonly uiQueueTotal   = signal(0);
  readonly uiQueuePage    = signal(1);
  readonly uiQueueTotalPages = signal(0);
  readonly uiQueueLoading = signal(false);
  readonly uiQueueError   = signal(false);

  readonly uiSelectedUser  = signal<UsageUserDetail | null>(null);
  readonly uiDetailLoading = signal(false);
  readonly uiDetailError   = signal(false);

  readonly uiActionNotes    = signal('');
  readonly uiActioning      = signal(false);
  readonly uiActionMsg      = signal('');
  readonly uiActionMsgOk    = signal(true);
  readonly uiConfirmAction  = signal('');  // the action type being confirmed

  ngOnInit(): void {
    this.loadOverview();
    this.loadInsights();
  }

  // ── Tab switching ─────────────────────────────────────────────────────────
  switchTab(tab: Tab): void {
    this.activeTab.set(tab);
    const loaded = this.loadedTabs();
    if (!loaded.has(tab)) {
      loaded.add(tab);
      this.loadedTabs.set(new Set(loaded));
      if (tab === 'conversations')      this.loadRequests();
      if (tab === 'feedback')           this.loadFeedback();
      if (tab === 'performance')        this.loadPerformance();
      if (tab === 'usageIntelligence')  { this.loadUiOverview(); this.loadUiQueue(); }
    }
  }

  // ── Overview ──────────────────────────────────────────────────────────────
  loadOverview(): void {
    this.overviewLoading.set(true);
    this.overviewError.set(false);
    this.adminService.getAiOverview().subscribe({
      next: data => { this.overview.set(data); this.overviewLoading.set(false); },
      error: ()   => { this.overviewError.set(true); this.overviewLoading.set(false); }
    });
  }

  loadInsights(): void {
    this.insightsLoading.set(true);
    this.insightsError.set(false);
    this.adminService.getAiInsights().subscribe({
      next: data => { this.insights.set(data); this.insightsLoading.set(false); },
      error: ()   => { this.insightsError.set(true); this.insightsLoading.set(false); }
    });
  }

  // ── Conversations ─────────────────────────────────────────────────────────
  loadRequests(): void {
    this.requestsLoading.set(true);
    this.requestsError.set(false);
    this.adminService.getAiRequests(this.requestsQuery()).subscribe({
      next: data => {
        this.requests.set(data.items);
        this.requestsTotal.set(data.totalCount);
        this.requestsPage.set(data.page);
        this.requestsTotalPages.set(data.totalPages);
        this.requestsLoading.set(false);
      },
      error: () => { this.requestsError.set(true); this.requestsLoading.set(false); }
    });
  }

  onRequestSearch(v: string)         { this.requestsQuery.update(q => ({ ...q, search: v, page: 1 })); this.loadRequests(); }
  onRequestFeatureChange(v: string)  { this.requestsQuery.update(q => ({ ...q, feature: v, page: 1 })); this.loadRequests(); }
  onRequestStatusChange(v: string)   { this.requestsQuery.update(q => ({ ...q, status: v as never, page: 1 })); this.loadRequests(); }
  prevRequestsPage()                 { if (this.requestsPage() <= 1) return; this.requestsQuery.update(q => ({ ...q, page: this.requestsPage() - 1 })); this.loadRequests(); }
  nextRequestsPage()                 { if (this.requestsPage() >= this.requestsTotalPages()) return; this.requestsQuery.update(q => ({ ...q, page: this.requestsPage() + 1 })); this.loadRequests(); }

  openDrawer(row: AiRequestItem): void {
    this.drawerOpen.set(true);
    this.drawerLoading.set(true);
    this.drawerError.set(false);
    this.requestDetail.set(null);
    this.adminService.getAiRequestDetail(row.id).subscribe({
      next: detail => { this.requestDetail.set(detail); this.drawerLoading.set(false); },
      error: ()     => { this.drawerError.set(true); this.drawerLoading.set(false); }
    });
  }

  closeDrawer(): void {
    this.drawerOpen.set(false);
    this.requestDetail.set(null);
  }

  @HostListener('keydown.escape')
  onEsc(): void {
    if (this.drawerOpen()) this.closeDrawer();
    if (this.feedbackModalOpen()) this.closeFeedbackModal();
  }

  // ── Feedback ──────────────────────────────────────────────────────────────
  loadFeedback(): void {
    this.feedbackLoading.set(true);
    this.feedbackError.set(false);
    const q: AiFeedbackQuery = {
      ...this.feedbackQuery(),
      dateFrom: this.feedbackDateFrom() || undefined,
      dateTo:   this.feedbackDateTo()   || undefined
    };
    this.adminService.getAiFeedback(q).subscribe({
      next: data => {
        this.feedback.set(data.items);
        this.feedbackTotal.set(data.totalCount);
        this.feedbackPage.set(data.page);
        this.feedbackTotalPages.set(data.totalPages);
        this.feedbackLoading.set(false);
        // Seed local triage edits from server state
        const edits: Record<string, { status: string; category: string; notes: string }> = {};
        for (const item of data.items) {
          edits[item.id] = {
            status:   item.triageStatus,
            category: item.category ?? '',
            notes:    item.adminNotes ?? ''
          };
        }
        this.triageEdits.set(edits);
      },
      error: () => { this.feedbackError.set(true); this.feedbackLoading.set(false); }
    });
  }

  onFeedbackSearch(v: string)         { this.feedbackQuery.update(q => ({ ...q, search: v, page: 1 })); this.loadFeedback(); }
  onFeedbackReactionChange(v: string) { this.feedbackQuery.update(q => ({ ...q, reaction: v as never, page: 1 })); this.loadFeedback(); }
  onFeedbackStatusChange(v: string)   { this.feedbackQuery.update(q => ({ ...q, status: v, page: 1 })); this.loadFeedback(); }
  onFeedbackCategoryChange(v: string) { this.feedbackQuery.update(q => ({ ...q, category: v, page: 1 })); this.loadFeedback(); }
  onDateFilterChange()                { this.feedbackQuery.update(q => ({ ...q, page: 1 })); this.loadFeedback(); }
  prevFeedbackPage()                  { if (this.feedbackPage() <= 1) return; this.feedbackQuery.update(q => ({ ...q, page: this.feedbackPage() - 1 })); this.loadFeedback(); }
  nextFeedbackPage()                  { if (this.feedbackPage() >= this.feedbackTotalPages()) return; this.feedbackQuery.update(q => ({ ...q, page: this.feedbackPage() + 1 })); this.loadFeedback(); }

  openFeedbackModal(item: AiFeedbackItem): void {
    this.feedbackModalItem.set(item);
    this.feedbackModalOpen.set(true);
    // Seed the triage edit state if not already there
    if (!this.triageEdits()[item.id]) {
      this.updateTriageEdit(item.id, 'status', item.triageStatus);
      this.updateTriageEdit(item.id, 'category', item.category ?? '');
      this.updateTriageEdit(item.id, 'notes', item.adminNotes ?? '');
    }
  }

  closeFeedbackModal(): void {
    this.feedbackModalOpen.set(false);
    this.feedbackModalItem.set(null);
  }

  updateTriageEdit(id: string, field: 'status' | 'category' | 'notes', value: string): void {
    const current = this.triageEdits()[id] ?? { status: 'New', category: '', notes: '' };
    this.triageEdits.update(e => ({ ...e, [id]: { ...current, [field]: value } }));
  }

  saveReview(item: AiFeedbackItem): void {
    const edit = this.triageEdits()[item.id];
    if (!edit) return;

    this.savingFeedback.update(s => ({ ...s, [item.id]: true }));

    const body: UpdateAiFeedbackRequest = {
      triageStatus: edit.status,
      category:    edit.category || null,
      adminNotes:  edit.notes    || null
    };

    this.adminService.updateAiFeedback(item.id, body).subscribe({
      next: () => {
        this.savingFeedback.update(s => ({ ...s, [item.id]: false }));
        this.feedback.update(list =>
          list.map(f => f.id === item.id
            ? { ...f, triageStatus: edit.status, category: edit.category || null, adminNotes: edit.notes || null }
            : f)
        );
        this.closeFeedbackModal();
      },
      error: () => this.savingFeedback.update(s => ({ ...s, [item.id]: false }))
    });
  }

  // ── Usage Intelligence ────────────────────────────────────────────────────
  loadUiOverview(): void {
    this.uiOverviewLoading.set(true);
    this.uiOverviewError.set(false);
    this.adminService.getUsageIntelligenceOverview().subscribe({
      next: data => { this.uiOverview.set(data); this.uiOverviewLoading.set(false); },
      error: ()   => { this.uiOverviewError.set(true); this.uiOverviewLoading.set(false); }
    });
  }

  loadUiQueue(): void {
    this.uiQueueLoading.set(true);
    this.uiQueueError.set(false);
    this.adminService.getUsageAttentionQueue(this.uiQueuePage(), 20).subscribe({
      next: data => {
        this.uiQueue.set(data.items);
        this.uiQueueTotal.set(data.totalCount);
        this.uiQueueTotalPages.set(data.totalPages);
        this.uiQueueLoading.set(false);
      },
      error: () => { this.uiQueueError.set(true); this.uiQueueLoading.set(false); }
    });
  }

  prevUiQueuePage(): void {
    if (this.uiQueuePage() <= 1) return;
    this.uiQueuePage.update(p => p - 1);
    this.loadUiQueue();
  }

  nextUiQueuePage(): void {
    if (this.uiQueuePage() >= this.uiQueueTotalPages()) return;
    this.uiQueuePage.update(p => p + 1);
    this.loadUiQueue();
  }

  openUserDetail(user: UsageAttentionUser): void {
    this.uiSelectedUser.set(null);
    this.uiDetailLoading.set(true);
    this.uiDetailError.set(false);
    this.uiActionNotes.set('');
    this.uiActionMsg.set('');
    this.uiConfirmAction.set('');
    this.adminService.getUsageUserDetail(user.userId).subscribe({
      next: detail => { this.uiSelectedUser.set(detail); this.uiDetailLoading.set(false); },
      error: ()     => { this.uiDetailError.set(true); this.uiDetailLoading.set(false); }
    });
  }

  backToQueue(): void {
    this.uiSelectedUser.set(null);
    this.uiActionMsg.set('');
    this.uiConfirmAction.set('');
  }

  confirmAdminAction(actionType: string): void {
    this.uiConfirmAction.set(actionType);
    this.uiActionMsg.set('');
  }

  cancelAdminAction(): void {
    this.uiConfirmAction.set('');
    this.uiActionNotes.set('');
  }

  submitAdminAction(): void {
    const user   = this.uiSelectedUser();
    const action = this.uiConfirmAction();
    if (!user || !action) return;

    this.uiActioning.set(true);
    this.uiActionMsg.set('');

    const body: TakeUsageActionRequest = { actionType: action, notes: this.uiActionNotes() || null };

    this.adminService.takeUsageAction(user.userId, body).subscribe({
      next: () => {
        this.uiActioning.set(false);
        this.uiActionMsgOk.set(true);
        this.uiActionMsg.set(this.copy().uiActionSuccess);
        this.uiConfirmAction.set('');
        this.uiActionNotes.set('');
        // Refresh detail and queue
        this.adminService.getUsageUserDetail(user.userId).subscribe({
          next: d => this.uiSelectedUser.set(d)
        });
        this.loadUiOverview();
        this.loadUiQueue();
      },
      error: () => {
        this.uiActioning.set(false);
        this.uiActionMsgOk.set(false);
        this.uiActionMsg.set(this.copy().uiActionError);
      }
    });
  }

  classificationLabel(cls: string): string {
    const c = this.copy();
    const map: Record<string, string> = {
      NonMedicalUpload:   c.uiClassNonMedical,
      WrongDocumentType:  c.uiClassWrongType,
      OffDomainChat:      c.uiClassOffDomainChat,
      OffDomainVoice:     c.uiClassOffDomainVoice,
      PolicyViolation:    c.uiClassPolicyViolation,
    };
    return map[cls] ?? cls;
  }

  classificationPillClass(cls: string): string {
    return cls === 'PolicyViolation'   ? 'pill--red'
         : cls === 'NonMedicalUpload'  ? 'pill--yellow'
         : cls === 'WrongDocumentType' ? 'pill--orange'
         : cls === 'OffDomainChat'     ? 'pill--blue'
         : cls === 'OffDomainVoice'    ? 'pill--blue'
         : 'pill--muted';
  }

  actionLabel(actionType: string): string {
    const c = this.copy();
    const map: Record<string, string> = {
      Warning:                   c.uiActionWarning,
      FinalWarning:              c.uiActionFinalWarning,
      RestrictAi:                c.uiActionRestrictAi,
      RemoveAiRestriction:       c.uiActionRemoveAiRestriction,
      RestrictAccount:           c.uiActionRestrictAccount,
      RemoveAccountRestriction:  c.uiActionRemoveAccountRestriction,
      SuspendAccount:            c.uiActionSuspend,
      UnsuspendAccount:          c.uiActionUnsuspend,
    };
    return map[actionType] ?? actionType;
  }

  actionPillClass(actionType: string): string {
    return actionType === 'SuspendAccount'           ? 'pill--red'
         : actionType === 'FinalWarning'             ? 'pill--yellow'
         : actionType === 'RestrictAi'               ? 'pill--orange'
         : actionType === 'RestrictAccount'          ? 'pill--red'
         : actionType === 'RemoveAiRestriction'      ? 'pill--green'
         : actionType === 'RemoveAccountRestriction' ? 'pill--green'
         : actionType === 'UnsuspendAccount'         ? 'pill--green'
         : 'pill--muted';
  }

  // ── Performance ───────────────────────────────────────────────────────────
  loadPerformance(): void {
    this.performanceLoading.set(true);
    this.performanceError.set(false);
    this.adminService.getAiPerformance(30).subscribe({
      next: data => { this.performance.set(data); this.performanceLoading.set(false); },
      error: ()   => { this.performanceError.set(true); this.performanceLoading.set(false); }
    });
  }

  // ── Chart helpers ─────────────────────────────────────────────────────────
  lineChartPath(points: { value: number }[], W = 560, H = 100): string {
    if (!points || points.length < 2) return '';
    const vals = points.map(p => p.value);
    const min  = Math.min(...vals);
    const max  = Math.max(...vals) || 1;
    const xs   = points.map((_, i) => (i / (points.length - 1)) * W);
    const ys   = vals.map(v => H - ((v - min) / (max - min || 1)) * (H - 8) - 4);
    let d = `M${xs[0].toFixed(1)},${ys[0].toFixed(1)}`;
    for (let i = 1; i < xs.length; i++) {
      const cpx = (xs[i - 1] + xs[i]) / 2;
      d += ` C${cpx.toFixed(1)},${ys[i - 1].toFixed(1)} ${cpx.toFixed(1)},${ys[i].toFixed(1)} ${xs[i].toFixed(1)},${ys[i].toFixed(1)}`;
    }
    return d;
  }

  lineChartArea(points: { value: number }[], W = 560, H = 100): string {
    const line = this.lineChartPath(points, W, H);
    return line ? `${line} L${W},${H} L0,${H} Z` : '';
  }

  barHeight(value: number, points: { value: number }[]): number {
    const max = Math.max(...points.map(p => p.value), 1);
    return Math.max(4, Math.round((value / max) * 100));
  }

  donutSegments(items: AdminDistributionItem[]): { path: string; color: string; label: string; value: number; pct: number }[] {
    const total  = items.reduce((s, i) => s + i.value, 0) || 1;
    const colors = ['#3b82f6', '#8b5cf6', '#10b981', '#f59e0b', '#ef4444', '#0ea5e9', '#a855f7', '#f97316'];
    const R = 40; const cx = 50; const cy = 50;
    let startAngle = -90;
    return items.map((item, idx) => {
      const pct   = item.value / total;
      const sweep = pct * 360;
      const start = startAngle * (Math.PI / 180);
      const end   = (startAngle + sweep) * (Math.PI / 180);
      const x1 = cx + R * Math.cos(start); const y1 = cy + R * Math.sin(start);
      const x2 = cx + R * Math.cos(end);   const y2 = cy + R * Math.sin(end);
      const large = sweep > 180 ? 1 : 0;
      const path  = `M ${cx} ${cy} L ${x1.toFixed(2)} ${y1.toFixed(2)} A ${R} ${R} 0 ${large} 1 ${x2.toFixed(2)} ${y2.toFixed(2)} Z`;
      startAngle += sweep;
      return { path, color: colors[idx % colors.length], label: item.label, value: item.value, pct: Math.round(pct * 100) };
    });
  }

  // ── Utilities ─────────────────────────────────────────────────────────────
  formatDate(value: string): string {
    return new Intl.DateTimeFormat(
      this.l10n.lang() === 'ar' ? 'ar-EG' : 'en-US',
      { month: 'short', day: 'numeric', year: 'numeric', hour: 'numeric', minute: '2-digit' }
    ).format(new Date(value));
  }

  formatDateShort(value: string): string {
    return new Intl.DateTimeFormat(
      this.l10n.lang() === 'ar' ? 'ar-EG' : 'en-US',
      { month: 'short', day: 'numeric' }
    ).format(new Date(value));
  }

  featureLabel(f: string): string {
    const map: Record<string, string> = {
      Chat: 'Chat', Voice: 'Voice', LabOcr: 'Lab OCR',
      ImagingOcr: 'Imaging OCR', PrescriptionOcr: 'Prescription OCR',
      GeneralDocOcr: 'General Doc', MedicineScan: 'Medicine Scan',
      HealthSummary: 'Health Summary', MedicalReport: 'Medical Report'
    };
    return map[f] ?? f;
  }

  categoryLabel(cat: string | null | undefined): string {
    if (!cat) return this.copy().uncategorized;
    const map: Record<string, keyof ReturnType<typeof this.copy>> = {
      Hallucination:     'catHallucination',
      WrongMedicalAdvice:'catWrongMedical',
      OcrIssue:          'catOcrIssue',
      VoiceRecognition:  'catVoice',
      Performance:       'catPerformance',
      Bug:               'catBug',
      FeatureRequest:    'catFeatureRequest',
      Other:             'catOther'
    };
    const key = map[cat];
    return key ? (this.copy()[key] as string) : cat;
  }

  statusClass(s: string): string {
    return s === 'Resolved'      ? 'pill--green'
         : s === 'Investigating' ? 'pill--yellow'
         : s === 'Ignored'       ? 'pill--muted'
         : 'pill--blue'; // New
  }

  statusLabel(s: string): string {
    const map: Record<string, keyof ReturnType<typeof this.copy>> = {
      New:           'statusNew',
      Investigating: 'statusInvestigating',
      Resolved:      'statusResolved',
      Ignored:       'statusIgnored'
    };
    const key = map[s];
    return key ? (this.copy()[key] as string) : s;
  }

  triageEdit(id: string) {
    return this.triageEdits()[id] ?? { status: 'New', category: '', notes: '' };
  }
}
