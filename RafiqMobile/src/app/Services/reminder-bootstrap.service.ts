import { Injectable, inject } from '@angular/core';
import { Network } from '@capacitor/network';
import { firstValueFrom } from 'rxjs';
import { MedicationRemindersService, UpcomingReminderDto } from './medication-reminders.service';
import { AppointmentsService } from './appointments.service';
import { OfflineReminderService } from './offline-reminder.service';
import { AuthService } from './auth-service';
import { TokenStorageService } from './token-storage-service';
import { ProfileSelectionService } from './profile-selection.service';
import { HealthProfileService } from './health-profile.service';
import { AlarmSchedulerService } from './alarm-scheduler.service';

/**
 * Orchestrates the offline reminder bootstrap sequence on every app start.
 *
 * Safeguards implemented:
 *  1. Idempotent — calling bootstrap() multiple times is a no-op after the
 *     first successful run. SQLite is never reopened, no duplicate sync.
 *  2. Concurrency-safe — concurrent bootstrap() calls share the same in-flight
 *     Promise; only one sync ever runs at a time (enforced here and in
 *     OfflineReminderService.syncFromServer).
 *  3. Auth-guarded — bootstrap() returns immediately if:
 *       • the user is not authenticated (AuthService.isLoggedIn = false), or
 *       • no active profile ID can be resolved.
 *     No backend calls, no SQLite writes, no notifications are triggered.
 *  4. Non-invasive — does not modify AuthService, ProfileSelectionService,
 *     or any existing login flow.
 *
 * Usage:
 *   Inject into App component and call bootstrap() from ngOnInit.
 *   Re-call after login to trigger the first sync for a newly authenticated user.
 */
@Injectable({ providedIn: 'root' })
export class ReminderBootstrapService {
  private readonly medicationSvc   = inject(MedicationRemindersService);
  private readonly appointmentSvc  = inject(AppointmentsService);
  private readonly offlineSvc      = inject(OfflineReminderService);
  private readonly authSvc         = inject(AuthService);
  private readonly tokenStorage    = inject(TokenStorageService);
  private readonly profileSvc      = inject(ProfileSelectionService);
  private readonly healthProfileSvc = inject(HealthProfileService);
  private readonly alarmScheduler  = inject(AlarmSchedulerService);

  /**
   * Whether bootstrap() has successfully completed at least once this session.
   * Prevents re-running restore + sync on every route change or lifecycle call.
   */
  private bootstrapDone = false;

  /**
   * Tracks the last observed auth identity so we only react to real transitions
   * (login / logout / account switch) rather than every BehaviorSubject replay.
   */
  private lastAuthUserId: string | null = null;

  constructor() {
    // Deterministic login/logout re-entry. On a cold start with a valid stored
    // token, app.ts also calls bootstrap() (idempotent). This subscription covers
    // the "logged in AFTER app startup" and "logged out" transitions.
    this.authSvc.currentUser$.subscribe(user => {
      const nextId = user?.userId ?? null;
      if (nextId === this.lastAuthUserId) return;
      const previous = this.lastAuthUserId;
      this.lastAuthUserId = nextId;

      if (nextId) {
        // New login (or account switch) → allow a fresh bootstrap run.
        if (previous && previous !== nextId) this.reset();
        void this.bootstrap();
      } else {
        // Logout → allow the next login to re-run bootstrap.
        this.reset();
      }
    });
  }

  /**
   * In-flight bootstrap Promise. Concurrent callers share this instead of
   * spawning a second bootstrap run.
   */
  private bootstrapInFlight: Promise<void> | null = null;

  // ── Public API ────────────────────────────────────────────────────────────

  /**
   * Idempotent entry point. Safe to call from ngOnInit, after login, or
   * after a network reconnect event.
   *
   * - First call: runs the full sequence.
   * - Subsequent calls while first is in flight: await the same Promise.
   * - Subsequent calls after success: return immediately (no-op).
   */
  async bootstrap(): Promise<void> {
    // Idempotency: already completed successfully this session.
    if (this.bootstrapDone) return;

    // Concurrency: already in flight — share the existing Promise.
    if (this.bootstrapInFlight) return this.bootstrapInFlight;

    this.bootstrapInFlight = this._run().finally(() => {
      this.bootstrapInFlight = null;
    });
    return this.bootstrapInFlight;
  }

  /**
   * Force an immediate sync and alarm schedule.
   * Useful when a new reminder is created online and needs to be scheduled natively.
   */
  async forceSync(): Promise<void> {
    // Deterministic: never evaluate auth before token hydration has settled.
    await this.tokenStorage.initialize();
    if (!(await this.tokenStorage.isLoggedInAsync())) return;
    const profileId = await this.resolveProfileId();
    if (!profileId) return;

    await this.syncFromServer(profileId);
  }

  /**
   * Call after logout to allow the next login to re-run bootstrap.
   * Does not touch SQLite — OfflineReminderService.syncFromServer will diff
   * on the next bootstrap and remove any stale reminders.
   */
  reset(): void {
    this.bootstrapDone = false;
  }

  // ── Private ───────────────────────────────────────────────────────────────

