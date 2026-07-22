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
import { AdminDashboard, AdminTrendPoint } from '../../models/admin.models';
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
  readonly dashboard = signal<AdminDashboard | null>(null);
  readonly loading = signal(true);
  readonly error = signal(false);

  readonly primaryMetrics = computed(() => {
    const data = this.dashboard();
    if (!data) return [];

    return [
      { label: this.copy().totalUsers, value: data.totalUsers, icon: 'fa-users', tone: 'cyan' },
      { label: this.copy().activeUsers, value: data.activeUsers, icon: 'fa-user-check', tone: 'green' },
      { label: this.copy().profiles, value: data.totalProfiles, icon: 'fa-heart-pulse', tone: 'blue' },
      { label: this.copy().appointmentsToday, value: data.appointmentsToday, icon: 'fa-calendar-day', tone: 'orange' }
    ];
  });

  readonly secondaryMetrics = computed(() => {
    const data = this.dashboard();
    if (!data) return [];

    return [
      { label: this.copy().monthlyAppointments, value: data.appointmentsThisMonth },
      { label: this.copy().pendingAppointments, value: data.pendingAppointments },
      { label: this.copy().completedAppointments, value: data.completedAppointments },
      { label: this.copy().remindersToday, value: data.medicationRemindersToday },
      { label: this.copy().documents, value: data.medicalDocuments },
      { label: this.copy().aiConversations, value: data.aiConversations }
    ];
  });

  ngOnInit(): void {
    this.loadDashboard();
  }

  loadDashboard(): void {
    this.loading.set(true);
    this.error.set(false);

    this.adminService.getDashboard().subscribe({
      next: data => {
        this.dashboard.set(data);
        this.loading.set(false);
      },
      error: () => {
        this.error.set(true);
        this.loading.set(false);
      }
    });
  }

  barHeight(point: AdminTrendPoint, points: AdminTrendPoint[]): number {
    const max = Math.max(...points.map(item => item.value), 1);
    return Math.max(8, Math.round(point.value / max * 100));
  }

  distributionWidth(value: number): number {
    const total = this.dashboard()?.genderDistribution
      .reduce((sum, item) => sum + item.value, 0) ?? 0;
    return total === 0 ? 0 : Math.round(value / total * 100);
  }

  avatarUrl(path: string | null | undefined): string {
    return path ? `${environment.fileBaseUrl}${path}` : 'images/user_avatar.png';
  }

  formatDate(value: string): string {
    return new Intl.DateTimeFormat(this.l10n.lang() === 'ar' ? 'ar-EG' : 'en-US', {
      month: 'short',
      day: 'numeric',
      year: 'numeric'
    }).format(new Date(value));
  }

  formatAppointment(value: string): string {
    return new Intl.DateTimeFormat(this.l10n.lang() === 'ar' ? 'ar-EG' : 'en-US', {
      month: 'short',
      day: 'numeric',
      hour: 'numeric',
      minute: '2-digit'
    }).format(new Date(value));
  }
}
