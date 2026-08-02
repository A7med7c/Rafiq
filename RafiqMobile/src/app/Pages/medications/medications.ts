import {
  Component, effect, inject, OnInit, OnDestroy, signal, computed, HostListener,
} from '@angular/core';
import { CommonModule } from '@angular/common';
import { FormsModule } from '@angular/forms';
import { ActivatedRoute, Router, RouterLink, RouterLinkActive } from '@angular/router';
import { toSignal } from '@angular/core/rxjs-interop';
import { forkJoin, Observable, of, map, switchMap } from 'rxjs';
import { AuthService } from '../../Services/auth-service';
import { ProfileCacheService } from '../../Services/profile-cache.service';
import { AiChatService } from '../../Services/ai-chat.service';
import { NotificationService } from '../../Services/notification.service';
import { AllergyCheckResult, MedicationRemindersService } from '../../Services/medication-reminders.service';
import { FamilyProfileBannerComponent } from '../../Components/family-profile-banner/family-profile-banner';
import { BottomNav } from '../../shared/bottom-nav/bottom-nav';
import { MobileHeader } from '../../shared/mobile-header/mobile-header';
import { MedicationReminderLogDto, MedicationReminderStatus } from '../../Modles/medication-reminder.models';
import { AddUserMedicinePayload, CreateReminderPayload, MedicineReminder, UpdateReminderPayload, UpdateUserMedicinePayload, UserMedicine } from '../../Modles/dashboard.models';
import { LocalizationService } from '../../Services/localization.service';
import { FamilyProfilesService, AccessibleProfileDto } from '../../Services/family-profiles.service';
import { ProfileSelectionService } from '../../Services/profile-selection.service';
import { AssistantAnchorDirective } from '../../core/assistant/directives/assistant-anchor.directive';
import { ReviewTrackingService } from '../../Services/review-tracking.service';

type MedTab = 'schedule' | 'medications';
type MedSubTab = 'all' | 'with-reminder' | 'no-reminder' | 'paused';
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

interface AddMedForm {
  medicineName: string;
  dosage: string;
  frequency: string;
  duration: string;
  notes: string;
}

/**
 * Display-only state for a single reminder attempt chip.
 *
 * Upcoming   — scheduled, not yet fired (Pending)
 * Sent       — notification delivered, awaiting confirmation (Sent, actionable)
 * Overdue    — attempt time elapsed with no notification sent, still actionable
 * NoResponse — earlier attempt passed without user response (not actionable)
 * Completed  — user confirmed this occurrence
 * Cancelled  — attempt cancelled (dose confirmed via a different attempt)
 */
type AttemptState = 'Upcoming' | 'Sent' | 'Overdue' | 'NoResponse' | 'Completed' | 'Cancelled';

interface AttemptView {
  reminderNumber: number;
  state: AttemptState;
  /** Wall-clock time this specific attempt fires, taken directly from the backend log. */
  scheduledTime: string;
}

/**
 * One scheduled dose. The backend creates three attempt logs per occurrence;
 * this view folds them into a single card representing the medication the patient
 * actually has to take.
 */
interface Dose {
  key: string;
  medicineName: string;
  dosage: string;
  /** Authoritative dose time from MedicineReminder.ReminderTime — the user-configured time. */
  scheduledTime: string;
  minutes: number;
  status: MedicationReminderStatus;
  attempts: number;
  /** Each attempt rendered as a timeline chip. */
  attemptStates: AttemptView[];
  confirmedAt: string | null;
  /**
   * The one log the user should confirm right now.
   * Null when the occurrence is done (Confirmed) or all Cancelled.
   * Determined server-side via IsActionable — not inferred on the frontend.
   */
  actionable: MedicationReminderLogDto | null;
  ids: string[];
}

