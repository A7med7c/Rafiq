import {
  Component, effect, inject, OnInit, OnDestroy, signal, computed, HostListener,
} from '@angular/core';
import { CommonModule } from '@angular/common';
import { FormsModule } from '@angular/forms';
import { ActivatedRoute, Router, RouterLink, RouterLinkActive } from '@angular/router';
import { forkJoin, Observable } from 'rxjs';
import { AuthService } from '../../Services/auth-service';
import { NotificationService } from '../../Services/notification.service';
import { MedicationRemindersService } from '../../Services/medication-reminders.service';
import { MedicationReminderLogDto, MedicationReminderStatus } from '../../Modles/medication-reminder.models';
import { CreateReminderPayload, MedicineReminder, UpdateReminderPayload, UserMedicine } from '../../Modles/dashboard.models';

type MedTab = 'schedule' | 'medications';
type RepeatOption = 'Once' | 'Daily' | 'Weekly' | 'Monthly';

interface ReminderForm {
  reminderTimes: string[];
  repeatType: RepeatOption;
  startDate: string;
  endDate: string;
  notificationsEnabled: boolean;
  notes: string;
}

interface Toast {
  id: number;
  message: string;
  type: 'success' | 'error';
}

/**
 * One scheduled dose. The backend escalates a single dose into up to three
 * reminder logs (#1, #2, #3), so the logs are folded back into the dose the
 * patient actually has to take.
 */
interface Dose {
  key: string;
  medicineName: string;
  dosage: string;
  scheduledTime: string;
  minutes: number;
  status: MedicationReminderStatus;
  attempts: number;
  confirmedAt: string | null;
  /** Newest log still awaiting an answer — the one "I took it" confirms. */
  actionable: MedicationReminderLogDto | null;
  ids: string[];
}

@Component({
  selector: 'app-medications',
  standalone: true,
  imports: [CommonModule, FormsModule, RouterLink, RouterLinkActive],
  templateUrl: './medications.html',
  styleUrl: './medications.css',
})
export class Medications implements OnInit, OnDestroy {
  private static minutesNow(): number {
    const d = new Date();
    return d.getHours() * 60 + d.getMinutes();
  }

  /** "08:30:00" → 510 */
  private static toMinutes(time: string): number {
    const [h, m] = time.split(':').map(Number);
    return (h || 0) * 60 + (m || 0);
  }

  /**
   * Today's date in the user's own timezone.
   *
   * `toISOString()` would give the UTC date, which is the wrong day for part of every
   * evening east of Greenwich — and the reminder *times* on this form are local wall
   * clock, so the date must be local too or the two disagree.
   */
  private static localToday(): string {
    const d = new Date();
    const pad = (n: number) => String(n).padStart(2, '0');
    return `${d.getFullYear()}-${pad(d.getMonth() + 1)}-${pad(d.getDate())}`;
  }

  private readonly authSvc   = inject(AuthService);
  protected readonly notifSvc  = inject(NotificationService);
  private readonly medSvc    = inject(MedicationRemindersService);
  private readonly route     = inject(ActivatedRoute);
  private readonly router    = inject(Router);

  private readonly medicationRefreshEffect = effect(() => {
    if (this.notifSvc.reminderDataRefreshTick() === 0) {
      return;
    }

    this.loadSchedule();
    this.loadAllMedicineReminders();
  });

  // ── Layout ──────────────────────────────────────────────────────────────
  readonly sidebarCollapsed  = signal(false);
  readonly mobileSidebarOpen = signal(false);
  readonly dropdownOpen      = signal(false);

  // ── Tabs ─────────────────────────────────────────────────────────────────
  readonly activeTab = signal<MedTab>('schedule');

  // ── Today's Schedule ──────────────────────────────────────────────────────
  readonly todayLogs       = signal<MedicationReminderLogDto[]>([]);
  readonly scheduleLoading = signal(true);
  readonly scheduleError   = signal<string | null>(null);

  // ── My Medications ────────────────────────────────────────────────────────
  readonly medicines     = signal<UserMedicine[]>([]);
  readonly medsLoading   = signal(false);
  readonly medsError     = signal<string | null>(null);

  // ── Medicine Reminders ────────────────────────────────────────────────────
  readonly medicineReminders     = signal<Record<string, MedicineReminder[]>>({});
  readonly remindersLoading      = signal(false);
  readonly highlightNoReminders  = signal(false);

