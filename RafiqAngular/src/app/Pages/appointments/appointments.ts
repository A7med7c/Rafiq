import {
  Component, inject, OnInit, OnDestroy, effect, signal, computed, HostListener,
} from '@angular/core';
import { CommonModule } from '@angular/common';
import { FormsModule } from '@angular/forms';
import { Router, RouterLink, RouterLinkActive, ActivatedRoute } from '@angular/router';
import { toSignal } from '@angular/core/rxjs-interop';
import { of, map, switchMap } from 'rxjs';
import { AuthService } from '../../Services/auth-service';
import { ProfileCacheService } from '../../Services/profile-cache.service';
import { AppointmentsService } from '../../Services/appointments.service';
import { LocalizationService } from '../../Services/localization.service';
import { NotificationService } from '../../Services/notification.service';
import { AiChatService } from '../../Services/ai-chat.service';
import { FamilyProfilesService, AccessibleProfileDto } from '../../Services/family-profiles.service';
import { ProfileSelectionService } from '../../Services/profile-selection.service';
import {
  AppointmentDto, AppointmentStatus, AppointmentType,
  CreateAppointmentRequest, UpdateAppointmentRequest,
  APPOINTMENT_TYPE_LABELS, APPOINTMENT_TYPE_ICONS,
} from '../../Modles/appointment.models';

type ApptTab = 'all' | 'upcoming' | 'completed' | 'cancelled';

interface Toast {
  id: number;
  message: string;
  type: 'success' | 'error';
}

interface ApptForm {
  appointmentType: AppointmentType | null;
  customType: string;
  title: string;
  provider: string;
  date: string;
  time: string;
  reminderOffsetMinutes: number | null;
  notes: string;
}

const blankForm = (): ApptForm => ({
  appointmentType: null,
  customType: '',
  title: '',
  provider: '',
  date: '',
  time: '',
  reminderOffsetMinutes: 30,
  notes: '',
});

@Component({
  selector: 'app-appointments',
  standalone: true,
  imports: [CommonModule, FormsModule, RouterLink, RouterLinkActive],
  templateUrl: './appointments.html',
  styleUrl: './appointments.css',
})
export class Appointments implements OnInit, OnDestroy {
  private readonly authSvc        = inject(AuthService);
  protected readonly profileCache = inject(ProfileCacheService);
  private readonly apptSvc   = inject(AppointmentsService);
  protected readonly notifSvc  = inject(NotificationService);
  private readonly router     = inject(Router);
  private readonly route      = inject(ActivatedRoute);
  protected readonly l10n     = inject(LocalizationService);
  protected readonly aiChatService = inject(AiChatService);
  protected readonly t        = this.l10n.t;
  private readonly fpSvc      = inject(FamilyProfilesService);
  private readonly profileSelectSvc = inject(ProfileSelectionService);

  // ── Family-profile read-only gate ────────────────────────────────────────
  // Uses profileId from query param when present (direct link from family-profiles),
  // otherwise falls back to the profile stored in localStorage by the
  // family-profiles page.  If the resolved profile's access role is 'Viewer'
  // all write actions are hidden.
  readonly viewingProfile = toSignal<AccessibleProfileDto | null>(
    this.route.queryParamMap.pipe(
      map(params => params.get('profileId') ?? this.profileSelectSvc.selectedProfileId),
      switchMap(profileId => {
        if (!profileId) return of(null);
        return this.fpSvc.getAccessible().pipe(
          map(profiles => profiles.find(p => p.userHealthProfileId === profileId) ?? null)
        );
      })
    ),
    { initialValue: null }
  );

  readonly fpReadOnly = computed(() => this.viewingProfile()?.accessRole === 'Viewer');

  // profileId override for data loading (populated from query param)
  private readonly fpProfileId = signal<string | null>(null);

  // ── Layout ──────────────────────────────────────────────────────────────
  readonly sidebarCollapsed  = signal(false);
  readonly mobileSidebarOpen = signal(false);
  readonly dropdownOpen      = signal(false);

