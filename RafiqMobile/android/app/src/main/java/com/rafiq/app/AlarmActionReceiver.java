package com.rafiq.mobile;

import android.app.NotificationManager;
import android.app.PendingIntent;
import android.content.BroadcastReceiver;
import android.content.Context;
import android.content.Intent;
import android.os.Build;
import android.util.Log;

/**
 * Handles "Take Medicine" / "I Attended" / "Snooze" taps from BOTH the outer Android
 * system notification and the full-screen AlarmActivity — entirely natively, with NO
 * Activity/app launch. AlarmActivity's buttons call the same static methods directly
 * (same process) instead of duplicating this logic.
 *
 * Design (see class-level rationale in the audit that introduced this file):
 *  - Silencing the alarm (stop sound/vibration, dismiss notification) is unconditional
 *    and instant — no network required.
 *  - Cancelling the remaining native AlarmManager occurrences for this reminder is ALSO
 *    unconditional and instant (AlarmReceiver.cancelAlarm is a pure local operation) —
 *    so no further stage can ever fire once the user acts, regardless of whether the
 *    backend is reachable right now.
 *  - The backend confirmation (medication confirm / appointment complete) is NOT made
 *    natively here. Doing so would require re-implementing token read + HTTP + JSON
 *    parsing in Java with no way to compile/verify it in this environment — too risky
 *    for a health-reminder app's auth path. Instead we reuse the EXISTING, already-safe
 *    mechanism: the action is recorded to SharedPreferences (same store
 *    AlarmSchedulerPlugin.recordAlarmAction already writes) and is reconciled with the
 *    backend the next time the app is naturally opened by the user
 *    (AlarmSchedulerService.consumePendingNativeAction, called from app.ts on every
 *    startup — unchanged). This mirrors exactly what already happened when the user
 *    tapped these buttons before this change; the only difference is we no longer force
 *    the app open to do it.
 */
public class AlarmActionReceiver extends BroadcastReceiver {

    public static final String ACTION_TAKE   = "com.rafiq.mobile.ACTION_TAKE";
    public static final String ACTION_SNOOZE = "com.rafiq.mobile.ACTION_SNOOZE";

    private static final long SNOOZE_DELAY_MILLIS = 10 * 60 * 1000L;
    private static final int SNOOZE_MINUTES = 10;

    @Override
    public void onReceive(Context context, Intent intent) {
        String action = intent.getAction();
        String reminderId   = intent.getStringExtra(AlarmActivity.EXTRA_REMINDER_ID);
        String reminderType = intent.getStringExtra(AlarmActivity.EXTRA_REMINDER_TYPE);
        String title         = intent.getStringExtra(AlarmActivity.EXTRA_TITLE);
        String body           = intent.getStringExtra(AlarmActivity.EXTRA_BODY);
        String scheduledAt   = intent.getStringExtra(AlarmActivity.EXTRA_SCHEDULED_AT);

        if (ACTION_TAKE.equals(action)) {
            handleTake(context, reminderId, reminderType);
        } else if (ACTION_SNOOZE.equals(action)) {
            handleSnooze(context, reminderId, reminderType, title, body, scheduledAt);
        }
    }

    /** Stops sound/vibration and dismisses the alarm/notification. Always safe to call. */
    static void stopAlarmAndNotification(Context context, String reminderId) {
        context.stopService(new Intent(context, AlarmService.class));
        NotificationManager nm = (NotificationManager) context.getSystemService(Context.NOTIFICATION_SERVICE);
        if (nm != null && reminderId != null) {
            nm.cancel(AlarmReceiver.stableId(reminderId));
        }
    }

    /**
     * "Take Medicine" / "I Attended". Idempotent: if the reminder was already cancelled
     * (e.g. a duplicate tap, or the notification/action arriving twice), cancelAlarm and
     * the pending-action write are both safe no-ops the second time.
     */
    static void handleTake(Context context, String reminderId, String reminderType) {
        if (reminderId == null) return;
        Log.i(AlarmDiagnostics.TAG, "AlarmActionReceiver TAKE reminderId=" + reminderId + " type=" + reminderType);

        stopAlarmAndNotification(context, reminderId);
        // Cancels EVERY remaining occurrence for this reminder right now — no waiting on
        // the backend. Safe/idempotent: cancelling an already-cancelled alarm is a no-op.
        AlarmReceiver.cancelAlarm(context, reminderId);
        recordPendingAction(context, AlarmActivity.ACTION_TAKE_MEDICINE, reminderId, reminderType, 0);
    }

