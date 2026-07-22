import { CommonModule } from '@angular/common';
import {
  ChangeDetectionStrategy,
  Component,
  HostListener,
  computed,
  inject,
  signal
} from '@angular/core';
import { RouterLink, RouterLinkActive, RouterOutlet } from '@angular/router';
import { AuthService } from '../../../Services/auth-service';
import { LocalizationService } from '../../../Services/localization.service';
import { NotificationService } from '../../../Services/notification.service';
import { adminCopy } from '../admin-copy';

interface AdminNavItem {
  path: string;
  icon: string;
  label: keyof typeof adminCopy.en.nav;
}

@Component({
  selector: 'app-admin-layout',
  standalone: true,
  imports: [CommonModule, RouterLink, RouterLinkActive, RouterOutlet],
  templateUrl: './admin-layout.component.html',
  styleUrl: './admin-layout.component.css',
  changeDetection: ChangeDetectionStrategy.OnPush
})
export class AdminLayoutComponent {
  private readonly authService = inject(AuthService);
  protected readonly l10n = inject(LocalizationService);
  protected readonly notifications = inject(NotificationService);

  readonly sidebarOpen = signal(false);
  readonly profileOpen = signal(false);
  readonly darkMode = signal(localStorage.getItem('rafiq_admin_theme') === 'dark');
  readonly copy = computed(() => adminCopy[this.l10n.lang()]);

  readonly navigation: AdminNavItem[] = [
    { path: '/admin/dashboard', icon: 'fa-chart-pie', label: 'dashboard' },
    { path: '/admin/users', icon: 'fa-users', label: 'users' },
    { path: '/admin/doctors', icon: 'fa-user-doctor', label: 'doctors' },
    { path: '/admin/families', icon: 'fa-people-roof', label: 'families' },
    { path: '/admin/medical-records', icon: 'fa-notes-medical', label: 'medicalRecords' },
    { path: '/admin/appointments', icon: 'fa-calendar-check', label: 'appointments' },
    { path: '/admin/medications', icon: 'fa-capsules', label: 'medications' },
    { path: '/admin/notifications', icon: 'fa-bell', label: 'notifications' },
    { path: '/admin/documents', icon: 'fa-file-shield', label: 'documents' },
    { path: '/admin/ai-analysis', icon: 'fa-wand-magic-sparkles', label: 'aiAnalysis' },
    { path: '/admin/statistics', icon: 'fa-chart-line', label: 'statistics' },
    { path: '/admin/settings', icon: 'fa-sliders', label: 'settings' },
    { path: '/admin/audit-logs', icon: 'fa-clock-rotate-left', label: 'auditLogs' }
  ];

  get adminName(): string {
    const user = this.authService.currentUser;
    return user ? `${user.firstName} ${user.lastName}`.trim() : 'Rafiq Admin';
  }

  get adminEmail(): string {
    return this.authService.currentUser?.email ?? '';
  }

  get avatarUrl(): string {
    return this.authService.avatarUrl;
  }

  toggleTheme(): void {
    this.darkMode.update(value => !value);
    localStorage.setItem('rafiq_admin_theme', this.darkMode() ? 'dark' : 'light');
  }

  toggleLanguage(): void {
    this.l10n.toggle();
  }

  closeSidebar(): void {
    this.sidebarOpen.set(false);
  }

  logout(): void {
    this.profileOpen.set(false);
    this.authService.logout().subscribe();
  }

  @HostListener('document:keydown.escape')
  closeOverlays(): void {
    this.sidebarOpen.set(false);
    this.profileOpen.set(false);
  }
}