  readonly hasAnyReminders = computed(() => {
    return this.medicines().some(m => this.hasReminders(m.id));
  });

  // ── Add / Edit Reminder modal ─────────────────────────────────────────────
  readonly editMode        = signal(false);
  private readonly editReminderIds = signal<string[]>([]);
  readonly showAddReminderModal    = signal(false);
  readonly addReminderMedicineId   = signal<string | null>(null);
  readonly addReminderMedicineName = signal('');
  readonly addReminderSaving       = signal(false);

  // ── Delete Reminder confirm modal ─────────────────────────────────────────
  readonly showDeleteReminderModal = signal(false);
  readonly deleteReminderMedId     = signal<string | null>(null);
  readonly deleteReminderMedName   = signal('');
  readonly deletingReminder        = signal(false);

  // ── Confirm modal ─────────────────────────────────────────────────────────
  readonly showConfirmModal  = signal(false);
  readonly confirmingLog     = signal<MedicationReminderLogDto | null>(null);
  readonly confirming        = signal(false);
  readonly pendingReminderId = signal<string | null>(null);
  readonly highlightReminderId = signal<string | null>(null);

  // ── Reminder form ─────────────────────────────────────────────────────────
  reminderForm: ReminderForm = this.emptyReminderForm();
  readonly repeatOptions: { label: string; value: RepeatOption }[] = [
    { label: 'Once',    value: 'Once'    },
    { label: 'Daily',   value: 'Daily'   },
    { label: 'Weekly',  value: 'Weekly'  },
    { label: 'Monthly', value: 'Monthly' },
  ];

  // ── Toasts ───────────────────────────────────────────────────────────────
  private toastSeq = 0;
  readonly toasts  = signal<Toast[]>([]);

  // ── Clock ────────────────────────────────────────────────────────────────
  /** Minutes since midnight, ticked every 30s so the rail and countdowns stay live. */
  readonly nowMinutes = signal(Medications.minutesNow());
  private clockId?: ReturnType<typeof setInterval>;

  // ── Doses (escalation logs folded into the dose they belong to) ───────────
  readonly doses = computed<Dose[]>(() => {
    const groups = new Map<string, MedicationReminderLogDto[]>();

    for (const log of this.todayLogs()) {
      const key = `${log.medicineReminderId}|${log.scheduledTime}`;
      const bucket = groups.get(key);
      if (bucket) bucket.push(log);
      else groups.set(key, [log]);
    }

    const doses: Dose[] = [];

    for (const [key, logs] of groups) {
      const ordered = [...logs].sort((a, b) => a.reminderNumber - b.reminderNumber);
      const newest  = ordered[ordered.length - 1];

      const confirmed = ordered.find(l => l.status === 'Confirmed');
      // Overdue is answerable too — a missed dose can still be confirmed late.
      const open = [...ordered].reverse().find(
        l => l.status === 'Pending' || l.status === 'Sent' || l.status === 'Overdue'
      ) ?? null;

      let status: MedicationReminderStatus;
      if (confirmed)                                  status = 'Confirmed';
      else if (open)                                  status = open.status;
      else if (ordered.every(l => l.status === 'Cancelled')) status = 'Cancelled';
      else                                            status = newest.status;

      doses.push({
        key,
        medicineName:  newest.medicineName,
        dosage:        newest.dosage,
        scheduledTime: newest.scheduledTime,
        minutes:       Medications.toMinutes(newest.scheduledTime),
        status,
        attempts:      ordered.filter(l => l.status === 'Sent' || l.status === 'Confirmed').length || ordered.length,
        confirmedAt:   confirmed?.confirmedAt ?? null,
        actionable:    confirmed ? null : open,
        ids:           ordered.map(l => l.id),
      });
    }

    return doses.sort((a, b) => a.minutes - b.minutes);
  });

  readonly takenCount   = computed(() => this.doses().filter(d => d.status === 'Confirmed').length);
  readonly dueCount     = computed(() => this.doses().filter(d => !!d.actionable).length);
  readonly overdueCount = computed(() =>
    this.doses().filter(d => !!d.actionable && d.minutes < this.nowMinutes()).length
  );

  /** Share of doses whose time has passed that were actually taken. */
  readonly adherencePct = computed(() => {
    const elapsed = this.doses().filter(d => d.minutes <= this.nowMinutes() && d.status !== 'Cancelled');
    if (elapsed.length === 0) return null;
    const taken = elapsed.filter(d => d.status === 'Confirmed').length;
    return Math.round((taken / elapsed.length) * 100);
  });

