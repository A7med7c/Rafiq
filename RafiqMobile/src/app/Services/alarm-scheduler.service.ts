import { Injectable, inject } from '@angular/core';
import { Capacitor } from '@capacitor/core';
import { firstValueFrom } from 'rxjs';
import { AlarmSchedulerPlugin } from './alarm-scheduler-plugin';
import type { AlarmScheduleRequest } from './alarm-scheduler-plugin';
import { MedicationRemindersService, UpcomingReminderDto } from './medication-reminders.service';
import { CachedReminder, OfflineReminderService } from './offline-reminder.service';
import { AppointmentsService } from './appointments.service';

/**
 * Thin wrapper around AlarmSchedulerPlugin that schedules / cancels native
 * Android AlarmManager alarms for every reminder in the SQLite cache.
 *
 * Design rules:
 *  - Only active on Android. On other platforms it is a no-op.
 *  - Does NOT duplicate OfflineReminderService's sync logic.
 *  - Called by ReminderBootstrapService after every successful sync so that
 *    AlarmManager entries mirror the SQLite cache.
 *  - The existing Capacitor LocalNotifications path is intentionally left
 *    intact; this service adds native exact-alarm scheduling on top.
 */
@Injectable({ providedIn: 'root' })
export class AlarmSchedulerService {

  private readonly isAndroid = Capacitor.getPlatform() === 'android';
  private readonly medicationReminders = inject(MedicationRemindersService);
  private readonly appointmentsSvc    = inject(AppointmentsService);
  private readonly offlineReminders = inject(OfflineReminderService);
  private actionInFlight: Promise<boolean> | null = null;

  /**
   * Schedules (or reschedules) a native Android alarm for a single reminder.
   * Safe to call for past-due reminders — the native plugin will skip them.
   */
  async scheduleAlarm(reminder: UpcomingReminderDto): Promise<void> {
    if (!this.isAndroid) return;
    if (reminder.isDeleted) return;

    const request: AlarmScheduleRequest = {
      reminderId:   reminder.reminderId,
      reminderType: reminder.reminderType,
      title:        reminder.title,
      body:         reminder.body,
      scheduledAt:  reminder.scheduledAt,
    };

    try {
      await AlarmSchedulerPlugin.scheduleAlarm(request);
      console.info('[RafiqAlarm] scheduled native alarm', {
        reminderId: reminder.reminderId,
        reminderType: reminder.reminderType,
        scheduledAt: reminder.scheduledAt,
      });
    } catch (e) {
      // Do not swallow — include the exact reminder + scheduledAt that failed.
      console.error('[RafiqAlarm] scheduleAlarm FAILED', {
        reminderId: reminder.reminderId,
        reminderType: reminder.reminderType,
        scheduledAt: reminder.scheduledAt,
        error: e,
      });
    }
  }

  /**
   * Cancels a native Android alarm for a reminder.
   * Called when a reminder is deleted, confirmed, skipped, or snoozed.
   */
  async cancelAlarm(reminderId: string): Promise<void> {
    if (!this.isAndroid) return;
    try {
      await AlarmSchedulerPlugin.cancelAlarm({ reminderId });
    } catch (e) {
      console.error('[AlarmSchedulerService] cancelAlarm error', e);
    }
  }

  /**
   * Stops the currently playing alarm (sound + vibration) and cancels its notification.
   * Called from the alarm screen when the user taps Take Medicine, Snooze, or Dismiss.
   */
  async dismissAlarm(reminderId: string): Promise<void> {
    if (!this.isAndroid) return;
    try {
      await AlarmSchedulerPlugin.dismissAlarm({ reminderId });
    } catch (e) {
      console.error('[AlarmSchedulerService] dismissAlarm error', e);
    }
  }

