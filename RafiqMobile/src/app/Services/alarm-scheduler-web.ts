import { WebPlugin } from '@capacitor/core';
import type { AlarmSchedulerPluginInterface, AlarmScheduleRequest, AlarmScheduleResult, AlarmCancelResult, AlarmDismissResult, PendingAlarmActionResult, PendingAlarmActionCompleteResult } from './alarm-scheduler-plugin';

/**
 * No-op web/iOS fallback for AlarmSchedulerPlugin.
 * On Android the native plugin handles everything.
 * On web/iOS this resolves successfully without scheduling anything —
 * Capacitor LocalNotifications covers iOS scheduling.
 */
export class AlarmSchedulerWeb extends WebPlugin implements AlarmSchedulerPluginInterface {
  async scheduleAlarm(options: AlarmScheduleRequest): Promise<AlarmScheduleResult> {
    console.debug('[AlarmSchedulerWeb] scheduleAlarm noop', options);
    return { scheduled: false, skipped: true, reason: 'Web platform — no native alarm' };
  }

  async cancelAlarm(options: { reminderId: string }): Promise<AlarmCancelResult> {
    console.debug('[AlarmSchedulerWeb] cancelAlarm noop', options);
    return { cancelled: false, reminderId: options.reminderId };
  }

  async dismissAlarm(options: { reminderId: string }): Promise<AlarmDismissResult> {
    console.debug('[AlarmSchedulerWeb] dismissAlarm noop', options);
    return { dismissed: false };
  }

  async consumePendingAction(): Promise<PendingAlarmActionResult> {
    return { hasAction: false };
  }

  async completePendingAction(): Promise<PendingAlarmActionCompleteResult> {
    return { completed: true };
  }
}