  // ── Data ─────────────────────────────────────────────────────────────────
  readonly appointments = signal<AppointmentDto[]>([]);
  readonly loading      = signal(true);
  readonly loadError    = signal<string | null>(null);

  // ── Filters ──────────────────────────────────────────────────────────────
  readonly activeTab   = signal<ApptTab>('all');
  readonly searchQuery = signal('');
  readonly dateFrom    = signal('');
  readonly dateTo      = signal('');
  readonly sortBy      = signal<'recent' | 'oldest' | 'az' | 'za'>('recent');
  readonly currentPage = signal(1);
  readonly PAGE_SIZE    = 2;
  readonly tabDirection = signal<'left' | 'right'>('left');
  readonly tabAnimating = signal(false);
  readonly mobileTabMenuOpen = signal(false);

  // ── Action menus ─────────────────────────────────────────────────────────
  readonly openMenuId = signal<string | null>(null);

  // ── Add / Edit modal ──────────────────────────────────────────────────────
  readonly showAddModal = signal(false);
  readonly editingId    = signal<string | null>(null);
  readonly formStep     = signal<1 | 2>(1);
  // form fields as individual signals for clean ngModel binding
  readonly fType        = signal<AppointmentType | null>(null);
  readonly fCustomType  = signal('');
  readonly fTitle       = signal('');
  readonly fProvider    = signal('');
  readonly fDate        = signal('');
  readonly fTime        = signal('');
  readonly fReminder    = signal<number | null>(30);
  readonly fCustomReminder = signal<number | null>(null);
  readonly customReminderSelected = signal(false);
  readonly fNotes       = signal('');
  readonly formErrors   = signal<Record<string, string>>({});
  readonly submitting   = signal(false);

  // ── View modal ────────────────────────────────────────────────────────────
  readonly showViewModal = signal(false);
  readonly viewingAppt   = signal<AppointmentDto | null>(null);

  // ── Delete modal ──────────────────────────────────────────────────────────
  readonly showDeleteModal = signal(false);
  readonly deletingId      = signal<string | null>(null);
  readonly deleting        = signal(false);

  // ── Cancel modal ──────────────────────────────────────────────────────────
  readonly showCancelModal = signal(false);
  readonly cancellingId    = signal<string | null>(null);
  readonly cancelling      = signal(false);

  // ── Toasts ───────────────────────────────────────────────────────────────
  private toastSeq = 0;
  readonly toasts  = signal<Toast[]>([]);

  // ── Notification timer ────────────────────────────────────────────────────
  private notifTimer?: ReturnType<typeof setInterval>;
  private firedIds   = new Set<string>();

  // ── Expose enums / constants to template ─────────────────────────────────
  readonly AppointmentType   = AppointmentType;
  readonly AppointmentStatus = AppointmentStatus;
  readonly TYPE_LABELS       = APPOINTMENT_TYPE_LABELS;
  readonly TYPE_ICONS        = APPOINTMENT_TYPE_ICONS;
  readonly ALL_TYPES         = [
    AppointmentType.DoctorVisit,
    AppointmentType.LabTest,
    AppointmentType.Imaging,
    AppointmentType.Vaccination,
    AppointmentType.Dentist,
    AppointmentType.Therapy,
    AppointmentType.FollowUp,
    AppointmentType.Other,
  ] as const;

  readonly REMINDER_OPTIONS = [
    { value: 15,   label: '15 minutes before' },
    { value: 30,   label: '30 minutes before' },
    { value: 60,   label: '1 hour before' },
    { value: 120,  label: '2 hours before' },
    { value: 1440, label: '1 day before' },
  ];

  readonly canContinueType = computed(() =>
    !!this.fType() &&
    (this.fType() !== AppointmentType.Other || !!this.fCustomType().trim())
  );

  // ── Computed ──────────────────────────────────────────────────────────────
  readonly upcomingCount   = computed(() =>
    this.appointments().filter(a => a.status === AppointmentStatus.Upcoming).length
  );
  readonly completedCount  = computed(() =>
    this.appointments().filter(a => a.status === AppointmentStatus.Completed).length
  );
  readonly cancelledCount  = computed(() =>
    this.appointments().filter(
      a => a.status === AppointmentStatus.Cancelled || a.status === AppointmentStatus.Missed
    ).length
  );
  readonly totalCount      = computed(() => this.appointments().length);