  /**
   * Schedules native alarms for all items in a server snapshot.
   * Called by ReminderBootstrapService after syncFromServer() completes.
   * Runs all schedules concurrently; individual failures are logged but do
   * not abort the batch.
   */
  async scheduleBatch(reminders: UpcomingReminderDto[]): Promise<void> {
    if (!this.isAndroid) return;

    const active = reminders.filter(r => !r.isDeleted);
    console.info('[RafiqAlarm] scheduleBatch scheduling', active.length, 'native alarm(s)');
    await Promise.allSettled(active.map(r => this.scheduleAlarm(r)));
  }

  async scheduleCachedBatch(reminders: CachedReminder[]): Promise<void> {
    if (!this.isAndroid) return;

    const now = Date.now();
    const futures = reminders
      .filter(r => r.status === 'scheduled')
      .filter(r => new Date(r.reminderTime).getTime() > now)
      .map(r => AlarmSchedulerPlugin.scheduleAlarm({
        reminderId: r.serverId,
        reminderType: r.type === 'appointment' ? 'Appointment' : 'Medication',
        title: r.title,
        body: r.body,
        scheduledAt: r.reminderTime,
      }));

    await Promise.allSettled(futures);
  }

  /**
   * Cancels native alarms for a batch of reminder IDs.
   * Called during the diff-sync cleanup step.
   */
  async cancelBatch(reminderIds: string[]): Promise<void> {
    if (!this.isAndroid) return;

    const futures = reminderIds.map(id => this.cancelAlarm(id));
    await Promise.allSettled(futures);
  }

  /**
   * Resolves `true` when a pending native alarm action (Take Medicine / Confirm
   * Attendance / Snooze) was actually found and processed, `false` when there was
   * nothing to do. Callers (e.g. app.ts's appStateChange listener) use this to decide
   * whether a UI refresh is warranted, instead of refreshing unconditionally on every
   * app-foreground event.
   */
  async consumePendingNativeAction(): Promise<boolean> {
    if (!this.isAndroid) return false;
    if (this.actionInFlight) return this.actionInFlight;

    this.actionInFlight = this._consumePendingNativeAction().finally(() => {
      this.actionInFlight = null;
    });

    return this.actionInFlight;
  }

  stableId(reminderId: string): number {
    let hash = 0;
    for (let i = 0; i < reminderId.length; i++) {
      hash = Math.imul(31, hash) + reminderId.charCodeAt(i);
      hash |= 0;
    }

    const id = hash & 0x7fffffff;
    return id === 0 ? 1 : id;
  }

