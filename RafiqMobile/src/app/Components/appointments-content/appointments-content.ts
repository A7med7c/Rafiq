import {
  Component, Input, Output, EventEmitter, OnInit, OnChanges, SimpleChanges, OnDestroy,
  inject, signal, computed, HostListener, ViewEncapsulation
} from '@angular/core';
import { CommonModule } from '@angular/common';
import { FormsModule } from '@angular/forms';
import { Router, RouterLink, RouterLinkActive, ActivatedRoute } from '@angular/router';
import { AppointmentsService } from '../../Services/appointments.service';
import { NotificationService } from '../../Services/notification.service';
import { LocalizationService } from '../../Services/localization.service';
import { NotificationPermissionService, NotificationPermissionResult } from '../../Services/notification-permission.service';
import {
  AppointmentDto, AppointmentStatus, AppointmentType,
  CreateAppointmentRequest, UpdateAppointmentRequest,
  APPOINTMENT_TYPE_LABELS, APPOINTMENT_TYPE_ICONS, APPT_TYPE_KEYS,
} from '../../Modles/appointment.models';

type ApptTab = 'all' | 'upcoming' | 'completed' | 'cancelled';

interface Toast {
  id: number;
  message: string;
  type: 'success' | 'error';
}

import { AssistantAnchorDirective } from '../../core/assistant/directives/assistant-anchor.directive';

@Component({
  selector: 'app-appointments-content',
  standalone: true,
  imports: [CommonModule, FormsModule, AssistantAnchorDirective],
  templateUrl: './appointments-content.html',
  styleUrl: '../../Pages/appointments/appointments.css',
  encapsulation: ViewEncapsulation.None
})
export class AppointmentsContentComponent implements OnInit, OnChanges, OnDestroy {
  @Input() profileId: string | undefined;
  @Input() compact = false;
  @Input() readOnly = false;
  @Input() modalsOnly = false;

  @Output() appointmentChanged = new EventEmitter<void>();

  private readonly apptSvc = inject(AppointmentsService);
  private readonly notifSvc = inject(NotificationService);
  private readonly router = inject(Router);
  private readonly route = inject(ActivatedRoute);
  private readonly l10n  = inject(LocalizationService);
  private readonly notificationPermissionService = inject(NotificationPermissionService);
  readonly t = this.l10n.t;

  // ── Data ─────────────────────────────────────────────────────────────────
  readonly appointments = signal<AppointmentDto[]>([]);
  readonly loading = signal(true);
  readonly loadError = signal<string | null>(null);

  // ── Filters ──────────────────────────────────────────────────────────────
  readonly activeTab = signal<ApptTab>('all');
  readonly searchQuery = signal('');
  readonly dateFrom = signal('');
  readonly dateTo = signal('');
  readonly currentPage = signal(1);
  readonly PAGE_SIZE = 5;
  readonly tabDirection = signal<'left' | 'right'>('left');
  readonly tabAnimating = signal(false);

  // ── Action menus ─────────────────────────────────────────────────────────
  readonly openMenuId = signal<string | null>(null);

  // ── Add / Edit modal ──────────────────────────────────────────────────────
  readonly showAddModal = signal(false);
  readonly editingId = signal<string | null>(null);
  readonly formStep = signal<1 | 2>(1);
  // form fields as individual signals for clean ngModel binding
  readonly fType = signal<AppointmentType | null>(null);
  readonly fCustomType = signal('');
  readonly fTitle = signal('');
  readonly fProvider = signal('');
  readonly fDate = signal('');
  readonly fTime = signal('');
  readonly fReminder = signal<number | null>(30);
  readonly fCustomReminder = signal<number | null>(null);
  readonly customReminderSelected = signal(false);
  readonly fNotes = signal('');
  readonly formErrors = signal<Record<string, string>>({});
  readonly submitting = signal(false);

  // ── View modal ────────────────────────────────────────────────────────────
  readonly showViewModal = signal(false);
  readonly viewingAppt = signal<AppointmentDto | null>(null);

  // ── Delete modal ──────────────────────────────────────────────────────────
  readonly showDeleteModal = signal(false);
  readonly deletingId = signal<string | null>(null);
  readonly deleting = signal(false);

  // ── Cancel modal ──────────────────────────────────────────────────────────
  readonly showCancelModal = signal(false);
  readonly cancellingId = signal<string | null>(null);
  readonly cancelling = signal(false);

  // ── Toasts ───────────────────────────────────────────────────────────────
  private toastSeq = 0;
  readonly toasts = signal<Toast[]>([]);

  // ── Notification timer ────────────────────────────────────────────────────
  private notifTimer?: ReturnType<typeof setInterval>;
  private firedIds = new Set<string>();