  readonly nextAppointment = computed(() => {
    const now = Date.now();
    return this.appointments()
      .filter(a => a.status === AppointmentStatus.Upcoming && new Date(a.appointmentDateTime).getTime() > now)
      .sort((a, b) => new Date(a.appointmentDateTime).getTime() - new Date(b.appointmentDateTime).getTime())[0]
      ?? null;
  });

  readonly filtered = computed(() => {
    let list = this.appointments();
    const tab  = this.activeTab();
    const q    = this.searchQuery().toLowerCase().trim();
    const from = this.dateFrom();
    const to   = this.dateTo();

    if (tab === 'upcoming')  list = list.filter(a => a.status === AppointmentStatus.Upcoming);
    if (tab === 'completed') list = list.filter(a => a.status === AppointmentStatus.Completed);
    if (tab === 'cancelled') list = list.filter(
      a => a.status === AppointmentStatus.Cancelled || a.status === AppointmentStatus.Missed
    );

    if (q) {
      list = list.filter(a =>
        a.title.toLowerCase().includes(q) ||
        a.provider.toLowerCase().includes(q) ||
        (a.customType ?? '').toLowerCase().includes(q)
      );
    }

    if (from) list = list.filter(a => new Date(a.appointmentDateTime) >= new Date(from));
    if (to)   list = list.filter(a => new Date(a.appointmentDateTime) <= new Date(`${to}T23:59:59`));

    const sort = this.sortBy();
    if (sort === 'recent')  list = [...list].sort((a, b) => new Date(b.appointmentDateTime).getTime() - new Date(a.appointmentDateTime).getTime());
    if (sort === 'oldest')  list = [...list].sort((a, b) => new Date(a.appointmentDateTime).getTime() - new Date(b.appointmentDateTime).getTime());
    if (sort === 'az')      list = [...list].sort((a, b) => a.title.localeCompare(b.title));
    if (sort === 'za')      list = [...list].sort((a, b) => b.title.localeCompare(a.title));

    return list;
  });

  readonly paginated = computed(() => {
    const p = this.currentPage();
    return this.filtered().slice((p - 1) * this.PAGE_SIZE, p * this.PAGE_SIZE);
  });

  readonly totalPages = computed(() =>
    Math.max(1, Math.ceil(this.filtered().length / this.PAGE_SIZE))
  );

readonly pageNumbers = computed(() => {
  const total = this.totalPages();
  const cur = this.currentPage();

  const pages: (number | '...')[] = [];

  if (total <= 7) {
    for (let i = 1; i <= total; i++) {
      pages.push(i);
    }
  } else {
    pages.push(1);

    if (cur > 3)
      pages.push('...');

    for (
      let i = Math.max(2, cur - 1);
      i <= Math.min(total - 1, cur + 1);
      i++
    ) {
      pages.push(i);
    }

    if (cur < total - 2)
      pages.push('...');

    pages.push(total);
  }

  return pages;
});

goToPage(page: number | '...') {
  if (page === '...') return;
  if (page === this.currentPage()) return;

  this.animateTable(page > this.currentPage() ? 'left' : 'right');
  this.currentPage.set(page);
}

prevPage() {
  if (this.currentPage() > 1) {
    this.animateTable('right');
    this.currentPage.update(p => p - 1);
  }
}

nextPage() {
  if (this.currentPage() < this.totalPages()) {
    this.animateTable('left');
    this.currentPage.update(p => p + 1);
  }
}

  readonly unreadCount = this.notifSvc.unreadCount;

  constructor() {
    // Reload when an appointment is confirmed from the reminder modal so the
    // status updates immediately without requiring a page refresh.
    effect(() => {
      const tick = this.notifSvc.appointmentDataRefreshTick();
      if (tick > 0) this.loadAppointments();
    });
  }