  private async _consumePendingNativeAction(): Promise<boolean> {
    const action = await AlarmSchedulerPlugin.consumePendingAction();
    if (!action.hasAction || !action.reminderId) return false;

    if (action.action === 'takeMedicine' && action.reminderType !== 'Appointment') {
      // action.reminderId is the MEDICATION CONFIG id (MedicineReminder.Id), shared by all
      // 3 escalation-stage logs for today — it is NOT a MedicationReminderLog id. The backend
      // confirm endpoint requires the per-stage LOG id (POST /medication-reminders/{logId}/confirm
      // → ConfirmMedicationReminderCommand(ReminderLogId)). Resolve the correct actionable log
      // via the existing /today endpoint before confirming; do not assume the alarm's own id
      // is usable directly.
      try {
        const todayLogs = await firstValueFrom(this.medicationReminders.getToday());
        const actionableLog = todayLogs.find(
          l => l.medicineReminderId === action.reminderId && l.isActionable
        );

        if (actionableLog) {
          await firstValueFrom(this.medicationReminders.confirm(actionableLog.id));
        } else {
          console.warn('[RafiqAlarm] Take Medicine: no actionable log found for reminderId='
            + action.reminderId + ' — dose likely already resolved elsewhere');
        }

        // Confirmed (or already resolved by another path) — cancel every remaining native
        // alarm belonging to this SAME dose (cancelAlarm cancels all occurrences that share
        // this config-level reminderId; today's sync only ever schedules TODAY's stages, so
        // this cannot reach a different day's occurrences or a different medication).
        await this.offlineReminders.removeReminder(action.reminderId);
        await this.cancelAlarm(action.reminderId);
      } catch (e) {
        // Do not cancel remaining stage alarms on a genuine failure — the dose was not
        // actually confirmed server-side, so the escalation schedule must continue exactly
        // as before. Still log loudly so this is visible instead of silently swallowed.
        console.error('[RafiqAlarm] Take Medicine confirm FAILED reminderId=' + action.reminderId, e);
      }

      // Always clear the pending action, success or failure, so a transient error does not
      // leave the same stale action re-processed on every future app open.
      await AlarmSchedulerPlugin.completePendingAction({
        reminderId: action.reminderId,
        action: 'takeMedicine',
      });
      return true;
    }

    if (action.action === 'snooze' && action.reminderType !== 'Appointment') {
      await firstValueFrom(this.medicationReminders.snooze(action.reminderId, action.snoozeMinutes || 10));
      await AlarmSchedulerPlugin.completePendingAction({
        reminderId: action.reminderId,
        action: 'snooze',
      });
      return true;
    }

    // ── Appointment alarm actions ─────────────────────────────────────────────
    // AlarmActivity records the action to SharedPreferences via MainActivity.
    // Without these handlers completePendingAction() was never called for
    // appointments, leaving SharedPreferences permanently dirty.

    if (action.action === 'takeMedicine' && action.reminderType === 'Appointment') {
      // "Confirm Attendance" tapped — mark appointment complete on the backend,
      // remove from the offline SQLite cache, and cancel the AlarmManager entry.
      try {
        await firstValueFrom(this.appointmentsSvc.complete(action.reminderId));
      } catch (e) {
        console.error('[AlarmSchedulerService] Failed to complete appointment via alarm action', e);
      }
      await this.offlineReminders.removeReminder(action.reminderId);
      await this.cancelAlarm(action.reminderId);
      await AlarmSchedulerPlugin.completePendingAction({
        reminderId: action.reminderId,
        action: 'takeMedicine',
      });
      return true;
    }

    if (action.action === 'snooze' && action.reminderType === 'Appointment') {
      // AlarmActivity.snooze() already re-scheduled the native alarm as a new
      // occurrence and persisted it via NativeReminderStore.saveAlarm.
      // Just clear the SharedPreferences pending action.
      await AlarmSchedulerPlugin.completePendingAction({
        reminderId: action.reminderId,
        action: 'snooze',
      });
      return true;
    }

    return false;
  }

  /**
   * Checks whether the app is excluded from battery optimizations and, if not,
   * opens the system dialog asking the user to grant the exemption.
   *
   * Must be called from a user-interaction context (button tap, first-launch
   * prompt) — Android will reject the dialog if launched without prior user
   * engagement. Safe to call on non-Android platforms (returns immediately).
   *
   * Returns true when the app was already exempt or the dialog was launched,
   * false when the check/request was skipped (non-Android, or API < 23).
   */
  async checkAndRequestBatteryOptimization(): Promise<boolean> {
    if (!this.isAndroid) return false;
    try {
      const { isIgnoring } = await AlarmSchedulerPlugin.checkBatteryOptimization();
      if (isIgnoring) return true; // already exempt — nothing to do
      await AlarmSchedulerPlugin.requestBatteryOptimizationExemption();
      return true;
    } catch (e) {
      console.warn('[AlarmSchedulerService] battery optimization check/request failed', e);
      return false;
    }
  }

  /**
   * Returns true when the app is currently excluded from battery optimizations.
   * Used by the UI to decide whether to show a persistent warning banner.
   */
  async isBatteryOptimizationIgnored(): Promise<boolean> {
    if (!this.isAndroid) return true;
    try {
      const { isIgnoring } = await AlarmSchedulerPlugin.checkBatteryOptimization();
      return isIgnoring;
    } catch {
      return true; // assume OK on error — don't block UI
    }
  }
}