  private async _run(): Promise<void> {
    // ── Safeguard 0: Wait for token hydration (fixes cold-start auth race) ──
    // TokenStorageService hydrates its in-memory cache asynchronously from
    // SecureStorage. Evaluating isLoggedIn before that settles can wrongly
    // report "not authenticated" on a cold start and permanently skip native
    // alarm scheduling. Awaiting the existing initialization mechanism makes
    // the auth check deterministic — no arbitrary delays.
    await this.tokenStorage.initialize();

    // ── Safeguard 3: Auth check ─────────────────────────────────────────
    if (!(await this.tokenStorage.isLoggedInAsync())) {
      return; // Not authenticated; do nothing.
    }

    // Resolve the active profile ID from storage first (fast path),
    // then fall back to a network call if not yet stored.
    const profileId = await this.resolveProfileId();
    if (!profileId) {
      return; // No profile — cannot sync reminder data.
    }

    // A cache or sync failure must not silently swallow the whole bootstrap, and
    // must never let us falsely mark success. We track health and only mark the
    // bootstrap done when scheduling actually ran, so a later trigger (login,
    // forceSync, SignalR change) can retry instead of being permanently
    // short-circuited by the idempotency guard.
    let healthy = true;

    // ── Step 1: Restore cached alarms (no network required) ────────────
    // Isolated so a cache failure cannot block the authoritative server sync.
    try {
      await this.offlineSvc.restoreScheduledReminders();
      const cached = await this.offlineSvc.getCachedReminders();
      console.info('[RafiqAlarm] bootstrap: restoring', cached.length, 'cached occurrence(s)');
      await this.alarmScheduler.scheduleCachedBatch(cached);
    } catch (e) {
      healthy = false;
      // Real (e.g. native Android SQLite) failure — surface it, do not hide it.
      console.error('[RafiqAlarm] bootstrap: cache restore failed — cached native alarms NOT scheduled', e);
    }

    // ── Step 2: Sync from backend when online — schedules fresh native alarms ──
    const network = await Network.getStatus();
    console.info('[RafiqAlarm] bootstrap: network.connected =', network.connected);
    if (network.connected) {
      try {
        await this.syncFromServer(profileId);
      } catch (e) {
        healthy = false;
        console.error('[RafiqAlarm] bootstrap: server sync failed — native alarms NOT scheduled', e);
      }
    }

    // Only mark done when nothing failed. Never claim success (and never block
    // future retries) if scheduling did not actually complete.
    if (healthy) {
      this.bootstrapDone = true;
    }
  }

  private async syncFromServer(profileId: string): Promise<void> {
    // Fetch both domains independently. A failure in one must not block the other.
    const [medResult, apptResult] = await Promise.allSettled([
      this.medicationSvc.getUpcomingReminders(profileId),
      this.appointmentSvc.getUpcomingForSync(profileId),
    ]);

    const serverItems: UpcomingReminderDto[] = [];

    if (medResult.status === 'fulfilled') {
      serverItems.push(...medResult.value);
    } else {
      console.error('[ReminderBootstrapService] Medication reminders fetch failed:', medResult.reason);
    }

    if (apptResult.status === 'fulfilled') {
      serverItems.push(...apptResult.value);
    } else {
      console.error('[ReminderBootstrapService] Appointment reminders fetch failed:', apptResult.reason);
    }

    const cachedIds = await this.offlineSvc.getCachedReminderIds();
    const serverActiveIds = new Set(serverItems.filter(r => !r.isDeleted).map(r => r.reminderId));
    const nativeCancelIds = [
      ...serverItems.filter(r => r.isDeleted).map(r => r.reminderId),
      ...cachedIds.filter(id => !serverActiveIds.has(id)),
    ];

    await this.alarmScheduler.cancelBatch([...new Set(nativeCancelIds)]);

    // OfflineReminderService.syncFromServer is also concurrency-safe.
    await this.offlineSvc.syncFromServer(serverItems);

    // Schedule native Android alarms for all non-deleted upcoming reminders.
    // This runs after the diff-sync so the SQLite cache is authoritative.
    // No-op on non-Android platforms.
    await this.alarmScheduler.scheduleBatch(serverItems.filter(r => !r.isDeleted));
  }

  /**
   * Resolves the active profile ID without modifying any existing service.
   * Strategy:
   *   1. Check ProfileSelectionService (localStorage — instant, no network).
   *   2. Fetch from backend via HealthProfileService (one HTTP call).
   *   3. Return null if neither succeeds.
   */
  private async resolveProfileId(): Promise<string | null> {
    // Fast path: already stored from a previous session or profile switch.
    const stored = this.profileSvc.selectedProfileId;
    if (stored) return stored;

    // Slow path: fetch from backend.
    try {
      const response = await firstValueFrom(this.healthProfileSvc.getMyProfile());
      const id = response?.data?.id ?? null;
      if (id) {
        // Persist so subsequent fast-path hits work.
        this.profileSvc.select(id);
      }
      return id;
    } catch {
      // Network error or unauthenticated — do not throw.
      return null;
    }
  }
}