  // ── Auth helpers ──────────────────────────────────────────────────────────
  get displayName(): string {
    const u = this.authSvc.currentUser;
    return u?.firstName?.trim() || u?.email || 'there';
  }
  get userEmail(): string { return this.authSvc.currentUser?.email ?? ''; }
  get avatarUrl(): string { return this.authSvc.avatarUrl; }

  get hasProfileImage(): boolean { return !!this.authSvc.currentUser?.profileImageUrl; }

  get userInitials(): string {
    const u = this.authSvc.currentUser;
    if (!u) return '?';
    const f = (u.firstName ?? '')[0] ?? '';
    const l = (u.lastName ?? '')[0] ?? '';
    return (f + l).toUpperCase() || (u.email ?? '?')[0].toUpperCase();
  }

  get avatarBgColor(): string {
    const palette = ['#0EAFD7', '#7C3AED', '#16A34A', '#EA580C', '#0D9488'];
    const seed = this.displayName || this.userEmail || 'U';
    let h = 0;
    for (let i = 0; i < seed.length; i++) h = seed.charCodeAt(i) + ((h << 5) - h);
    return palette[Math.abs(h) % palette.length];
  }

  // ── Lifecycle ─────────────────────────────────────────────────────────────
  ngOnInit(): void {
    this.profileCache.ensure();
    this.applyResponsiveSidebar();
    this.notifTimer = setInterval(() => this.checkDueNotifications(), 60_000);

    this.route.queryParams.subscribe(params => {
      const profileId = params['profileId'] ?? null;
      this.fpProfileId.set(profileId);
      this.loadAppointments();

      // Auto-open add modal only when navigated from dashboard with ?openAdd=1
      // and the current profile is not read-only.
      if (params['openAdd'] && !this.fpReadOnly()) {
        this.openAdd();
        this.router.navigate([], { replaceUrl: true, queryParams: {} });
      }
    });
  }

  ngOnDestroy(): void {
    if (this.notifTimer) clearInterval(this.notifTimer);
  }

  @HostListener('window:resize')
  onResize(): void { this.applyResponsiveSidebar(); }

  @HostListener('document:click', ['$event'])
  onDocumentClick(e: MouseEvent): void {
    const t = e.target as HTMLElement;
    if (!t.closest('.hdr-user')) this.dropdownOpen.set(false);
    if (!t.closest('.record-actions')) this.openMenuId.set(null);
  }

  private applyResponsiveSidebar(): void {
    this.sidebarCollapsed.set(window.innerWidth <= 1024);
    if (window.innerWidth > 768) this.mobileSidebarOpen.set(false);
  }

  toggleSidebar(): void       { this.sidebarCollapsed.update(v => !v); }
  toggleMobileSidebar(): void { this.mobileSidebarOpen.update(v => !v); }
  toggleDropdown(): void      { this.dropdownOpen.update(v => !v); }
  logout(): void { this.dropdownOpen.set(false); this.authSvc.logout().subscribe(); }

  goToMyProfile(): void { this.dropdownOpen.set(false); this.router.navigate(['/my-profile']); }

  goToFamilyProfiles(): void { this.router.navigate(['/family-profiles']); }

  toggleMenu(id: string, e: MouseEvent): void {
    e.stopPropagation();
    this.openMenuId.update(cur => cur === id ? null : id);
  }

  // ── Data loading ──────────────────────────────────────────────────────────
  loadAppointments(): void {
    this.loading.set(true);
    this.loadError.set(null);
    this.apptSvc.getAll(this.fpProfileId() ?? undefined).subscribe({
      next: data => {
        this.appointments.set(data);
        this.loading.set(false);
        // Keep the detail modal in sync if it's open
        const viewing = this.viewingAppt();
        if (viewing) {
          const updated = data.find(a => a.id === viewing.id);
          if (updated) this.viewingAppt.set(updated);
        }
      },
      error: err => {
        this.loadError.set(
          err?.error?.message ?? this.t().appointments.couldNotLoad
        );
        this.loading.set(false);
      },
    });
  }

