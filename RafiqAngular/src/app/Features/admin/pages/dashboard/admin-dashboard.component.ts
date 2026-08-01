import { CommonModule } from '@angular/common';
import {
  ChangeDetectionStrategy,
  Component,
  OnInit,
  computed,
  inject,
  signal
} from '@angular/core';
import { RouterLink } from '@angular/router';
import { environment } from '../../../../Environments/Environment';
import { LocalizationService } from '../../../../Services/localization.service';
import { adminCopy } from '../../admin-copy';
import { AdminDashboard, AdminDistributionItem, AdminTrendPoint, ReviewStats } from '../../models/admin.models';
import { AdminService } from '../../services/admin.service';

@Component({
  selector: 'app-admin-dashboard',
  standalone: true,
  imports: [CommonModule, RouterLink],
  templateUrl: './admin-dashboard.component.html',
  styleUrl: './admin-dashboard.component.css',
  changeDetection: ChangeDetectionStrategy.OnPush
})
export class AdminDashboardComponent implements OnInit {
  private readonly adminService = inject(AdminService);
  protected readonly l10n = inject(LocalizationService);

  readonly copy = computed(() => adminCopy[this.l10n.lang()].dashboard);
  // Typed accessor for the new AI-related copy keys added to adminCopy but not yet in the inferred type
  readonly aiCopy = computed(() => {
    const c = adminCopy[this.l10n.lang()].dashboard as Record<string, string>;
    return {
      aiUsage:             c['aiUsage']             ?? 'AI Usage Overview',
      totalAiRequests:     c['totalAiRequests']     ?? 'Total AI requests',
      flaggedAiRequests:   c['flaggedAiRequests']   ?? 'Flagged AI requests',
      mostActiveUser:      c['mostActiveUser']      ?? 'Most active user',
      usersNeedAttention:  c['usersNeedAttention']  ?? 'Users needing attention',
      reason:              c['reason']              ?? 'Reason',
      actionView:          c['actionView']          ?? 'View user',
    };
  });
  readonly dashboard = signal<AdminDashboard | null>(null);
  readonly reviewStats = signal<ReviewStats | null>(null);
  readonly loading = signal(true);
  readonly error = signal(false);

  // KPI cards – only rows backed by real backend data
  readonly kpiRow1 = computed(() => {
    const d = this.dashboard();
    if (!d) return [];
    const g = this.copy().monthlyGrowth;
    return [
      {
        label: this.copy().totalUsers,
        value: d.totalUsers,
        icon: 'fa-users',
        color: '#3b82f6',
        bg: '#eff6ff',
        growth: d.monthlyGrowthPercent,
        suffix: g,
        sparkPoints: d.userGrowth
      },
      {
        label: this.copy().profiles,
        value: d.totalProfiles,
        icon: 'fa-heart-pulse',
        color: '#8b5cf6',
        bg: '#f5f3ff',
        growth: null,
        suffix: '',
        sparkPoints: [] as AdminTrendPoint[]
      },
      {
        label: this.copy().documents,
        value: d.medicalDocuments,
        icon: 'fa-file-medical',
        color: '#10b981',
        bg: '#ecfdf5',
        growth: null,
        suffix: '',
        sparkPoints: [] as AdminTrendPoint[]
      },
      {
        label: (this.copy() as any).totalAiRequests,
        value: d.totalAiRequests,
        icon: 'fa-robot',
        color: '#f59e0b',
        bg: '#fffbeb',
        growth: null,
        suffix: '',
        sparkPoints: [] as AdminTrendPoint[]
      }
    ];
  });

  readonly kpiRow2 = computed(() => {
    const d = this.dashboard();
    if (!d) return [];
    return [
      {
        label: this.copy().activeUsers,
        value: d.activeUsers,
        icon: 'fa-user-check',
        color: '#0ea5e9',
        bg: '#f0f9ff',
        growth: null,
        suffix: '',
        sparkPoints: [] as AdminTrendPoint[]
      },
      {
        label: this.copy().aiConversations,
        value: d.aiConversations,
        icon: 'fa-robot',
        color: '#a855f7',
        bg: '#faf5ff',
        growth: null,
        suffix: '',
        sparkPoints: [] as AdminTrendPoint[]
      },
      {
        label: (this.copy() as any).flaggedAiRequests,
        value: d.flaggedAiRequests,
        icon: 'fa-triangle-exclamation',
        color: '#ef4444',
        bg: '#fef2f2',
        growth: null,
        suffix: '',
        sparkPoints: [] as AdminTrendPoint[]
      },
      {
        label: this.copy().remindersToday,
        value: d.medicationRemindersToday,
        icon: 'fa-bell',
        color: '#14b8a6',
        bg: '#f0fdfa',
        growth: null,
        suffix: '',
        sparkPoints: [] as AdminTrendPoint[]
      }
    ];
  });

  ngOnInit(): void {
    this.loadDashboard();
    this.adminService.getReviewStats().subscribe({
      next: s => this.reviewStats.set(s)
    });
  }

  loadDashboard(): void {
    this.loading.set(true);
    this.error.set(false);
    this.adminService.getDashboard().subscribe({
      next: data => { this.dashboard.set(data); this.loading.set(false); },
      error: () => { this.error.set(true); this.loading.set(false); }
    });
  }