  /** The dose the page is really about: the next one still owed. */
  readonly nextDose = computed<Dose | null>(() => {
    const open = this.doses().filter(d => !!d.actionable);
    if (open.length === 0) return null;
    const now = this.nowMinutes();
    const overdue  = open.filter(d => d.minutes < now);
    // An overdue dose outranks an upcoming one; the most overdue comes first.
    if (overdue.length > 0) return overdue[0];
    return open.find(d => d.minutes >= now) ?? open[0];
  });

  readonly allDosesDone = computed(() =>
    this.doses().length > 0 && this.doses().every(d => !d.actionable)
  );

  readonly uniqueScheduledCount = computed(() => {
    const ids = new Set(this.todayLogs().map(l => l.medicineReminderId));
    return ids.size;
  });

  readonly unreadCount = this.notifSvc.unreadCount;

  // ── Auth helpers ──────────────────────────────────────────────────────────
  get displayName(): string {
    const u = this.authSvc.currentUser;
    return u?.firstName?.trim() || u?.email || 'there';
  }
  get userEmail(): string { return this.authSvc.currentUser?.email ?? ''; }

  get reminderFormErrors(): string[] {
    const f = this.reminderForm;
    const errs: string[] = [];
    const filled = f.reminderTimes.filter(t => t.trim());
    if (filled.length === 0) errs.push('Add at least one reminder time.');
    if (new Set(filled).size < filled.length) errs.push('Remove duplicate times.');
    if (f.repeatType !== 'Once' && f.endDate && f.startDate && f.endDate < f.startDate)
      errs.push('End date must be on or after start date.');
    return errs;
  }

  get reminderFormValid(): boolean { return this.reminderFormErrors.length === 0; }

  ngOnInit(): void {
    this.applyResponsiveSidebar();
    this.loadSchedule();
    this.loadMedicines();

    this.clockId = setInterval(() => this.nowMinutes.set(Medications.minutesNow()), 30_000);

    this.route.queryParams.subscribe(params => {
      if (params['tab'] === 'medications' || params['tab'] === 'schedule') {
        this.setTab(params['tab']);
      }

      const reminderId = params['reminderId'] ?? null;
      if (reminderId) {
        this.pendingReminderId.set(reminderId);
        this.highlightReminderId.set(reminderId);
        this.setTab('schedule');
        this.tryOpenReminderDetails();
      }
    });
  }

  ngOnDestroy(): void {
    if (this.clockId) clearInterval(this.clockId);
  }

  @HostListener('window:resize')
  onResize(): void { this.applyResponsiveSidebar(); }

  @HostListener('document:keydown.escape')
  onEsc(): void {
    if (this.showAddReminderModal())    { this.closeAddReminder(); return; }
    if (this.showDeleteReminderModal()) { this.closeDeleteReminder(); return; }
    if (this.showConfirmModal())        { this.closeConfirm(); }
  }

  @HostListener('document:click', ['$event'])
  onDocumentClick(e: MouseEvent): void {
    const t = e.target as HTMLElement;
    if (!t.closest('.hdr-user')) this.dropdownOpen.set(false);
  }

  private applyResponsiveSidebar(): void {
    this.sidebarCollapsed.set(window.innerWidth <= 1024);
    if (window.innerWidth > 768) this.mobileSidebarOpen.set(false);
  }

  toggleSidebar(): void       { this.sidebarCollapsed.update(v => !v); }
  toggleMobileSidebar(): void { this.mobileSidebarOpen.update(v => !v); }
  toggleDropdown(): void      { this.dropdownOpen.update(v => !v); }
  logout(): void { this.dropdownOpen.set(false); this.authSvc.logout().subscribe(); }

  // ── Tabs ──────────────────────────────────────────────────────────────────
  setTab(tab: MedTab): void {
    this.activeTab.set(tab);
    if (tab === 'medications' && this.medicines().length === 0 && !this.medsLoading()) {
      this.loadMedicines();
    }
  }

  goToMedsAndHighlight(): void {
    this.setTab('medications');
    this.highlightNoReminders.set(true);
    setTimeout(() => this.highlightNoReminders.set(false), 4000);
  }

