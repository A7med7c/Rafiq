import { registerPlugin } from '@capacitor/core';

/**
 * Schedule request sent to the native Android layer.
 * scheduledAt must be an ISO-8601 string with UTC offset (e.g. "2026-08-07T10:30:00Z").
 */
export interface AlarmScheduleRequest {
  reminderId: string;
  reminderType: 'Medication' | 'Appointment';
  title: string;
  body: string;
  scheduledAt: string; // ISO-8601
}

export interface AlarmScheduleResult {
  scheduled?: boolean;
  skipped?: boolean;
  reason?: string;
  reminderId?: string;
}

export interface AlarmCancelResult {
  cancelled: boolean;
  reminderId: string;
}

export interface AlarmDismissResult {
  dismissed: boolean;
}

export interface PendingAlarmActionResult {
  hasAction: boolean;
  action?: 'takeMedicine' | 'snooze' | 'dismiss';
  reminderId?: string;
  reminderType?: 'Medication' | 'Appointment';
  snoozeMinutes?: number;
}

export interface PendingAlarmActionCompleteResult {
  completed: boolean;
}

export interface BatteryOptimizationResult {
  isIgnoring: boolean;
}

export interface BatteryOptimizationExemptionResult {
  requested: boolean;
}

/**
 * Capacitor plugin interface for native Android alarm scheduling.
 * On non-Android platforms (iOS, web) all methods resolve successfully
 * but perform no native action — Capacitor LocalNotifications handle
 * those platforms.
 */
export interface AlarmSchedulerPluginInterface {
  scheduleAlarm(options: AlarmScheduleRequest): Promise<AlarmScheduleResult>;
  cancelAlarm(options: { reminderId: string }): Promise<AlarmCancelResult>;
  dismissAlarm(options: { reminderId: string }): Promise<AlarmDismissResult>;
  consumePendingAction(): Promise<PendingAlarmActionResult>;
  completePendingAction(options: { reminderId: string; action: 'takeMedicine' | 'snooze' | 'dismiss' }): Promise<PendingAlarmActionCompleteResult>;
  /** Returns whether the app is currently excluded from battery optimizations (Android 6+). */
  checkBatteryOptimization(): Promise<BatteryOptimizationResult>;
  /** Opens the system dialog to request battery optimization exemption (Android 6+). */
  requestBatteryOptimizationExemption(): Promise<BatteryOptimizationExemptionResult>;
}

const AlarmSchedulerPlugin = registerPlugin<AlarmSchedulerPluginInterface>(
  'AlarmScheduler',
  {
    // Web / iOS fallback: no-op implementations so Angular code compiles
    // and runs on all platforms without platform checks everywhere.
    web: () => import('./alarm-scheduler-web').then(m => new m.AlarmSchedulerWeb()),
  }
);

export { AlarmSchedulerPlugin };
