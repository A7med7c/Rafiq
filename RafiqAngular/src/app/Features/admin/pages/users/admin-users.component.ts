import { CommonModule } from '@angular/common';
import {
  ChangeDetectionStrategy,
  Component,
  DestroyRef,
  OnInit,
  computed,
  inject,
  signal,
  HostListener
} from '@angular/core';
import { FormsModule } from '@angular/forms';
import { Subject, debounceTime, distinctUntilChanged } from 'rxjs';
import { takeUntilDestroyed } from '@angular/core/rxjs-interop';
import { environment } from '../../../../Environments/Environment';
import { AuthService } from '../../../../Services/auth-service';
import { LocalizationService } from '../../../../Services/localization.service';
import { adminCopy } from '../../admin-copy';
import { AdminDashboard, AdminUser, AdminUserQuery, PagedResult } from '../../models/admin.models';
import { AdminService } from '../../services/admin.service';

@Component({
  selector: 'app-admin-users',
  standalone: true,
  imports: [CommonModule, FormsModule],
  templateUrl: './admin-users.component.html',
  styleUrl: './admin-users.component.css',
  changeDetection: ChangeDetectionStrategy.OnPush
})
export class AdminUsersComponent implements OnInit {
  private readonly adminService = inject(AdminService);
  private readonly authService = inject(AuthService);
  private readonly destroyRef = inject(DestroyRef);
  private readonly searchChanges = new Subject<string>();
  protected readonly l10n = inject(LocalizationService);

  readonly copy = computed(() => adminCopy[this.l10n.lang()].users);
  readonly result = signal<PagedResult<AdminUser> | null>(null);
  readonly dashboard = signal<AdminDashboard | null>(null);
  readonly loading = signal(true);
  readonly error = signal(false);
  readonly updatingUserId = signal<string | null>(null);
  readonly confirmUser = signal<AdminUser | null>(null);
  readonly openMenuId = signal<string | null>(null);

  search = '';
  status: AdminUserQuery['status'] = '';
  role: AdminUserQuery['role'] = '';
  sort = 'createdAt-desc';
  readonly pageSize = 20;

  readonly kpiMetrics = computed(() => {
    const data = this.dashboard();
    if (!data) return [];
    
    return [
      { label: this.copy().total, value: data.totalUsers, icon: 'fa-users', tone: 'cyan' },
      { label: this.copy().active, value: data.activeUsers, icon: 'fa-user-check', tone: 'green' },
      { label: this.copy().inactive, value: data.totalUsers - data.activeUsers, icon: 'fa-user-lock', tone: 'orange' },
      { label: 'New This Month', value: data.newRegistrationsThisMonth, icon: 'fa-user-plus', tone: 'purple' }
    ];
  });

  ngOnInit(): void {
    this.searchChanges
      .pipe(
        debounceTime(350),
        distinctUntilChanged(),
        takeUntilDestroyed(this.destroyRef)
      )
      .subscribe(() => this.loadUsers(1));

    this.loadDashboardData();
    this.loadUsers(1);
  }

  loadDashboardData(): void {
    this.adminService.getDashboard().subscribe({
      next: data => this.dashboard.set(data),
      error: () => console.error('Failed to load dashboard KPIs')
    });
  }

  onSearch(value: string): void {
    this.search = value;
    this.searchChanges.next(value.trim());
  }

  applyFilters(): void {
    this.loadUsers(1);
  }

  loadUsers(page: number): void {
    const [sortBy, sortDirection] = this.sort.split('-') as [
      AdminUserQuery['sortBy'],
      AdminUserQuery['sortDirection']
    ];

    this.loading.set(true);
    this.error.set(false);

    this.adminService.getUsers({
      search: this.search.trim(),
      status: this.status,
      role: this.role,
      sortBy,
      sortDirection,
      page,
      pageSize: this.pageSize
    }).subscribe({
      next: data => {
        this.result.set(data);
        this.loading.set(false);
      },
      error: () => {
        this.error.set(true);
        this.loading.set(false);
      }
    });
  }

  requestStatusChange(user: AdminUser): void {
    if (!user.isActive) {
      this.updateStatus(user, true);
      return;
    }

    this.confirmUser.set(user);
  }

  confirmDeactivation(): void {
    const user = this.confirmUser();
    if (!user) return;

    this.confirmUser.set(null);
    this.updateStatus(user, false);
  }

  toggleMenu(userId: string, event: Event): void {
    event.stopPropagation();
    if (this.openMenuId() === userId) {
      this.openMenuId.set(null);
    } else {
      this.openMenuId.set(userId);
    }
  }

  @HostListener('document:click')
  closeMenu(): void {
    if (this.openMenuId()) {
      this.openMenuId.set(null);
    }
  }

  isCurrentAdmin(user: AdminUser): boolean {
    return user.id === this.authService.currentUser?.userId;
  }

  avatarUrl(path: string | null | undefined): string {
    return path ? `${environment.fileBaseUrl}${path}` : 'images/user_avatar.png';
  }

  fullName(user: AdminUser): string {
    return `${user.firstName} ${user.lastName}`.trim();
  }

  formatDate(value: string): string {
    return new Intl.DateTimeFormat(this.l10n.lang() === 'ar' ? 'ar-EG' : 'en-US', {
      month: 'short',
      day: 'numeric',
      year: 'numeric'
    }).format(new Date(value));
  }

  private updateStatus(user: AdminUser, isActive: boolean): void {
    this.updatingUserId.set(user.id);

    this.adminService.setUserStatus(user.id, isActive).subscribe({
      next: () => {
        this.result.update(current => current
          ? {
              ...current,
              items: current.items.map(item =>
                item.id === user.id ? { ...item, isActive } : item)
            }
          : current);
        this.updatingUserId.set(null);
      },
      error: () => {
        this.error.set(true);
        this.updatingUserId.set(null);
      }
    });
  }
}