  // ── Filters / tabs ────────────────────────────────────────────────────────
  setTab(tab: ApptTab): void {
    if (tab === this.activeTab()) return;

    const order: ApptTab[] = ['all', 'upcoming', 'completed', 'cancelled'];
    const currentIndex = order.indexOf(this.activeTab());
    const nextIndex = order.indexOf(tab);
    this.animateTable(nextIndex >= currentIndex ? 'left' : 'right');
    this.activeTab.set(tab);
    this.currentPage.set(1);
    this.mobileTabMenuOpen.set(false);
  }

  toggleMobileTabMenu(): void { this.mobileTabMenuOpen.update(v => !v); }
  closeMobileTabMenu(): void  { this.mobileTabMenuOpen.set(false); }

  activeTabLabel(): string {
    const t = this.t().appointments;
    const map: Record<string, string> = {
      all:       t.all || 'All',
      upcoming:  t.upcoming || 'Upcoming',
      completed: t.completed || 'Completed',
      cancelled: t.cancelledMissed || 'Cancelled/Missed',
    };
    return map[this.activeTab()] ?? (t.all || 'All');
  }

  activeTabIcon(): string {
    const map: Record<string, string> = {
      all: 'fa-layer-group',
      upcoming: 'fa-calendar',
      completed: 'fa-circle-check',
      cancelled: 'fa-circle-xmark',
    };
    return map[this.activeTab()] ?? 'fa-layer-group';
  }
  setPage(p: number | '...'): void { if (typeof p === 'number') this.goToPage(p); }
  onFilterChange(): void {
    if (this.currentPage() !== 1) this.animateTable('right');
    this.currentPage.set(1);
  }
  clearFilters(): void {
    if (this.searchQuery() || this.dateFrom() || this.dateTo()) this.animateTable('right');
    this.searchQuery.set('');
    this.dateFrom.set('');
    this.dateTo.set('');
    this.currentPage.set(1);
  }

  private animateTable(direction: 'left' | 'right'): void {
    this.tabDirection.set(direction);
    this.tabAnimating.set(false);
    setTimeout(() => this.tabAnimating.set(true));
    setTimeout(() => this.tabAnimating.set(false), 260);
  }

  // ── Add / Edit ────────────────────────────────────────────────────────────
  openAdd(): void {
    if (this.fpReadOnly()) return;
    this.editingId.set(null);
    this.resetForm();
    this.formStep.set(1);
    this.showAddModal.set(true);
  }

  openEdit(a: AppointmentDto): void {
    if (this.fpReadOnly()) return;
    this.editingId.set(a.id);
    const dt = new Date(a.appointmentDateTime);
    this.fType.set(a.appointmentType);
    this.fCustomType.set(a.customType ?? '');
    this.fTitle.set(a.title);
    this.fProvider.set(a.provider);
    this.fDate.set(dt.toISOString().slice(0, 10));
    this.fTime.set(dt.toTimeString().slice(0, 5));
    this.applyReminderValue(a.reminderOffsetMinutes ?? null);
    this.fNotes.set(a.notes ?? '');
    this.formErrors.set({});
    this.formStep.set(2);
    this.showAddModal.set(true);
  }

  closeAddModal(): void { this.showAddModal.set(false); }

  selectType(t: AppointmentType): void {
    this.fType.set(t);
    if (!this.editingId() && !this.fTitle()) {
      this.fTitle.set(APPOINTMENT_TYPE_LABELS[t]);
    }
    this.formErrors.update(e => ({ ...e, appointmentType: '' }));
  }

  onCustomTypeChange(value: string): void {
    this.fCustomType.set(value);
    this.formErrors.update(e => ({ ...e, customType: '' }));
  }

  goStep2(): void {
    if (!this.fType()) {
      this.formErrors.update(e => ({ ...e, appointmentType: this.t().appointments.pleaseSelectType }));
      return;
    }
    if (this.fType() === AppointmentType.Other && !this.fCustomType().trim()) {
      this.formErrors.update(e => ({ ...e, customType: this.t().appointments.pleaseDescribeType }));
      return;
    }
    this.formErrors.set({});
    this.formStep.set(2);
  }