@Component({
  selector: 'app-medications',
  standalone: true,
  imports: [CommonModule, FormsModule, AssistantAnchorDirective, FamilyProfileBannerComponent, BottomNav, MobileHeader],
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

  private readonly authSvc = inject(AuthService);
  protected readonly profileCache = inject(ProfileCacheService);
  protected readonly notifSvc = inject(NotificationService);
  private readonly medSvc = inject(MedicationRemindersService);
  private readonly route = inject(ActivatedRoute);
  protected readonly router = inject(Router);
  protected readonly l10n = inject(LocalizationService);
  protected readonly aiChatService = inject(AiChatService);
  protected readonly t = this.l10n.t;
  private readonly fpSvc = inject(FamilyProfilesService);
  private readonly profileSelectSvc = inject(ProfileSelectionService);
  private readonly reviewTracking = inject(ReviewTrackingService);

  private readonly medicationRefreshEffect = effect(() => {
    if (this.notifSvc.reminderDataRefreshTick() === 0) {
      return;
    }

    this.loadSchedule();
    this.loadAllMedicineReminders();
  });

  // ── Family profile override (set from query params) ──────────────────────
  readonly fpProfileId = signal<string | null>(null);
  readonly fpProfileName = signal<string | null>(null);

  // Derive readOnly from the accessible-profiles endpoint so it is always
  // authoritative — even when the user navigates here via the sidebar without
  // query params but with a Viewer-access profile selected in localStorage.
  private readonly _viewingMedProfile = toSignal<AccessibleProfileDto | null>(
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

  readonly fpReadOnly = computed(() => this._viewingMedProfile()?.accessRole === 'Viewer');

  /** The resolved family profile for template use (banner name + relationship). */
  readonly viewingFpProfile = computed(() => this._viewingMedProfile());
  readonly fpBannerName = computed(() => {
    const p = this._viewingMedProfile();
    return p && !p.isSelf ? `${p.firstName} ${p.lastName}` : null;
  });
  readonly fpBannerRelationship = computed(() => {
    const p = this._viewingMedProfile();
    return p && !p.isSelf ? (p.relationship ?? null) : null;
  });

  /** Display label for the owner of every dose card on this page. */
  readonly doseOwnerLabel = computed<string>(() => {
    const profile = this._viewingMedProfile();
    if (!profile || profile.isSelf) return 'You';
    const rel = profile.relationship ? ` (${profile.relationship})` : '';
    return `${profile.firstName}${rel}`;
  });

  // ── Layout ──────────────────────────────────────────────────────────────
  readonly sidebarCollapsed = signal(false);
  readonly mobileSidebarOpen = signal(false);
  readonly dropdownOpen = signal(false);

  // ── Tabs ─────────────────────────────────────────────────────────────────
  readonly activeTab = signal<MedTab>('schedule');

  // ── Today's Schedule ──────────────────────────────────────────────────────
  readonly todayLogs = signal<MedicationReminderLogDto[]>([]);
  readonly scheduleLoading = signal(true);
  readonly scheduleError = signal<string | null>(null);

  // ── My Medications ────────────────────────────────────────────────────────
  readonly medicines = signal<UserMedicine[]>([]);
  readonly medsLoading = signal(false);
  readonly medsError = signal<string | null>(null);

  // ── My Medications — sub-tab, search, filters, pagination, menus ──────────
  readonly medSubTab = signal<MedSubTab>('all');
  readonly medSearch = signal('');
  readonly medSourceFilter = signal('');
  readonly openMedMenuId = signal<string | null>(null);
  readonly viewingMed = signal<UserMedicine | null>(null);
  readonly medPage = signal(1);
  private readonly MED_PAGE_SIZE = 2;
  readonly mobileTabMenuOpen = signal(false);

  // ── Medicine Reminders ────────────────────────────────────────────────────
  readonly medicineReminders = signal<Record<string, MedicineReminder[]>>({});
  readonly remindersLoading = signal(false);
  readonly highlightNoReminders = signal(false);

  readonly hasAnyReminders = computed(() => {
    return this.medicines().some(m => this.hasReminders(m.id));
  });

  // ── Add / Edit Reminder modal ─────────────────────────────────────────────
  readonly editMode = signal(false);
  private readonly editReminderIds = signal<string[]>([]);
  readonly showAddReminderModal = signal(false);
  readonly addReminderMedicineId = signal<string | null>(null);
  readonly addReminderMedicineName = signal('');
  readonly addReminderSaving = signal(false);

  // ── Delete Reminder confirm modal ─────────────────────────────────────────
  readonly showDeleteReminderModal = signal(false);
  readonly deleteReminderMedId = signal<string | null>(null);
  readonly deleteReminderMedName = signal('');
  readonly deletingReminder = signal(false);

  // ── Confirm modal ─────────────────────────────────────────────────────────
  readonly showConfirmModal = signal(false);
  readonly confirmingLog = signal<MedicationReminderLogDto | null>(null);
  readonly confirming = signal(false);
  readonly pendingReminderId = signal<string | null>(null);
  readonly highlightReminderId = signal<string | null>(null);
  readonly pendingReminderMedicineId = signal<string | null>(null);

  // ── Add Medicine modal ────────────────────────────────────────────────────
  readonly showAddMedModal = signal(false);
  readonly addMedSaving = signal(false);
  readonly addMedTouched = signal(false);

  // ── Allergy safety check ──────────────────────────────────────────────────
  readonly allergyChecking = signal(false);
  readonly showAllergyWarning = signal(false);
  readonly allergyCheckResult = signal<AllergyCheckResult | null>(null);
  private _pendingMedPayload: AddUserMedicinePayload | null = null;

  // ── Edit Medicine modal ───────────────────────────────────────────────────
  readonly showEditMedModal = signal(false);
  readonly editingMed = signal<UserMedicine | null>(null);
  readonly editMedSaving = signal(false);
  readonly editMedTouched = signal(false);

  // ── Delete Medicine confirm modal ─────────────────────────────────────────
  readonly showDeleteMedModal = signal(false);
  readonly deleteMedId = signal<string | null>(null);
  readonly deleteMedName = signal('');
  readonly deletingMed = signal(false);

  // ── Reminder form ─────────────────────────────────────────────────────────
  reminderForm: ReminderForm = this.emptyReminderForm();

  // ── Add / Edit Medicine forms ─────────────────────────────────────────────
  addMedForm: AddMedForm = this.emptyAddMedForm();
  editMedForm: AddMedForm = this.emptyAddMedForm();
  readonly repeatOptions: { label: string; value: RepeatOption }[] = [
    { label: 'Once', value: 'Once' },
    { label: 'Daily', value: 'Daily' },
    { label: 'Weekly', value: 'Weekly' },
    { label: 'Monthly', value: 'Monthly' },
  ];

  // ── Toasts ───────────────────────────────────────────────────────────────
  private toastSeq = 0;
  readonly toasts = signal<Toast[]>([]);

  // ── Clock ────────────────────────────────────────────────────────────────
  /** Minutes since midnight, ticked every 30s so the rail and countdowns stay live. */
  readonly nowMinutes = signal(Medications.minutesNow());
  private clockId?: ReturnType<typeof setInterval>;

  // ── Doses (escalation logs folded into the single dose they represent) ──────
  readonly doses = computed<Dose[]>(() => {
    const today = Medications.localToday();
    this.nowMinutes(); // read so computed re-evaluates on clock ticks

    const groups = new Map<string, MedicationReminderLogDto[]>();

    for (const log of this.todayLogs()) {
      // Safety-filter: backend /today endpoint scopes by the reminder timezone's "today",
      // but the client may be in a different timezone; only show logs whose date matches.
      if (log.scheduledDate !== today) continue;

      // Group by (medicineReminderId, scheduledDate) — the same key the unique filtered
      // index guarantees maps to exactly one dose occurrence.
      const key = `${log.medicineReminderId}|${log.scheduledDate}`;
      const bucket = groups.get(key);
      if (bucket) bucket.push(log);
      else groups.set(key, [log]);
    }

    const doses: Dose[] = [];

    for (const [key, logs] of groups) {
      const ordered = [...logs].sort((a, b) => a.reminderNumber - b.reminderNumber);

      // All logs in the group share the same MedicineReminder.ReminderTime — the user's
      // configured dose time.  Use it directly instead of inferring from ReminderNumber.
      const anyLog = ordered[0];
      const doseTime = anyLog.reminderTime;   // e.g. "20:00:00"

      // The actionable log is determined by the backend (IsActionable flag).
      // At most one log per occurrence has it set.  Do not re-derive it here.
      const actionable = ordered.find(l => l.isActionable) ?? null;
      const confirmed = ordered.find(l => l.status === 'Confirmed') ?? null;

      let status: MedicationReminderStatus;
      if (confirmed) status = 'Confirmed';
      else if (actionable) status = actionable.status;
      else if (ordered.every(l => l.status === 'Cancelled')) status = 'Cancelled';
      else status = anyLog.status;

      // Map each log to its display chip.
      //
      // Attempt statuses are kept strictly separate from the dose-level status:
      //   • NoResponse — earlier reminder fired (Sent) or was skipped (Overdue) but user
      //                  didn't respond; the dose cycle moved to the next attempt.
      //   • Overdue    — the actionable attempt whose configured time has already passed
      //                  (no notification was sent because scheduling was late).
      //   • Sent       — the current actionable attempt; notification was delivered, awaiting
      //                  confirmation.
      //   • Upcoming   — a future Pending attempt not yet fired.
      const attemptStates: AttemptView[] = ordered.map(l => {
        let state: AttemptState;
        if (confirmed) {
          state = l.status === 'Confirmed' ? 'Completed' : 'Cancelled';
        } else if (l.status === 'Confirmed') {
          state = 'Completed';
        } else if (l.status === 'Cancelled') {
          state = 'Cancelled';
        } else if (l.isActionable) {
          // This is the one attempt the user should act on right now.
          state = l.status === 'Sent' ? 'Sent'
            : l.status === 'Overdue' ? 'Overdue'
              : 'Upcoming';
        } else if (l.status === 'Sent' || l.status === 'Overdue') {
          // Earlier attempt passed without user response — label it accordingly,
          // not as "Missed" (the dose is still active and can still be confirmed).
          state = 'NoResponse';
        } else {
          state = 'Upcoming';
        }
        return { reminderNumber: l.reminderNumber, state, scheduledTime: l.scheduledTime };
      });

      doses.push({
        key,
        medicineName: anyLog.medicineName,
        dosage: anyLog.dosage,
        scheduledTime: doseTime,
        minutes: Medications.toMinutes(doseTime),
        status,
        attempts: ordered.length,
        attemptStates,
        confirmedAt: confirmed?.confirmedAt ?? null,
        actionable,
        ids: ordered.map(l => l.id),
      });
    }

    return doses.sort((a, b) => a.minutes - b.minutes);
  });

  /**
   * Reactive display order for Today's Schedule.
   *
   * Priority tiers (re-evaluated whenever nowMinutes or doses change):
   *   0 — overdue + still actionable  → ascending by time (most overdue first)
   *   1 — upcoming + actionable        → ascending by time (nearest first)
   *   2 — confirmed                    → descending by time (most recent first)
   *   3 — cancelled                    → descending by time
   */
  readonly sortedDoses = computed<Dose[]>(() => {
    const now = this.nowMinutes();
    const priority = (d: Dose): number => {
      if (d.actionable && d.minutes < now) return 0;
      if (d.actionable) return 1;
      if (d.status === 'Confirmed') return 2;
      return 3;
    };
    return [...this.doses()].sort((a, b) => {
      const pa = priority(a), pb = priority(b);
      if (pa !== pb) return pa - pb;
      // actionable tiers: nearest (ascending); done tiers: most-recent-first (descending)
      return pa <= 1 ? a.minutes - b.minutes : b.minutes - a.minutes;
    });
  });

  readonly takenCount = computed(() => this.doses().filter(d => d.status === 'Confirmed').length);

  /** All doses that still need a confirmation (both upcoming and overdue-but-still-active). */
  readonly dueCount = computed(() => this.doses().filter(d => !!d.actionable).length);

  /**
   * Doses whose configured time has NOT yet passed and still have an actionable attempt.
   * A dose counted here is never counted in overdueCount — no double-counting.
   */
  readonly upcomingCount = computed(() =>
    this.doses().filter(d => !!d.actionable && d.minutes >= this.nowMinutes()).length
  );

  /**
   * Doses whose configured time HAS passed and still have an actionable attempt.
   * These doses are overdue but not yet confirmed — the user can still confirm them.
   * This is mutually exclusive with upcomingCount.
   */
  readonly overdueCount = computed(() =>
    this.doses().filter(d => !!d.actionable && d.minutes < this.nowMinutes()).length
  );

  readonly cancelledCount = computed(() =>
    this.doses().filter(d => d.status === 'Cancelled').length
  );

  /** Share of non-cancelled doses that were actually taken. */
  readonly adherencePct = computed(() => {
    const nonCancelled = this.doses().filter(d => d.status !== 'Cancelled');
    if (nonCancelled.length === 0) return null;
    const taken = nonCancelled.filter(d => d.status === 'Confirmed').length;
    return Math.round((taken / nonCancelled.length) * 100);
  });

  /** The dose the page is really about: the next one still owed. */
  readonly nextDose = computed<Dose | null>(() => {
    const open = this.doses().filter(d => !!d.actionable);
    if (open.length === 0) return null;
    const now = this.nowMinutes();
    const overdue = open.filter(d => d.minutes < now);
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

  // ── My Medications computed ────────────────────────────────────────────────
  readonly withReminderCount = computed(() =>
    this.medicines().filter(m => this.hasReminders(m.id)).length
  );
  readonly noReminderCount = computed(() =>
    this.medicines().filter(m => !this.hasReminders(m.id)).length
  );
  readonly activeMedCount = computed(() =>
    this.medicines().filter(m => this.hasActiveReminders(m.id)).length
  );
  readonly pausedMedCount = computed(() =>
    this.medicines().filter(m => this.isPaused(m.id)).length
  );
  readonly filteredMedicines = computed(() => {
    const subTab = this.medSubTab();
    const q = this.medSearch().toLowerCase().trim();
    const src = this.medSourceFilter();
    return this.medicines().filter(m => {
      if (subTab === 'with-reminder' && !this.hasReminders(m.id)) return false;
      if (subTab === 'no-reminder' && this.hasReminders(m.id)) return false;
      if (subTab === 'paused' && !this.isPaused(m.id)) return false;
      if (q && !m.medicineName.toLowerCase().includes(q) && !m.dosage.toLowerCase().includes(q)) return false;
      if (src && m.source !== src) return false;
      return true;
    });
  });
  readonly medTotalPages = computed(() =>
    Math.max(1, Math.ceil(this.filteredMedicines().length / this.MED_PAGE_SIZE))
  );
  readonly paginatedMedicines = computed(() => {
    const page = this.medPage();
    const start = (page - 1) * this.MED_PAGE_SIZE;
    return this.filteredMedicines().slice(start, start + this.MED_PAGE_SIZE);
  });
  readonly medPageNumbers = computed(() =>
    Array.from({ length: this.medTotalPages() }, (_, i) => i + 1)
  );

  readonly unreadCount = this.notifSvc.unreadCount;

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

  // ── Reminder form validation ──────────────────────────────────────────────

  /** Per-time-index inline errors: past-time or duplicate. */
  get timeErrors(): Record<number, string> {
    const result: Record<number, string> = {};
    const today = Medications.localToday();
    const isToday = this.reminderForm.startDate === today;
    const nowMins = this.nowMinutes();

    // Build a map of value → [indices] to detect duplicates
    const seen = new Map<string, number[]>();
    this.reminderForm.reminderTimes.forEach((t, i) => {
      if (!t.trim()) return;
      const arr = seen.get(t) ?? [];
      arr.push(i);
      seen.set(t, arr);
    });

    this.reminderForm.reminderTimes.forEach((t, i) => {
      if (!t.trim()) return;
      if ((seen.get(t)?.length ?? 0) > 1) {
        result[i] = 'This reminder time already exists.';
        return;
      }
      if (isToday) {
        const [h, m] = t.split(':').map(Number);
        if (h * 60 + m < nowMins) {
          result[i] = 'The selected time has already passed.';
        }
      }
    });

    return result;
  }

  /** Global form errors — shown in the bottom summary panel. */
  get reminderFormErrors(): string[] {
    const errs: string[] = [];
    if (this.reminderForm.reminderTimes.filter(t => t.trim()).length === 0)
      errs.push('Add at least one reminder time.');
    return errs;
  }

  /** True only when global errors, per-time errors, and date errors are all clear. */
  get reminderFormValid(): boolean {
    const f = this.reminderForm;
    const today = Medications.localToday();
    if (this.reminderFormErrors.length > 0) return false;
    if (Object.keys(this.timeErrors).length > 0) return false;
    if (f.startDate && f.startDate < today) return false;
    if (f.repeatType !== 'Once') {
      if (f.endDate && f.endDate < today) return false;
      if (f.endDate && f.startDate && f.endDate < f.startDate) return false;
    }
    return true;
  }

  /** Today's date string "YYYY-MM-DD" used for [min] bindings in the template. */
  get todayDate(): string { return Medications.localToday(); }

  /** Minimum allowed end date: whichever is later — today or the chosen start date. */
  get minEndDate(): string {
    const today = Medications.localToday();
    const start = this.reminderForm.startDate;
    return start > today ? start : today;
  }

  /** Current time as "HH:MM" derived from the live nowMinutes signal. */
  get currentTimeHHMM(): string {
    const m = this.nowMinutes();
    return `${String(Math.floor(m / 60)).padStart(2, '0')}:${String(m % 60).padStart(2, '0')}`;
  }

  get addMedErrors(): string[] {
    const f = this.addMedForm;
    const errs: string[] = [];
    if (!f.medicineName.trim()) errs.push('Medicine name is required.');
    if (!f.dosage.trim()) errs.push('Dosage is required.');
    if (!f.frequency.trim()) errs.push('Frequency is required.');
    if (!f.duration.trim()) errs.push('Duration is required.');
    return errs;
  }

  ngOnInit(): void {
    this.profileCache.ensure();
    this.applyResponsiveSidebar();

    this.clockId = setInterval(() => this.nowMinutes.set(Medications.minutesNow()), 30_000);

    this.route.queryParams.subscribe(params => {
      const fpId = params['profileId'] ?? this.profileSelectSvc.selectedProfileId ?? null;
      if (fpId !== this.fpProfileId()) {
        this.fpProfileId.set(fpId);
        this.fpProfileName.set(params['name'] ?? null);
      }

      this.loadSchedule();
      this.loadMedicines();

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

      const medicineId = params['medicineId'] ?? null;
      if (medicineId) {
        this.pendingReminderMedicineId.set(medicineId);
        this.tryOpenAddReminderForMedicine();
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
    if (this.showEditMedModal()) { this.closeEditMed(); return; }
    if (this.showDeleteMedModal()) { this.closeDeleteMed(); return; }
    if (this.showAddMedModal()) { this.closeAddMed(); return; }
    if (this.showAddReminderModal()) { this.closeAddReminder(); return; }
    if (this.showDeleteReminderModal()) { this.closeDeleteReminder(); return; }
    if (this.viewingMed()) { this.closeMedView(); return; }
    if (this.showConfirmModal()) { this.closeConfirm(); }
  }

  @HostListener('document:click', ['$event'])
  onDocumentClick(e: MouseEvent): void {
    const t = e.target as HTMLElement;
    if (!t.closest('.hdr-user')) this.dropdownOpen.set(false);
    if (!t.closest('.record-actions')) this.openMedMenuId.set(null);
  }

  private applyResponsiveSidebar(): void {
    this.sidebarCollapsed.set(window.innerWidth <= 1024);
    if (window.innerWidth > 768) this.mobileSidebarOpen.set(false);
  }

  toggleSidebar(): void { this.sidebarCollapsed.update(v => !v); }
  toggleMobileSidebar(): void { this.mobileSidebarOpen.update(v => !v); }
  toggleDropdown(): void { this.dropdownOpen.update(v => !v); }
  logout(): void { this.dropdownOpen.set(false); this.authSvc.logout().subscribe(); }

  goToMyProfile(): void { this.dropdownOpen.set(false); this.router.navigate(['/my-profile']); }
  openRatingPopup(): void { this.dropdownOpen.set(false); this.reviewTracking.openManually(); }

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
    this.medSvc.getToday(this.fpProfileId() ?? undefined).subscribe({
      next: data => {
        this.todayLogs.set(data);
        this.scheduleLoading.set(false);
        this.tryOpenReminderDetails();
      },
      error: err => {
        this.scheduleError.set(err?.error?.message ?? 'Could not load today\'s schedule.');
        this.scheduleLoading.set(false);
      },
    });
  }

  loadMedicines(): void {
    this.medsLoading.set(true);
    this.medsError.set(null);
    this.medSvc.getUserMedicines(this.fpProfileId() ?? undefined).subscribe({
      next: data => {
        this.medicines.set(data);
        this.medsLoading.set(false);
        this.loadAllMedicineReminders();
        this.tryOpenAddReminderForMedicine();
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
      next: result => { this.medicineReminders.set(result as Record<string, MedicineReminder[]>); this.remindersLoading.set(false); },
      error: () => this.remindersLoading.set(false),
    });
  }

  // ── Day rail ──────────────────────────────────────────────────────────────
  /** Position on the 24h rail, as a percentage. */
  railPos(minutes: number): number {
    return Math.min(100, Math.max(0, (minutes / 1440) * 100));
  }

  readonly railTicks = [
    { minutes: 360, label: '6am' },
    { minutes: 720, label: 'noon' },
    { minutes: 1080, label: '6pm' },
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

  attemptPillClass(state: AttemptState): string {
    const map: Record<AttemptState, string> = {
      Upcoming: 'pill pill-yellow-sm',
      Sent: 'pill pill-blue-sm',
      Overdue: 'pill pill-orange-sm',
      NoResponse: 'pill pill-gray-sm',
      Completed: 'pill pill-green-sm',
      Cancelled: 'pill pill-gray-sm',
    };
    return map[state];
  }

  attemptIcon(state: AttemptState): string {
    const map: Record<AttemptState, string> = {
      Upcoming: 'fa-clock',
      Sent: 'fa-bell',
      Overdue: 'fa-hourglass-half',
      NoResponse: 'fa-minus-circle',
      Completed: 'fa-circle-check',
      Cancelled: 'fa-ban',
    };
    return map[state];
  }

  attemptLabel(state: AttemptState): string {
    const map: Record<AttemptState, string> = {
      Upcoming: 'Upcoming',
      Sent: 'Sent',
      Overdue: 'Overdue',
      NoResponse: 'No Response',
      Completed: 'Completed',
      Cancelled: 'Cancelled',
    };
    return map[state];
  }

  /**
   * Returns the interval in minutes between attempt 1 and attempt 2,
   * derived from the actual scheduled times on the dose.
   * Returns null when fewer than 2 attempts exist.
   */
  doseInterval(dose: Dose): number | null {
    if (dose.attemptStates.length < 2) return null;
    const t1 = Medications.toMinutes(dose.attemptStates[0].scheduledTime);
    const t2 = Medications.toMinutes(dose.attemptStates[1].scheduledTime);
    return Math.max(0, t2 - t1);
  }

  // ── Confirm medication ────────────────────────────────────────────────────
  /** Confirms the newest unanswered log for a dose; the API cancels its follow-ups. */
  confirmDose(dose: Dose): void {
    if (this.fpReadOnly()) return;
    if (dose.actionable) this.openConfirm(dose.actionable);
  }

  openConfirm(log: MedicationReminderLogDto): void {
    if (this.fpReadOnly()) return;
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

    // A notification can go stale (already confirmed/cancelled elsewhere) by the time it's
    // clicked — don't reopen the confirmation dialog for a dose that's no longer actionable.
    const confirmable = match.status === 'Pending' || match.status === 'Sent' || match.status === 'Overdue';
    if (confirmable) {
      this.openConfirm(match);
    } else {
      this.toast(
        match.status === 'Confirmed'
          ? `${match.medicineName} has already been marked as taken.`
          : `This reminder for ${match.medicineName} is no longer active.`,
        'success'
      );
    }

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
      next: res => {
        this.confirming.set(false);
        this.showConfirmModal.set(false);
        this.confirmingLog.set(null);

        // Backend responds success:false when the reminder was already confirmed/cancelled
        // elsewhere (duplicate click, stale dialog, race with another tab).  Refresh from
        // the server so the display reflects the actual persisted state.
        if (!res.success) {
          this.toast(res.message || `${log.medicineName} has already been updated.`, 'success');
          this.loadSchedule();
          return;
        }

        // Optimistic update for the whole occurrence so the UI is instantly consistent.
        //
        // The backend cancels all Pending logs for the same (medicineReminderId, scheduledDate)
        // pair except the confirmed one.  Mirror that here so no attempt chip stays "Upcoming"
        // after the dose is already taken.  A subsequent loadSchedule() from the server
        // remains the source of truth and will correct any edge-case discrepancy.
        const confirmedAt = new Date().toISOString();
        this.todayLogs.update(list =>
          list.map(l => {
            if (l.id === log.id) {
              return { ...l, status: 'Confirmed' as MedicationReminderStatus, confirmedAt, isActionable: false };
            }
            if (
              l.medicineReminderId === log.medicineReminderId &&
              l.scheduledDate === log.scheduledDate &&
              l.status === 'Pending'
            ) {
              return { ...l, status: 'Cancelled' as MedicationReminderStatus, isActionable: false };
            }
            // Sent logs retain their historical status; the occurrence-level Confirmed state
            // on the dose card supersedes the individual attempt display.
            if (
              l.medicineReminderId === log.medicineReminderId &&
              l.scheduledDate === log.scheduledDate
            ) {
              return { ...l, isActionable: false };
            }
            return l;
          })
        );

        this.reviewTracking.trackAction();
        this.toast(`${log.medicineName} marked as taken. Great job! 💊`, 'success');
        this.notifSvc.push({
          title: 'Medication Confirmed',
          body: `${log.medicineName} ${log.dosage} taken at ${this.formatTime(log.reminderTime)}`,
          type: 'reminder',
        });
        this.notifSvc.notifyReminderChanged();

        // Reload from the backend as the definitive source of truth.  This reconciles any
        // Sent logs that were not mirrored optimistically above and picks up the server-set
        // ConfirmedAt timestamp.
        this.loadSchedule();
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
    const hour = h % 12 || 12;
    return `${String(hour).padStart(2, '0')}:${String(m).padStart(2, '0')} ${period}`;
  }

  formatDate(dateStr: string): string {
    return new Date(dateStr).toLocaleDateString('en-US', {
      weekday: 'short', month: 'short', day: 'numeric',
    });
  }

  /** "08:15 AM" from a full ISO datetime string — used for "Taken at" chips. */
  formatConfirmedTime(dateStr: string): string {
    console.log('confirmedAt =', dateStr);

    return new Date(dateStr).toLocaleTimeString('en-US', {
      hour: '2-digit',
      minute: '2-digit',
    });
  }

  statusLabel(status: MedicationReminderStatus): string {
    const map: Record<MedicationReminderStatus, string> = {
      Pending: 'Upcoming',
      Sent: 'Awaiting Confirmation',
      Confirmed: 'Completed',
      Cancelled: 'Cancelled',
      Overdue: 'Overdue',
    };
    return map[status] ?? status;
  }

  statusClass(status: MedicationReminderStatus): string {
    const map: Record<MedicationReminderStatus, string> = {
      Pending: 'pill pill-yellow-sm',
      Sent: 'pill pill-blue-sm',
      Confirmed: 'pill pill-green-sm',
      Cancelled: 'pill pill-red-sm',
      Overdue: 'pill pill-red-sm',
    };
    return map[status] ?? 'pill';
  }

  statusIcon(status: MedicationReminderStatus): string {
    const map: Record<MedicationReminderStatus, string> = {
      Pending: 'fa-clock',
      Sent: 'fa-bell',
      Confirmed: 'fa-circle-check',
      Cancelled: 'fa-ban',
      Overdue: 'fa-triangle-exclamation',
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
      Manual: 'Manual',
      Prescription: 'Prescription',
      MedicineBox: 'Box Scan',
      '1': 'Manual',
      '2': 'Prescription',
      '3': 'Box Scan',
    };
    return map[source] ?? source;
  }

  sourceClass(source: string): string {
    if (source === 'Prescription' || source === '2') return 'pill pill-purple-sm';
    if (source === 'MedicineBox' || source === '3') return 'pill pill-teal-sm';
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

  /** Auto-opens the Add Reminder modal for a medicine passed via ?medicineId=, e.g. from the post-save reminder prompt. */
  private tryOpenAddReminderForMedicine(): void {
    const medicineId = this.pendingReminderMedicineId();
    if (!medicineId) return;

    const med = this.medicines().find(m => m.id === medicineId);
    if (!med) return;

    this.pendingReminderMedicineId.set(null);
    this.openAddReminder(med);

    void this.router.navigate([], {
      relativeTo: this.route,
      queryParams: { medicineId: null },
      queryParamsHandling: 'merge',
      replaceUrl: true,
    });
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
      endDate: first.endDate?.slice(0, 10) ?? today,
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
      times: f.reminderTimes.filter(t => t.trim()),
      startDate: f.startDate,
      endDate: f.repeatType === 'Once' ? f.startDate : f.endDate,
      repeatType: f.repeatType,
    };
    const medName = this.addReminderMedicineName();

    const doCreate = () => {
      this.medSvc.createReminder(medId, payload).subscribe({
        next: () => {
          this.addReminderSaving.set(false);
          this.closeAddReminder();
          this.notifSvc.notifyReminderChanged();
          this.reviewTracking.trackAction();
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
    const f = this.reminderForm;
    if (f.repeatType === 'Once') {
      this.reminderForm = { ...f, endDate: f.startDate };
    } else if (f.endDate && f.startDate && f.endDate < f.startDate) {
      // Auto-advance end date to match new start so the form stays valid
      this.reminderForm = { ...f, endDate: f.startDate };
    }
  }

  updateReminderTime(index: number, value: string): void {
    const times = [...this.reminderForm.reminderTimes];
    times[index] = value;
    // Sort filled times chronologically after each change; empty slots stay at the end
    const filled = times.filter(t => t.trim()).sort();
    const empty = times.filter(t => !t.trim());
    this.reminderForm = { ...this.reminderForm, reminderTimes: [...filled, ...empty] };
  }

  // ── My Medications — table actions ───────────────────────────────────────
  setMedSubTab(tab: MedSubTab): void {
    this.medSubTab.set(tab);
    this.medPage.set(1);
    this.mobileTabMenuOpen.set(false);
  }

  toggleMobileTabMenu(): void { this.mobileTabMenuOpen.update(v => !v); }
  closeMobileTabMenu(): void { this.mobileTabMenuOpen.set(false); }

  activeTabLabel(): string {
    const t = this.t().medications;
    const map: Record<string, string> = {
      all: t.allTab || 'All',
      'with-reminder': t.withReminderTab || 'With Reminder',
      'no-reminder': t.noReminderTab || 'No Reminder',
      paused: t.pausedTab || 'Paused',
    };
    return map[this.medSubTab()] ?? (t.allTab || 'All');
  }

  activeTabIcon(): string {
    const map: Record<string, string> = {
      all: 'fa-layer-group',
      'with-reminder': 'fa-bell',
      'no-reminder': 'fa-bell-slash',
      paused: 'fa-pause',
    };
    return map[this.medSubTab()] ?? 'fa-layer-group';
  }

  onMedSearch(q: string): void {
    this.medSearch.set(q);
    this.medPage.set(1);
  }

  onMedSourceFilter(src: string): void {
    this.medSourceFilter.set(src);
    this.medPage.set(1);
  }

  clearMedFilters(): void {
    this.medSearch.set('');
    this.medSourceFilter.set('');
    this.medPage.set(1);
  }

  toggleMedMenu(id: string): void {
    this.openMedMenuId.update(v => v === id ? null : id);
  }

  openMedView(med: UserMedicine): void {
    this.viewingMed.set(med);
    this.openMedMenuId.set(null);
  }

  /** "View Details" on the Next Dose card — reuses the same medicine-detail modal as the My Medications list. */
  viewDoseDetails(dose: Dose): void {
    const med = this.medicines().find(m => m.medicineName === dose.medicineName);
    if (med) this.openMedView(med);
  }

  closeMedView(): void { this.viewingMed.set(null); }

  setMedPage(p: number): void {
    if (p >= 1 && p <= this.medTotalPages()) this.medPage.set(p);
  }

  isLastMedRow(i: number): boolean {
    return i >= this.paginatedMedicines().length - 2;
  }

  private emptyReminderForm(): ReminderForm {
    const today = Medications.localToday();
    return {
      reminderTimes: ['08:00'],
      repeatType: 'Daily',
      startDate: today,
      endDate: today,
      notificationsEnabled: true,
      notes: '',
    };
  }

  // ── Add Medicine modal ────────────────────────────────────────────────────
  openAddMed(): void {
    this.addMedForm = this.emptyAddMedForm();
    this.addMedTouched.set(false);
    this.showAddMedModal.set(true);
  }

  closeAddMed(): void {
    if (this.addMedSaving()) return;
    this.showAddMedModal.set(false);
  }

  saveAddMed(): void {
    this.addMedTouched.set(true);
    if (this.addMedErrors.length > 0) return;

    const payload: AddUserMedicinePayload = {
      medicineName: this.addMedForm.medicineName.trim(),
      dosage: this.addMedForm.dosage.trim(),
      frequency: this.addMedForm.frequency.trim(),
      duration: this.addMedForm.duration.trim(),
      notes: this.addMedForm.notes.trim() || undefined,
      source: 1,
    };

    this.allergyChecking.set(true);
    this.medSvc.checkMedicationAllergy(payload.medicineName, this.fpProfileId() ?? undefined).subscribe({
      next: res => {
        this.allergyChecking.set(false);
        if (res.data && !res.data.isSafe) {
          this._pendingMedPayload = payload;
          this.allergyCheckResult.set(res.data);
          this.showAllergyWarning.set(true);
        } else {
          this.doSaveMedicine(payload);
        }
      },
      error: () => {
        this.allergyChecking.set(false);
        this.doSaveMedicine(payload);
      },
    });
  }

  continueAfterWarning(): void {
    const payload = this._pendingMedPayload;
    this.showAllergyWarning.set(false);
    this.allergyCheckResult.set(null);
    this._pendingMedPayload = null;
    if (payload) this.doSaveMedicine(payload);
  }

  dismissAllergyWarning(): void {
    this.showAllergyWarning.set(false);
    this.allergyCheckResult.set(null);
    this._pendingMedPayload = null;
  }

  private doSaveMedicine(payload: AddUserMedicinePayload): void {
    this.addMedSaving.set(true);
    this.medSvc.createMedicine(payload, this.fpProfileId() ?? undefined).subscribe({
      next: res => {
        this.addMedSaving.set(false);
        this.showAddMedModal.set(false);
        this.toast(`${payload.medicineName} added successfully.`, 'success');
        this.reviewTracking.trackAction();
        this.loadMedicines();
        if (res.data?.id) {
          this.setTab('medications');
        }
      },
      error: err => {
        const msg = err?.error?.errors?.[0] ?? err?.error?.message ?? 'Could not add medication.';
        this.toast(msg, 'error');
        this.addMedSaving.set(false);
      },
    });
  }

  private emptyAddMedForm(): AddMedForm {
    return { medicineName: '', dosage: '', frequency: '', duration: '', notes: '' };
  }

  get editMedErrors(): string[] {
    const f = this.editMedForm;
    const errs: string[] = [];
    if (!f.medicineName.trim()) errs.push('Medicine name is required.');
    if (!f.dosage.trim()) errs.push('Dosage is required.');
    if (!f.frequency.trim()) errs.push('Frequency is required.');
    if (!f.duration.trim()) errs.push('Duration is required.');
    return errs;
  }

  // ── Edit Medicine modal ───────────────────────────────────────────────────
  openEditMed(med: UserMedicine): void {
    this.editMedForm = {
      medicineName: med.medicineName,
      dosage: med.dosage,
      frequency: med.frequency,
      duration: med.duration,
      notes: med.notes ?? '',
    };
    this.editingMed.set(med);
    this.editMedTouched.set(false);
    this.showEditMedModal.set(true);
    this.openMedMenuId.set(null);
    if (this.viewingMed()) this.closeMedView();
  }

  closeEditMed(): void {
    if (this.editMedSaving()) return;
    this.showEditMedModal.set(false);
    this.editingMed.set(null);
  }

  saveEditMed(): void {
    this.editMedTouched.set(true);
    if (this.editMedErrors.length > 0) return;

    const med = this.editingMed();
    if (!med) return;

    const payload: UpdateUserMedicinePayload = {
      medicineName: this.editMedForm.medicineName.trim(),
      dosage: this.editMedForm.dosage.trim(),
      frequency: this.editMedForm.frequency.trim(),
      duration: this.editMedForm.duration.trim(),
      notes: this.editMedForm.notes.trim() || undefined,
    };

    this.editMedSaving.set(true);
    this.medSvc.updateMedicine(med.id, payload).subscribe({
      next: res => {
        this.editMedSaving.set(false);
        this.showEditMedModal.set(false);
        this.editingMed.set(null);
        this.toast(`${payload.medicineName} updated successfully.`, 'success');
        // Patch the medicine in the local list without a full reload
        if (res.data) {
          this.medicines.update(list =>
            list.map(m => m.id === med.id ? { ...m, ...res.data! } : m)
          );
        } else {
          this.loadMedicines();
        }
      },
      error: err => {
        const msg = err?.error?.errors?.[0] ?? err?.error?.message ?? 'Could not update medication.';
        this.toast(msg, 'error');
        this.editMedSaving.set(false);
      },
    });
  }

  // ── Delete Medicine confirm modal ─────────────────────────────────────────
  openDeleteMed(medId: string, medName: string): void {
    this.deleteMedId.set(medId);
    this.deleteMedName.set(medName);
    this.showDeleteMedModal.set(true);
    this.openMedMenuId.set(null);
    if (this.viewingMed()) this.closeMedView();
  }

  closeDeleteMed(): void {
    if (this.deletingMed()) return;
    this.showDeleteMedModal.set(false);
    this.deleteMedId.set(null);
    this.deleteMedName.set('');
  }

  confirmDeleteMed(): void {
    const medId = this.deleteMedId();
    if (!medId) return;
    const medName = this.deleteMedName();

    this.deletingMed.set(true);
    this.medSvc.deleteMedicine(medId).subscribe({
      next: () => {
        this.deletingMed.set(false);
        this.showDeleteMedModal.set(false);
        this.deleteMedId.set(null);
        this.deleteMedName.set('');
        this.toast(`${medName} has been removed from your medication list.`, 'success');
        // Remove from local list and reload schedule (reminders may have been cancelled)
        this.medicines.update(list => list.filter(m => m.id !== medId));
        this.medicineReminders.update(rec => {
          const updated = { ...rec };
          delete updated[medId];
          return updated;
        });
        this.loadSchedule();
      },
      error: err => {
        this.toast(err?.error?.message ?? 'Could not delete medication.', 'error');
        this.deletingMed.set(false);
      },
    });
  }
}