    /** "Snooze 10 min" — native reschedule, mirrors what AlarmActivity.snooze() already did. */
    static void handleSnooze(Context context, String reminderId, String reminderType,
                              String title, String body, String firedScheduledAt) {
        if (reminderId == null) return;
        Log.i(AlarmDiagnostics.TAG, "AlarmActionReceiver SNOOZE reminderId=" + reminderId);

        stopAlarmAndNotification(context, reminderId);

        long triggerAtMillis = System.currentTimeMillis() + SNOOZE_DELAY_MILLIS;
        String newScheduledAt = java.time.Instant.ofEpochMilli(triggerAtMillis).toString();

        // The snoozed alarm is a NEW occurrence (its scheduledAt changed) — replace the
        // fired occurrence's stored entry with the new one so reboot restores the snooze.
        NativeReminderStore.removeAlarm(context, AlarmDiagnostics.occurrenceKey(reminderId, firedScheduledAt));
        NativeReminderStore.saveAlarm(context,
            AlarmDiagnostics.occurrenceKey(reminderId, newScheduledAt),
            reminderId, reminderType, title, body, newScheduledAt, triggerAtMillis);

        AlarmReceiver.scheduleAlarm(context, reminderId, reminderType, title, body, newScheduledAt, triggerAtMillis);

        recordPendingAction(context, AlarmActivity.ACTION_SNOOZE, reminderId, reminderType, SNOOZE_MINUTES);
    }

    private static void recordPendingAction(Context context, String action, String reminderId,
                                             String reminderType, int snoozeMinutes) {
        Intent fake = new Intent();
        fake.putExtra(AlarmActivity.EXTRA_ALARM_ACTION, action);
        fake.putExtra(AlarmActivity.EXTRA_REMINDER_ID, reminderId);
        fake.putExtra(AlarmActivity.EXTRA_REMINDER_TYPE, reminderType);
        if (snoozeMinutes > 0) {
            fake.putExtra("snoozeMinutes", snoozeMinutes);
        }
        AlarmSchedulerPlugin.recordAlarmAction(context, fake);
    }

    // ── Shared PendingIntent builders — used by both AlarmReceiver's heads-up notification
    // and AlarmService's foreground-service notification, so the two notifications (which
    // occupy the SAME notification id after the dedup fix) expose identical actions. ─────

    static PendingIntent buildTakePendingIntent(Context context, String reminderId, String reminderType) {
        Intent intent = new Intent(context, AlarmActionReceiver.class);
        intent.setAction(ACTION_TAKE);
        intent.putExtra(AlarmActivity.EXTRA_REMINDER_ID, reminderId);
        intent.putExtra(AlarmActivity.EXTRA_REMINDER_TYPE, reminderType);

        int flags = Build.VERSION.SDK_INT >= Build.VERSION_CODES.M
            ? PendingIntent.FLAG_IMMUTABLE | PendingIntent.FLAG_UPDATE_CURRENT
            : PendingIntent.FLAG_UPDATE_CURRENT;
        int reqCode = AlarmDiagnostics.stableId(AlarmDiagnostics.occurrenceKey(reminderId, "take"));
        return PendingIntent.getBroadcast(context, reqCode, intent, flags);
    }

    static PendingIntent buildSnoozePendingIntent(Context context, String reminderId, String reminderType,
                                                   String title, String body, String scheduledAt) {
        Intent intent = new Intent(context, AlarmActionReceiver.class);
        intent.setAction(ACTION_SNOOZE);
        intent.putExtra(AlarmActivity.EXTRA_REMINDER_ID, reminderId);
        intent.putExtra(AlarmActivity.EXTRA_REMINDER_TYPE, reminderType);
        intent.putExtra(AlarmActivity.EXTRA_TITLE, title);
        intent.putExtra(AlarmActivity.EXTRA_BODY, body);
        intent.putExtra(AlarmActivity.EXTRA_SCHEDULED_AT, scheduledAt);

        int flags = Build.VERSION.SDK_INT >= Build.VERSION_CODES.M
            ? PendingIntent.FLAG_IMMUTABLE | PendingIntent.FLAG_UPDATE_CURRENT
            : PendingIntent.FLAG_UPDATE_CURRENT;
        int reqCode = AlarmDiagnostics.stableId(AlarmDiagnostics.occurrenceKey(reminderId, "snooze"));
        return PendingIntent.getBroadcast(context, reqCode, intent, flags);
    }
}