  goStep1(): void { this.formStep.set(1); }

  get minDate(): string {
    const d = new Date();
    d.setMinutes(d.getMinutes() + 5);
    return d.toISOString().slice(0, 10);
  }

  get minTime(): string {
    const fDate = this.fDate();
    const today = new Date().toISOString().slice(0, 10);
    if (fDate !== today) return '00:00';
    const d = new Date();
    d.setMinutes(d.getMinutes() + 5);
    return d.toTimeString().slice(0, 5);
  }

  setPresetReminder(value: number | null): void {
    this.customReminderSelected.set(false);
    this.fReminder.set(value);
    this.fCustomReminder.set(null);
    this.formErrors.update(e => ({ ...e, reminder: '' }));
  }

  selectCustomReminder(): void {
    this.customReminderSelected.set(true);
    const current = this.fCustomReminder() ?? this.fReminder() ?? 30;
    this.fCustomReminder.set(current);
    this.fReminder.set(current);
    this.formErrors.update(e => ({ ...e, reminder: '' }));
  }

  setCustomReminder(value: number | string | null): void {
    const parsed = value === null || value === '' ? null : Number(value);
    this.customReminderSelected.set(true);
    this.fCustomReminder.set(Number.isFinite(parsed) ? parsed : null);
    this.fReminder.set(Number.isFinite(parsed) ? parsed : null);
    this.formErrors.update(e => ({ ...e, reminder: '' }));
  }

  private applyReminderValue(value: number | null): void {
    const presetValues = this.REMINDER_OPTIONS.map(opt => opt.value);
    const isPreset = value === null || presetValues.includes(value);
    this.customReminderSelected.set(!isPreset);
    this.fReminder.set(value);
    this.fCustomReminder.set(isPreset ? null : value);
  }

  private getReminderOffset(): number | null {
    return this.customReminderSelected()
      ? this.fCustomReminder()
      : this.fReminder();
  }

  private hasUpcomingAppointmentAtSelectedTime(): boolean {
    const selectedKey = this.appointmentMinuteKey(`${this.fDate()}T${this.fTime()}:00`);
    const editingId = this.editingId();

    return this.appointments().some(appointment =>
      appointment.id !== editingId &&
      appointment.status === AppointmentStatus.Upcoming &&
      this.appointmentMinuteKey(appointment.appointmentDateTime) === selectedKey
    );
  }

  private appointmentMinuteKey(value: string): string {
    const date = new Date(value);
    if (Number.isNaN(date.getTime())) return value.slice(0, 16);

    const year = date.getFullYear();
    const month = String(date.getMonth() + 1).padStart(2, '0');
    const day = String(date.getDate()).padStart(2, '0');
    const hours = String(date.getHours()).padStart(2, '0');
    const minutes = String(date.getMinutes()).padStart(2, '0');
    return `${year}-${month}-${day}T${hours}:${minutes}`;
  }

  private validate(): boolean {
    const errs: Record<string, string> = {};
    const a = this.t().appointments;
    if (!this.fType()) errs['appointmentType'] = a.pleaseSelectType;
    if (this.fType() === AppointmentType.Other && !this.fCustomType().trim()) {
      errs['customType'] = a.pleaseDescribeType;
    }
    if (!this.fTitle().trim())    errs['title']    = a.titleRequired;
    if (!this.fProvider().trim()) errs['provider'] = a.providerRequired;
    if (!this.fDate())            errs['date']     = a.dateRequired;
    if (!this.fTime())            errs['time']     = a.timeRequired;
    if (this.fDate() && this.fTime()) {
      const sel = new Date(`${this.fDate()}T${this.fTime()}`);
      if (sel <= new Date()) errs['date'] = a.appointmentFuture;
      if (this.hasUpcomingAppointmentAtSelectedTime()) {
        errs['time'] = a.timeConflict;
      }
    }
    if (this.customReminderSelected()) {
      const customReminder = this.fCustomReminder();
      if (customReminder === null || customReminder < 1) {
        errs['reminder'] = a.customReminderMin;
      } else if (customReminder > 10080) {
        errs['reminder'] = a.customReminderMax;
      }
    }
    this.formErrors.set(errs);
    return Object.keys(errs).length === 0;
  }