  // ── Sparkline helpers ─────────────────────────────────────────────────────
  sparkPath(points: AdminTrendPoint[]): string {
    if (!points || points.length < 2) return '';
    const W = 88, H = 32;
    const vals = points.map(p => p.value);
    const min = Math.min(...vals);
    const max = Math.max(...vals) || 1;
    const xs = points.map((_, i) => (i / (points.length - 1)) * W);
    const ys = vals.map(v => H - ((v - min) / (max - min || 1)) * H);
    return xs.map((x, i) => `${i === 0 ? 'M' : 'L'}${x.toFixed(1)},${ys[i].toFixed(1)}`).join(' ');
  }

  // ── Bar chart helpers ─────────────────────────────────────────────────────
  barHeight(point: AdminTrendPoint, points: AdminTrendPoint[]): number {
    const max = Math.max(...points.map(p => p.value), 1);
    return Math.max(4, Math.round((point.value / max) * 100));
  }

  // ── Donut chart helpers ───────────────────────────────────────────────────
  donutSegments(items: AdminDistributionItem[]): { path: string; color: string; label: string; value: number; pct: number }[] {
    const total = items.reduce((s, i) => s + i.value, 0) || 1;
    const colors = ['#3b82f6', '#10b981', '#f59e0b', '#ef4444', '#8b5cf6', '#0ea5e9'];
    const R = 40, cx = 50, cy = 50;
    let startAngle = -90;
    return items.map((item, idx) => {
      const pct = item.value / total;
      const sweep = pct * 360;
      const start = startAngle * (Math.PI / 180);
      const end = (startAngle + sweep) * (Math.PI / 180);
      const x1 = cx + R * Math.cos(start);
      const y1 = cy + R * Math.sin(start);
      const x2 = cx + R * Math.cos(end);
      const y2 = cy + R * Math.sin(end);
      const large = sweep > 180 ? 1 : 0;
      const path = `M ${cx} ${cy} L ${x1.toFixed(2)} ${y1.toFixed(2)} A ${R} ${R} 0 ${large} 1 ${x2.toFixed(2)} ${y2.toFixed(2)} Z`;
      startAngle += sweep;
      return { path, color: colors[idx % colors.length], label: item.label, value: item.value, pct: Math.round(pct * 100) };
    });
  }

  // ── SVG line chart (user growth) ─────────────────────────────────────────
  lineChartPath(points: AdminTrendPoint[], W = 380, H = 120): string {
    if (!points || points.length < 2) return '';
    const vals = points.map(p => p.value);
    const min = Math.min(...vals);
    const max = Math.max(...vals) || 1;
    const xs = points.map((_, i) => (i / (points.length - 1)) * W);
    const ys = vals.map(v => H - ((v - min) / (max - min || 1)) * (H - 8) - 4);
    // Smooth curve via cubic bezier
    let d = `M${xs[0].toFixed(1)},${ys[0].toFixed(1)}`;
    for (let i = 1; i < xs.length; i++) {
      const cpx = (xs[i - 1] + xs[i]) / 2;
      d += ` C${cpx.toFixed(1)},${ys[i - 1].toFixed(1)} ${cpx.toFixed(1)},${ys[i].toFixed(1)} ${xs[i].toFixed(1)},${ys[i].toFixed(1)}`;
    }
    return d;
  }

  lineChartArea(points: AdminTrendPoint[], W = 380, H = 120): string {
    const line = this.lineChartPath(points, W, H);
    if (!line) return '';
    return `${line} L${W},${H} L0,${H} Z`;
  }

  lineYLabels(points: AdminTrendPoint[], steps = 4): string[] {
    if (!points || !points.length) return [];
    const vals = points.map(p => p.value);
    const min = Math.min(...vals);
    const max = Math.max(...vals) || 1;
    const labels: string[] = [];
    for (let i = steps; i >= 0; i--) {
      const v = min + ((max - min) / steps) * i;
      labels.push(v >= 1000 ? `${(v / 1000).toFixed(1)}k` : String(Math.round(v)));
    }
    return labels;
  }

  // ── Utilities ─────────────────────────────────────────────────────────────
  avatarUrl(path: string | null | undefined): string {
    return path ? `${environment.fileBaseUrl}${path}` : 'images/user_avatar.png';
  }

  formatDate(value: string): string {
    return new Intl.DateTimeFormat(this.l10n.lang() === 'ar' ? 'ar-EG' : 'en-US', {
      month: 'short', day: 'numeric', year: 'numeric'
    }).format(new Date(value));
  }

  formatAppointment(value: string): string {
    return new Intl.DateTimeFormat(this.l10n.lang() === 'ar' ? 'ar-EG' : 'en-US', {
      month: 'short', day: 'numeric', hour: 'numeric', minute: '2-digit'
    }).format(new Date(value));
  }

  today(): string {
    return new Intl.DateTimeFormat(this.l10n.lang() === 'ar' ? 'ar-EG' : 'en-US', {
      month: 'short', day: 'numeric', year: 'numeric'
    }).format(new Date());
  }
}