  // ── Expose enums / constants to template ─────────────────────────────────
  readonly AppointmentType = AppointmentType;
  readonly AppointmentStatus = AppointmentStatus;
  readonly TYPE_LABELS = APPOINTMENT_TYPE_LABELS;
  readonly TYPE_ICONS = APPOINTMENT_TYPE_ICONS;
  readonly ALL_TYPES = [
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
    { value: 15 },
    { value: 30 },
    { value: 60 },
    { value: 120 },
    { value: 1440 },
  ];

  reminderOptionLabel(value: number): string {
    const ap = this.t().appointments;
    const map: Record<number, string> = {
      15: ap.reminder15Before,
      30: ap.reminder30Before,
      60: ap.reminder1hrBefore,
      120: ap.reminder2hrsBefore,
      1440: ap.reminder1dayBefore,
    };
    return map[value] ?? ap.minBeforeFormat.replace('{mins}', String(value));
  }

  readonly canContinueType = computed(() =>
    !!this.fType() &&
    (this.fType() !== AppointmentType.Other || !!this.fCustomType().trim())
  );

  // ── Computed ──────────────────────────────────────────────────────────────
  readonly upcomingCount = computed(() =>
    this.appointments().filter(a => a.status === AppointmentStatus.Upcoming).length
  );
  readonly completedCount = computed(() =>
    this.appointments().filter(a => a.status === AppointmentStatus.Completed).length
  );
  readonly cancelledCount = computed(() =>
    this.appointments().filter(
      a => a.status === AppointmentStatus.Cancelled || a.status === AppointmentStatus.Missed
    ).length
  );
  readonly totalCount = computed(() => this.appointments().length);

  readonly nextAppointment = computed(() => {
    const now = Date.now();
    return this.appointments()
      .filter(a => a.status === AppointmentStatus.Upcoming && new Date(a.appointmentDateTime).getTime() > now)
      .sort((a, b) => new Date(a.appointmentDateTime).getTime() - new Date(b.appointmentDateTime).getTime())[0]
      ?? null;
  });