  submitForm(): void {
    if (!this.validate()) return;
    const dt = new Date(`${this.fDate()}T${this.fTime()}`);
    const body: CreateAppointmentRequest = {
      appointmentType:        this.fType()!,
      customType:             this.fType() === AppointmentType.Other ? this.fCustomType().trim() : undefined,
      title:                  this.fTitle().trim(),
      provider:               this.fProvider().trim(),
      appointmentDateTime: `${this.fDate()}T${this.fTime()}:00`,
      reminderOffsetMinutes:  this.getReminderOffset() ?? undefined,
      notes:                  this.fNotes().trim() || undefined,
    };

    this.submitting.set(true);
    const id  = this.editingId();
    const op$ = id
      ? this.apptSvc.update(id, body as UpdateAppointmentRequest)
      : this.apptSvc.create(body);

    op$.subscribe({
      next: saved => {
        if (id) {
          this.appointments.update(list => list.map(a => a.id === id ? saved : a));
          this.toast(this.t().appointments.appointmentUpdated, 'success');
        } else {
          this.appointments.update(list => [...list, saved]);
          this.toast(this.t().appointments.appointmentAdded, 'success');
        }
        this.submitting.set(false);
        this.closeAddModal();
      },
      error: err => {
        this.toast(err?.error?.message ?? this.t().appointments.failedSave, 'error');
        this.submitting.set(false);
      },
    });
  }

  private resetForm(): void {
    this.fType.set(null); this.fCustomType.set(''); this.fTitle.set('');
    this.fProvider.set(''); this.fDate.set(''); this.fTime.set('');
    this.applyReminderValue(30); this.fNotes.set('');
    this.formErrors.set({});
  }

  // ── View ──────────────────────────────────────────────────────────────────
  openView(a: AppointmentDto): void { this.viewingAppt.set(a); this.showViewModal.set(true); }
  closeView(): void { this.showViewModal.set(false); }

  // ── Delete ────────────────────────────────────────────────────────────────
  confirmDelete(id: string): void {
    if (this.fpReadOnly()) return;
    this.deletingId.set(id); this.showDeleteModal.set(true);
  }
  closeDelete(): void { this.showDeleteModal.set(false); this.deletingId.set(null); }
  executeDelete(): void {
    const id = this.deletingId();
    if (!id) return;
    this.deleting.set(true);
    this.apptSvc.delete(id).subscribe({
      next:  ()  => { this.appointments.update(l => l.filter(a => a.id !== id)); this.toast(this.t().appointments.appointmentDeleted, 'success'); this.deleting.set(false); this.closeDelete(); },
      error: err => { this.toast(err?.error?.message ?? this.t().appointments.deleteFailed, 'error'); this.deleting.set(false); this.closeDelete(); },
    });
  }

  // ── Cancel ────────────────────────────────────────────────────────────────
  confirmCancel(id: string): void {
    if (this.fpReadOnly()) return;
    this.cancellingId.set(id); this.showCancelModal.set(true);
  }
  closeCancel(): void { this.showCancelModal.set(false); this.cancellingId.set(null); }
  executeCancel(): void {
    const id = this.cancellingId();
    if (!id) return;
    this.cancelling.set(true);
    this.apptSvc.cancel(id).subscribe({
      next:  saved => { this.appointments.update(l => l.map(a => a.id === id ? saved : a)); this.toast(this.t().appointments.appointmentCancelled, 'success'); this.cancelling.set(false); this.closeCancel(); },
      error: err   => { this.toast(err?.error?.message ?? this.t().appointments.cancelFailed, 'error'); this.cancelling.set(false); this.closeCancel(); },
    });
  }

  // ── Complete ──────────────────────────────────────────────────────────────
  markComplete(id: string): void {
    if (this.fpReadOnly()) return;
    this.apptSvc.complete(id).subscribe({
      next:  saved => { this.appointments.update(l => l.map(a => a.id === id ? saved : a)); this.toast(this.t().appointments.markedCompleted, 'success'); },
      error: err   => { this.toast(err?.error?.message ?? this.t().appointments.failedSave, 'error'); },
    });
  }

