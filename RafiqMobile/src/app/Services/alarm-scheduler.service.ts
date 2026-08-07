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
  private actionInFlight: Promise<void> | null = null;

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
    } catch (e) {
      console.error('[AlarmSchedulerService] scheduleAlarm error', e);
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

    const futures = reminders
      .filter(r => !r.isDeleted)
      .map(r => this.scheduleAlarm(r));

    await Promise.allSettled(futures);
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

  async consumePendingNativeAction(): Promise<void> {
    if (!this.isAndroid) return;
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

  private async _consumePendingNativeAction(): Promise<void> {
    const action = await AlarmSchedulerPlugin.consumePendingAction();
    if (!action.hasAction || !action.reminderId) return;

    if (action.action === 'takeMedicine' && action.reminderType !== 'Appointment') {
      await firstValueFrom(this.medicationReminders.confirm(action.reminderId));
      await this.offlineReminders.removeReminder(action.reminderId);
      await this.cancelAlarm(action.reminderId);
      await AlarmSchedulerPlugin.completePendingAction({
        reminderId: action.reminderId,
        action: 'takeMedicine',
      });
      return;
    }

    if (action.action === 'snooze' && action.reminderType !== 'Appointment') {
      await firstValueFrom(this.medicationReminders.snooze(action.reminderId, action.snoozeMinutes || 10));
      await AlarmSchedulerPlugin.completePendingAction({
        reminderId: action.reminderId,
        action: 'snooze',
      });
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
      return;
    }

    if (action.action === 'snooze' && action.reminderType === 'Appointment') {
      // AlarmActivity.snooze() already re-scheduled the native alarm and
      // updated the SQLite reminderTime via NativeReminderStore.updateSnooze.
      // Just clear the SharedPreferences pending action.
      await AlarmSchedulerPlugin.completePendingAction({
        reminderId: action.reminderId,
        action: 'snooze',
      });
      return;
    }
  }
}