  // ── Data loading ──────────────────────────────────────────────────────────
  loadSchedule(): void {
    this.scheduleLoading.set(true);
    this.scheduleError.set(null);
    this.medSvc.getToday().subscribe({
      next:  data => {
        this.todayLogs.set(data);
        this.scheduleLoading.set(false);
        this.tryOpenReminderDetails();
      },
      error: err  => {
        this.scheduleError.set(err?.error?.message ?? 'Could not load today\'s schedule.');
        this.scheduleLoading.set(false);
      },
    });
  }

  loadMedicines(): void {
    this.medsLoading.set(true);
    this.medsError.set(null);
    this.medSvc.getUserMedicines().subscribe({
      next: data => {
        this.medicines.set(data);
        this.medsLoading.set(false);
        this.loadAllMedicineReminders();
      },
      error: err => {
        this.medsError.set(err?.error?.message ?? 'Could not load medications.');
        this.medsLoading.set(false);
      },
    });
  }

  private loadAllMedicineReminders(): void {
    const meds = this.medicines();
    if (meds.length === 0) return;
    this.remindersLoading.set(true);
    const calls = Object.fromEntries(
      meds.map(m => [m.id, this.medSvc.getRemindersForMedicine(m.id)])
    );
    forkJoin(calls).subscribe({
      next:  result => { this.medicineReminders.set(result as Record<string, MedicineReminder[]>); this.remindersLoading.set(false); },
      error: ()     => this.remindersLoading.set(false),
    });
  }

  // ── Day rail ──────────────────────────────────────────────────────────────
  /** Position on the 24h rail, as a percentage. */
  railPos(minutes: number): number {
    return Math.min(100, Math.max(0, (minutes / 1440) * 100));
  }

  readonly railTicks = [
    { minutes: 360,  label: '6am'  },
    { minutes: 720,  label: 'noon' },
    { minutes: 1080, label: '6pm'  },
  ];

  isOverdue(dose: Dose): boolean {
    return !!dose.actionable && dose.minutes < this.nowMinutes();
  }

  /** Status used for colour, with overdue promoted above the raw log status. */
  doseState(dose: Dose): string {
    if (dose.status === 'Confirmed') return 'confirmed';
    if (dose.status === 'Cancelled') return 'cancelled';
    return this.isOverdue(dose) ? 'overdue' : dose.status.toLowerCase();
  }

  /** "in 1h 20m" / "Overdue by 15m" / "Taken" — the one line that answers "am I behind?" */
  countdownLabel(dose: Dose): string {
    if (dose.status === 'Confirmed') return 'Taken';
    if (dose.status === 'Cancelled') return 'Cancelled';

    const diff = dose.minutes - this.nowMinutes();
    if (diff <= 0) return `Overdue by ${this.durationLabel(-diff)}`;
    return `in ${this.durationLabel(diff)}`;
  }

  private durationLabel(mins: number): string {
    if (mins < 1) return 'less than a minute';
    const h = Math.floor(mins / 60);
    const m = mins % 60;
    if (h === 0) return `${m}m`;
    if (m === 0) return `${h}h`;
    return `${h}h ${m}m`;
  }

  attemptLabel(dose: Dose): string | null {
    if (dose.status === 'Confirmed' || dose.status === 'Cancelled') return null;
    if (dose.attempts <= 1) return null;
    return `${dose.attempts} reminders sent`;
  }

  // ── Confirm medication ────────────────────────────────────────────────────
  /** Confirms the newest unanswered log for a dose; the API cancels its follow-ups. */
  confirmDose(dose: Dose): void {
    if (dose.actionable) this.openConfirm(dose.actionable);
  }

  openConfirm(log: MedicationReminderLogDto): void {
    this.confirmingLog.set(log);
    this.showConfirmModal.set(true);
  }

  private tryOpenReminderDetails(): void {
    const reminderId = this.pendingReminderId();
    if (!reminderId) {
      return;
    }

    const match = this.todayLogs().find(log => log.id === reminderId);
    if (!match) {
      return;
    }

    this.pendingReminderId.set(null);
    this.openConfirm(match);

    void this.router.navigate([], {
      relativeTo: this.route,
      queryParams: { reminderId: null },
      queryParamsHandling: 'merge',
      replaceUrl: true,
    });
  }

  closeConfirm(): void {
    if (this.confirming()) return;
    this.showConfirmModal.set(false);
    this.confirmingLog.set(null);
  }