  // ── Notification timer ────────────────────────────────────────────────────
  private checkDueNotifications(): void {
    const now = Date.now();
    this.appointments()
      .filter(a => a.status === AppointmentStatus.Upcoming && !this.firedIds.has(a.id))
      .forEach(a => {
        const t = new Date(a.appointmentDateTime).getTime();
        if (t - now <= 60_000 && t >= now - 60_000) {
          this.firedIds.add(a.id);
          this.notifSvc.push({
            title: 'Appointment Starting Now',
            body:  `${a.title} with ${a.provider}`,
            type:  'appointment',
          });
        }
      });
  }

  // ── Toast ─────────────────────────────────────────────────────────────────
  toast(message: string, type: 'success' | 'error'): void {
    const id = ++this.toastSeq;
    this.toasts.update(t => [...t, { id, message, type }]);
    setTimeout(() => this.toasts.update(t => t.filter(x => x.id !== id)), 4500);
  }
  dismissToast(id: number): void { this.toasts.update(t => t.filter(x => x.id !== id)); }

  // ── Display helpers ───────────────────────────────────────────────────────
  formatDate(dt: string): string {
    return new Date(dt).toLocaleDateString('en-US', {
      weekday: 'short', month: 'short', day: 'numeric', year: 'numeric',
    });
  }

  formatTime(dt: string): string {
    return new Date(dt).toLocaleTimeString('en-US', { hour: 'numeric', minute: '2-digit', hour12: true });
  }

  relativeDate(dt: string): string {
    const diff = Math.ceil((new Date(dt).getTime() - Date.now()) / 86_400_000);
    if (diff < 0)  return `${Math.abs(diff)}d ago`;
    if (diff === 0) return 'Today';
    if (diff === 1) return 'Tomorrow';
    return `In ${diff} days`;
  }

  typeLabel(a: AppointmentDto): string {
    if (a.appointmentType === AppointmentType.Other && a.customType) return a.customType;
    return APPOINTMENT_TYPE_LABELS[a.appointmentType] ?? 'Other';
  }

  typeIcon(t: AppointmentType): string {
    return APPOINTMENT_TYPE_ICONS[t] ?? 'fa-calendar';
  }

  statusLabel(s: AppointmentStatus): string {
    const a = this.t().appointments;
    return {
      [AppointmentStatus.Upcoming]:  a.upcomingStatus,
      [AppointmentStatus.Completed]: a.completedStatus,
      [AppointmentStatus.Cancelled]: a.cancelledStatus,
      [AppointmentStatus.Missed]:    a.missedStatus,
    }[s] ?? '';
  }

  getReminderLabel(mins: number): string {
    const before = this.t().appointments.minutesBefore;
    const map: Record<number, string> = {
      15:   `15 ${before}`,
      30:   `30 ${before}`,
      60:   `60 ${before}`,
      120:  `120 ${before}`,
      1440: `1440 ${before}`,
    };
    return map[mins] ?? `${mins} ${before}`;
  }

  statusClass(s: AppointmentStatus): string {
    return { [AppointmentStatus.Upcoming]: 'pill pill-blue-sm', [AppointmentStatus.Completed]: 'pill pill-green-sm',
             [AppointmentStatus.Cancelled]: 'pill pill-red-sm', [AppointmentStatus.Missed]: 'pill pill-yellow-sm' }[s] ?? 'pill';
  }

  pageEnd(): number {
    return Math.min(this.currentPage() * this.PAGE_SIZE, this.filtered().length);
  }

  reminderLabel(mins: number | null | undefined): string {
    if (!mins) return '—';
    const map: Record<number, string> = { 15: '15 min', 30: '30 min', 60: '1 hr', 120: '2 hrs', 1440: '1 day' };
    return (map[mins] ?? `${mins} min`) + ' before';
  }

  isLastRow(index: number): boolean {
  return index === this.paginated().length - 1;
}

}