  readonly filtered = computed(() => {
    let list = this.appointments();
    const tab = this.activeTab();
    const q = this.searchQuery().toLowerCase().trim();
    const from = this.dateFrom();
    const to = this.dateTo();

    if (tab === 'upcoming') list = list.filter(a => a.status === AppointmentStatus.Upcoming);
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
    if (to) list = list.filter(a => new Date(a.appointmentDateTime) <= new Date(`${to}T23:59:59`));

    return list.sort(
      (a, b) => new Date(a.appointmentDateTime).getTime() - new Date(b.appointmentDateTime).getTime()
    );
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

  // ── Lifecycle ─────────────────────────────────────────────────────────────
  ngOnInit(): void {
    this.loadAppointments();
    this.notifTimer = setInterval(() => this.checkDueNotifications(), 60_000);

    // Auto-open add modal if navigated from dashboard with ?openAdd=1
    this.route.queryParams.subscribe(params => {
      if (params['openAdd']) {
        this.openAdd();
        this.router.navigate([], { replaceUrl: true, queryParams: {} });
      }
    });
  }

  ngOnChanges(changes: SimpleChanges): void {
    if (changes['profileId'] && !changes['profileId'].firstChange) {
      this.appointments.set([]);
      this.currentPage.set(1);
      this.loadAppointments();
    }
  }

  ngOnDestroy(): void {
    if (this.notifTimer) clearInterval(this.notifTimer);
  }

  @HostListener('document:click', ['$event'])
  onDocumentClick(e: MouseEvent): void {
    const t = e.target as HTMLElement;
    if (!t.closest('.record-actions')) this.openMenuId.set(null);
  }

  toggleMenu(id: string, e: MouseEvent): void {
    e.stopPropagation();
    this.openMenuId.update(cur => cur === id ? null : id);
  }

  // ── Data loading ──────────────────────────────────────────────────────────
  loadAppointments(): void {
    this.loading.set(true);
    this.loadError.set(null);
    this.apptSvc.getAll(this.profileId).subscribe({
      next: data => { this.appointments.set(data); this.loading.set(false); },
      error: err => {
        this.loadError.set(
          this.t().appointments.couldNotLoad
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
    this.editingId.set(null);
    this.resetForm();
    this.formStep.set(1);
    this.showAddModal.set(true);
  }

  openEdit(a: AppointmentDto): void {
    this.editingId.set(a.id);
    const dt = new Date(a.appointmentDateTime);
    this.fType.set(a.appointmentType);
    this.fCustomType.set(a.customType ?? '');
    this.fTitle.set(a.title);
    this.fProvider.set(a.provider);
    const pad = (n: number) => String(n).padStart(2, '0');
    this.fDate.set(`${dt.getFullYear()}-${pad(dt.getMonth() + 1)}-${pad(dt.getDate())}`);
    this.fTime.set(`${pad(dt.getHours())}:${pad(dt.getMinutes())}`);
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
      this.fTitle.set(this.typeLabelForType(t));
    }
    this.formErrors.update(e => ({ ...e, appointmentType: '' }));
  }

  onCustomTypeChange(value: string): void {
    this.fCustomType.set(value);
    this.formErrors.update(e => ({ ...e, customType: '' }));
  }

  goStep2(): void {
    const ap = this.t().appointments;
    if (!this.fType()) {
      this.formErrors.update(e => ({ ...e, appointmentType: ap.pleaseSelectType }));
      return;
    }
    if (this.fType() === AppointmentType.Other && !this.fCustomType().trim()) {
      this.formErrors.update(e => ({ ...e, customType: ap.pleaseDescribeType }));
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
    const ap = this.t().appointments;
    const errs: Record<string, string> = {};
    if (!this.fType()) errs['appointmentType'] = ap.pleaseSelectType;
    if (this.fType() === AppointmentType.Other && !this.fCustomType().trim()) {
      errs['customType'] = ap.pleaseDescribeType;
    }
    if (!this.fTitle().trim()) errs['title'] = ap.titleRequired;
    if (!this.fProvider().trim()) errs['provider'] = ap.providerRequired;
    if (!this.fDate()) errs['date'] = ap.dateRequired;
    if (!this.fTime()) errs['time'] = ap.timeRequired;
    if (this.fDate() && this.fTime()) {
      const sel = new Date(`${this.fDate()}T${this.fTime()}`);
      if (sel <= new Date()) errs['date'] = ap.appointmentFuture;
      if (this.hasUpcomingAppointmentAtSelectedTime()) {
        errs['time'] = ap.timeConflict;
      }
    }
    if (this.customReminderSelected()) {
      const customReminder = this.fCustomReminder();
      if (customReminder === null || customReminder < 1) {
        errs['reminder'] = ap.customReminderMin;
      } else if (customReminder > 10080) {
        errs['reminder'] = ap.customReminderMax;
      }
    }
    this.formErrors.set(errs);
    return Object.keys(errs).length === 0;
  }

  submitForm(): void {
    if (!this.validate()) return;
    const dt = new Date(`${this.fDate()}T${this.fTime()}`);
    const body: CreateAppointmentRequest = {
      appointmentType: this.fType()!,
      customType: this.fType() === AppointmentType.Other ? this.fCustomType().trim() : undefined,
      title: this.fTitle().trim(),
      provider: this.fProvider().trim(),
      appointmentDateTime: new Date(`${this.fDate()}T${this.fTime()}`).toISOString(),
      reminderOffsetMinutes: this.getReminderOffset() ?? undefined,
      notes: this.fNotes().trim() || undefined,
    };

    this.submitting.set(true);
    const id = this.editingId();
    const op$ = id
      ? this.apptSvc.update(id, body as UpdateAppointmentRequest)
      : this.apptSvc.create(body, this.profileId);

    op$.subscribe({
      next: saved => {
        const ap = this.t().appointments;
        if (id) {
          this.appointments.update(list => list.map(a => a.id === id ? saved : a));
          this.toast(ap.appointmentUpdated, 'success');
        } else {
          this.appointments.update(list => [...list, saved]);
          this.toast(ap.appointmentAdded, 'success');
        }
        this.submitting.set(false);
        this.closeAddModal();
        this.appointmentChanged.emit();
        
        if (body.reminderOffsetMinutes !== undefined) {
           this.notificationPermissionService.ensurePermission().then(result => {
               if (result === NotificationPermissionResult.Denied || result === NotificationPermissionResult.PermanentlyDenied) {
                   this.toast(this.l10n.t().notificationPermission.appointmentSavedNoNotifs, 'error');
               }
           });
        }
      },
      error: err => {
        this.toast(this.t().appointments.failedSave, 'error');
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
  confirmDelete(id: string): void { this.deletingId.set(id); this.showDeleteModal.set(true); }
  closeDelete(): void { this.showDeleteModal.set(false); this.deletingId.set(null); }
  executeDelete(): void {
    const id = this.deletingId();
    if (!id) return;
    this.deleting.set(true);
    this.apptSvc.delete(id).subscribe({
      next: () => { this.appointments.update(l => l.filter(a => a.id !== id)); this.toast(this.t().appointments.appointmentDeleted, 'success'); this.deleting.set(false); this.closeDelete(); this.appointmentChanged.emit(); },
      error: err => { this.toast(this.t().appointments.deleteFailed, 'error'); this.deleting.set(false); this.closeDelete(); },
    });
  }

  // ── Cancel ────────────────────────────────────────────────────────────────
  confirmCancel(id: string): void { this.cancellingId.set(id); this.showCancelModal.set(true); }
  closeCancel(): void { this.showCancelModal.set(false); this.cancellingId.set(null); }
  executeCancel(): void {
    const id = this.cancellingId();
    if (!id) return;
    this.cancelling.set(true);
    this.apptSvc.cancel(id).subscribe({
      next: saved => { this.appointments.update(l => l.map(a => a.id === id ? saved : a)); this.toast(this.t().appointments.appointmentCancelled, 'success'); this.cancelling.set(false); this.closeCancel(); this.appointmentChanged.emit(); },
      error: err => { this.toast(this.t().appointments.cancelFailed, 'error'); this.cancelling.set(false); this.closeCancel(); },
    });
  }

  // ── Complete ──────────────────────────────────────────────────────────────
  markComplete(id: string): void {
    this.apptSvc.complete(id).subscribe({
      next: saved => { this.appointments.update(l => l.map(a => a.id === id ? saved : a)); this.toast(this.t().appointments.markedCompleted, 'success'); this.appointmentChanged.emit(); },
      error: err => { this.toast(this.t().appointments.genericFailed, 'error'); },
    });
  }

  // ── Notification timer ────────────────────────────────────────────────────
  private checkDueNotifications(): void {
    const now = Date.now();
    const ap = this.t().appointments;
    this.appointments()
      .filter(a => a.status === AppointmentStatus.Upcoming && !this.firedIds.has(a.id))
      .forEach(a => {
        const t = new Date(a.appointmentDateTime).getTime();
        if (t - now <= 60_000 && t >= now - 60_000) {
          this.firedIds.add(a.id);
          this.notifSvc.push({
            title: ap.notifStartingTitle,
            body: `${a.title} ${ap.withWord} ${a.provider}`,
            type: 'appointment',
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
  private get locale(): string {
    return this.l10n.lang() === 'ar' ? 'ar-EG' : 'en-US';
  }

  formatDate(dt: string): string {
    return new Date(dt).toLocaleDateString(this.locale, {
      weekday: 'short', month: 'short', day: 'numeric', year: 'numeric', numberingSystem: 'latn',
    });
  }

  formatTime(dt: string): string {
    return new Date(dt).toLocaleTimeString(this.locale, { hour: 'numeric', minute: '2-digit', hour12: true, numberingSystem: 'latn' });
  }

  relativeDate(dt: string): string {
    const ap = this.t().appointments;
    const diff = Math.ceil((new Date(dt).getTime() - Date.now()) / 86_400_000);
    if (diff < 0) return ap.daysAgoFormat.replace('{days}', String(Math.abs(diff)));
    if (diff === 0) return ap.todayWord;
    if (diff === 1) return ap.tomorrowWord;
    return ap.inDaysFormat.replace('{days}', String(diff));
  }

  typeLabel(a: AppointmentDto): string {
    if (a.appointmentType === AppointmentType.Other && a.customType) return a.customType;
    const key = APPT_TYPE_KEYS[a.appointmentType];
    const fallback = APPOINTMENT_TYPE_LABELS[a.appointmentType] ?? this.t().appointments.other;
    if (!key) return fallback;
    return key.split('.').reduce((obj: any, part) => obj?.[part], this.t()) ?? fallback;
  }

  /** Returns the translated label for a type enum value (used in the type-picker grid) */
  typeLabelForType(type: AppointmentType): string {
    const key = APPT_TYPE_KEYS[type];
    const fallback = APPOINTMENT_TYPE_LABELS[type] ?? this.t().appointments.other;
    if (!key) return fallback;
    return key.split('.').reduce((obj: any, part) => obj?.[part], this.t()) ?? fallback;
  }

  typeIcon(t: AppointmentType): string {
    return APPOINTMENT_TYPE_ICONS[t] ?? 'fa-calendar';
  }

  statusLabel(s: AppointmentStatus): string {
    const ap = this.t().appointments;
    return {
      [AppointmentStatus.Upcoming]: ap.upcomingStatus, [AppointmentStatus.Completed]: ap.completedStatus,
      [AppointmentStatus.Cancelled]: ap.cancelledStatus, [AppointmentStatus.Missed]: ap.missedStatus
    }[s] ?? '';
  }

  statusClass(s: AppointmentStatus): string {
    return {
      [AppointmentStatus.Upcoming]: 'pill pill-blue-sm', [AppointmentStatus.Completed]: 'pill pill-green-sm',
      [AppointmentStatus.Cancelled]: 'pill pill-red-sm', [AppointmentStatus.Missed]: 'pill pill-yellow-sm'
    }[s] ?? 'pill';
  }

  pageEnd(): number {
    return Math.min(this.currentPage() * this.PAGE_SIZE, this.filtered().length);
  }

  reminderLabel(mins: number | null | undefined): string {
    if (!mins) return '—';
    return this.reminderOptionLabel(mins);
  }

  isLastRow(index: number): boolean {
    return index === this.paginated().length - 1;
  }
}