  submitConfirm(): void {
    const log = this.confirmingLog();
    if (!log || this.confirming()) return;

    this.confirming.set(true);
    this.medSvc.confirm(log.id).subscribe({
      next: () => {
        this.todayLogs.update(list =>
          list.map(l => l.id === log.id ? { ...l, status: 'Confirmed' as MedicationReminderStatus, confirmedAt: new Date().toISOString() } : l)
        );
        this.confirming.set(false);
        this.showConfirmModal.set(false);
        this.confirmingLog.set(null);
        this.toast(`${log.medicineName} marked as taken. Great job! 💊`, 'success');
        this.notifSvc.push({
          title: 'Medication Confirmed',
          body:  `${log.medicineName} ${log.dosage} taken at ${this.formatTime(log.scheduledTime)}`,
          type:  'reminder',
        });
      },
      error: err => {
        this.toast(err?.error?.message ?? 'Could not confirm medication.', 'error');
        this.confirming.set(false);
      },
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
  formatTime(time: string): string {
    // "08:00:00" → "08:00 AM"
    const [h, m] = time.split(':').map(Number);
    const period = h >= 12 ? 'PM' : 'AM';
    const hour   = h % 12 || 12;
    return `${String(hour).padStart(2, '0')}:${String(m).padStart(2, '0')} ${period}`;
  }

  formatDate(dateStr: string): string {
    return new Date(dateStr).toLocaleDateString('en-US', {
      weekday: 'short', month: 'short', day: 'numeric',
    });
  }

  statusLabel(status: MedicationReminderStatus): string {
    const map: Record<MedicationReminderStatus, string> = {
      Pending:   'Upcoming',
      Sent:      'Reminder Sent',
      Confirmed: 'Completed',
      Cancelled: 'Cancelled',
      Overdue:   'Missed',
    };
    return map[status] ?? status;
  }

  statusClass(status: MedicationReminderStatus): string {
    const map: Record<MedicationReminderStatus, string> = {
      Pending:   'pill pill-yellow-sm',
      Sent:      'pill pill-blue-sm',
      Confirmed: 'pill pill-green-sm',
      Cancelled: 'pill pill-red-sm',
      Overdue:   'pill pill-red-sm',
    };
    return map[status] ?? 'pill';
  }

  statusIcon(status: MedicationReminderStatus): string {
    const map: Record<MedicationReminderStatus, string> = {
      Pending:   'fa-clock',
      Sent:      'fa-bell',
      Confirmed: 'fa-circle-check',
      Cancelled: 'fa-ban',
      Overdue:   'fa-triangle-exclamation',
    };
    return map[status] ?? 'fa-circle';
  }

  canConfirm(status: MedicationReminderStatus): boolean {
    return status === 'Pending' || status === 'Sent' || status === 'Overdue';
  }

  reminderAttemptLabel(n: number): string {
    return n === 1 ? '1st reminder' : n === 2 ? '2nd reminder' : '3rd reminder';
  }

  sourceLabel(source: string): string {
    const map: Record<string, string> = {
      Manual:       'Manual',
      Prescription: 'Prescription',
      MedicineBox:  'Box Scan',
      '1': 'Manual',
      '2': 'Prescription',
      '3': 'Box Scan',
    };
    return map[source] ?? source;
  }

  sourceClass(source: string): string {
    if (source === 'Prescription' || source === '2') return 'pill pill-purple-sm';
    if (source === 'MedicineBox'  || source === '3') return 'pill pill-teal-sm';
    return 'pill pill-blue-sm';
  }

  medIconClass(index: number): string {
    const classes = ['med-ico-orange', 'med-ico-yellow', 'med-ico-pink', 'med-ico-blue', 'med-ico-teal'];
    return classes[index % classes.length];
  }

  todayLabel(): string {
    return new Date().toLocaleDateString('en-US', {
      weekday: 'long', month: 'long', day: 'numeric',
    });
  }

  // ── Reminder display helpers ──────────────────────────────────────────────
  getReminders(medId: string): MedicineReminder[] {
    return this.medicineReminders()[medId] ?? [];
  }

  hasReminders(medId: string): boolean {
    return this.getReminders(medId).length > 0;
  }

  hasActiveReminders(medId: string): boolean {
    const reminders = this.getReminders(medId);
    return reminders.length > 0 && reminders.some(r => r.isEnabled);
  }

  getSortedReminderTimes(medId: string): string[] {
    return [...this.getReminders(medId)]
      .sort((a, b) => a.reminderTime.localeCompare(b.reminderTime))
      .map(r => this.formatTime(r.reminderTime));
  }

  isPaused(medId: string): boolean {
    const reminders = this.getReminders(medId);
    return reminders.length > 0 && reminders.every(r => !r.isEnabled);
  }

  getRepeatType(medId: string): string {
    return this.getReminders(medId)[0]?.repeatType ?? '';
  }

  // ── Add Reminder modal ────────────────────────────────────────────────────
  openAddReminder(med: UserMedicine): void {
    this.reminderForm = this.emptyReminderForm();
    this.editMode.set(false);
    this.editReminderIds.set([]);
    this.addReminderMedicineId.set(med.id);
    this.addReminderMedicineName.set(med.medicineName);
    this.showAddReminderModal.set(true);
  }

  openEditReminder(med: UserMedicine): void {
    const reminders = this.getReminders(med.id);
    const first = reminders[0];
    if (!first) return;
    const today = Medications.localToday();
    this.reminderForm = {
      reminderTimes: reminders.map(r => r.reminderTime.substring(0, 5)),
      repeatType: (first.repeatType as RepeatOption) ?? 'Daily',
      startDate: first.startDate?.slice(0, 10) ?? today,
      endDate:   first.endDate?.slice(0, 10)   ?? today,
      notificationsEnabled: true,
      notes: '',
    };
    this.editMode.set(true);
    this.editReminderIds.set(reminders.map(r => r.id));
    this.addReminderMedicineId.set(med.id);
    this.addReminderMedicineName.set(med.medicineName);
    this.showAddReminderModal.set(true);
  }

  closeAddReminder(): void {
    if (this.addReminderSaving()) return;
    this.showAddReminderModal.set(false);
    this.addReminderMedicineId.set(null);
    this.addReminderMedicineName.set('');
    this.editMode.set(false);
    this.editReminderIds.set([]);
  }

  saveNewReminder(): void {
    if (!this.reminderFormValid) return;
    const medId = this.addReminderMedicineId();
    if (!medId) return;

    this.addReminderSaving.set(true);
    const f = this.reminderForm;
    const payload: CreateReminderPayload = {
      userMedicineId: medId,
      times:          f.reminderTimes.filter(t => t.trim()),
      startDate:      f.startDate,
      endDate:        f.repeatType === 'Once' ? f.startDate : f.endDate,
      repeatType:     f.repeatType,
    };
    const medName = this.addReminderMedicineName();

    const doCreate = () => {
      this.medSvc.createReminder(medId, payload).subscribe({
        next: () => {
          this.addReminderSaving.set(false);
          this.closeAddReminder();
          this.notifSvc.notifyReminderChanged();
          this.toast(`Reminder set for ${medName}.`, 'success');
        },
        error: err => {
          this.toast(err?.error?.message ?? 'Could not save reminder.', 'error');
          this.addReminderSaving.set(false);
        },
      });
    };

    if (this.editMode()) {
      const ids = this.editReminderIds();
      const newTimes = f.reminderTimes.filter(t => t.trim());
      const maxLen = Math.max(ids.length, newTimes.length);
      const operations: Observable<any>[] = [];

      for (let i = 0; i < maxLen; i++) {
        if (i < newTimes.length && i < ids.length) {
          // Update existing
          operations.push(
            this.medSvc.updateReminder(ids[i], {
              id: ids[i],
              reminderTime: newTimes[i],
              startDate: f.startDate,
              endDate: f.repeatType === 'Once' ? f.startDate : f.endDate,
              repeatType: f.repeatType,
            })
          );
        } else if (i < newTimes.length && i >= ids.length) {
          // Create new single time
          operations.push(
            this.medSvc.createReminder(medId, {
              userMedicineId: medId,
              times: [newTimes[i]],
              startDate: f.startDate,
              endDate: f.repeatType === 'Once' ? f.startDate : f.endDate,
              repeatType: f.repeatType,
            })
          );
        } else if (i >= newTimes.length && i < ids.length) {
          // Delete removed time
          operations.push(this.medSvc.deleteReminder(ids[i]));
        }
      }

      if (operations.length > 0) {
        forkJoin(operations).subscribe({
          next: () => {
            this.addReminderSaving.set(false);
            this.closeAddReminder();
            this.notifSvc.notifyReminderChanged();
            this.toast(`Reminder updated for ${medName}.`, 'success');
          },
          error: err => {
            this.toast(err?.error?.message ?? 'Could not update reminder.', 'error');
            this.addReminderSaving.set(false);
          },
        });
      } else {
        this.addReminderSaving.set(false);
        this.closeAddReminder();
      }
    } else {
      doCreate();
    }
  }

  // ── Toggle (pause/resume) all reminders for a medicine ───────────────────
  toggleAllReminders(medId: string): void {
    const reminders = this.getReminders(medId);
    if (reminders.length === 0) return;

    // Optimistic update
    const oldReminders = [...reminders];
    this.medicineReminders.update(rec => ({
      ...rec,
      [medId]: (rec[medId] ?? []).map(r => ({ ...r, isEnabled: !r.isEnabled })),
    }));

    forkJoin(reminders.map(r => this.medSvc.toggleReminderStatus(r.id))).subscribe({
      next: () => {
        const isNowPaused = this.isPaused(medId);
        const medName = this.medicines().find(m => m.id === medId)?.medicineName ?? 'medicine';
        this.notifSvc.notifyReminderChanged();
        this.toast(`Reminders ${isNowPaused ? 'paused' : 'resumed'} for ${medName}.`, 'success');
      },
      error: err => {
        // Revert on error
        this.medicineReminders.update(rec => ({
          ...rec,
          [medId]: oldReminders,
        }));
        this.toast(err?.error?.message ?? 'Could not toggle reminders.', 'error');
      },
    });
  }

  // ── Delete Reminder confirm modal ─────────────────────────────────────────
  openDeleteReminder(medId: string, medName: string): void {
    this.deleteReminderMedId.set(medId);
    this.deleteReminderMedName.set(medName);
    this.showDeleteReminderModal.set(true);
  }

  closeDeleteReminder(): void {
    if (this.deletingReminder()) return;
    this.showDeleteReminderModal.set(false);
    this.deleteReminderMedId.set(null);
    this.deleteReminderMedName.set('');
  }

  confirmDeleteReminder(): void {
    const medId = this.deleteReminderMedId();
    if (!medId) return;
    const reminders = this.getReminders(medId);
    if (reminders.length === 0) { this.closeDeleteReminder(); return; }

    this.deletingReminder.set(true);
    const medName = this.deleteReminderMedName();

    // Optimistic update
    const oldReminders = [...reminders];
    this.medicineReminders.update(rec => ({ ...rec, [medId]: [] }));

    forkJoin(reminders.map(r => this.medSvc.deleteReminder(r.id))).subscribe({
      next: () => {
        this.deletingReminder.set(false);
        this.closeDeleteReminder();
        this.notifSvc.notifyReminderChanged();
        this.toast(`Reminders deleted for ${medName}.`, 'success');
      },
      error: err => {
        // Revert on error
        this.medicineReminders.update(rec => ({ ...rec, [medId]: oldReminders }));
        this.toast(err?.error?.message ?? 'Could not delete reminders.', 'error');
        this.deletingReminder.set(false);
      },
    });
  }

  // ── Reminder form helpers ─────────────────────────────────────────────────
  addReminderTime(): void {
    this.reminderForm = { ...this.reminderForm, reminderTimes: [...this.reminderForm.reminderTimes, ''] };
  }

  removeReminderTime(i: number): void {
    if (this.reminderForm.reminderTimes.length <= 1) return;
    const times = this.reminderForm.reminderTimes.filter((_, idx) => idx !== i);
    this.reminderForm = { ...this.reminderForm, reminderTimes: times };
  }

  setRepeatType(val: RepeatOption): void {
    this.reminderForm = { ...this.reminderForm, repeatType: val };
    if (val === 'Once') {
      this.reminderForm = { ...this.reminderForm, endDate: this.reminderForm.startDate };
    }
  }

  onReminderStartDateChange(): void {
    if (this.reminderForm.repeatType === 'Once') {
      this.reminderForm = { ...this.reminderForm, endDate: this.reminderForm.startDate };
    }
  }

  updateReminderTime(index: number, value: string): void {
    const times = [...this.reminderForm.reminderTimes];
    times[index] = value;
    this.reminderForm = { ...this.reminderForm, reminderTimes: times };
  }

  private emptyReminderForm(): ReminderForm {
    const today = Medications.localToday();
    return {
      reminderTimes:        ['08:00'],
      repeatType:           'Daily',
      startDate:            today,
      endDate:              today,
      notificationsEnabled: true,
      notes:                '',
    };
  }
}
