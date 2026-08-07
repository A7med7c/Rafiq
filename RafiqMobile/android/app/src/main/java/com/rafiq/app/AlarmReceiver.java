package com.rafiq.mobile;

import android.app.AlarmManager;
import android.app.Notification;
import android.app.NotificationChannel;
import android.app.NotificationManager;
import android.app.PendingIntent;
import android.content.BroadcastReceiver;
import android.content.Context;
import android.content.Intent;
import android.os.Build;

import androidx.core.app.NotificationCompat;

/**
 * BroadcastReceiver fired by AlarmManager at the exact scheduled reminder time.
 *
 * Responsibilities:
 *  1. Start AlarmService (sound + vibration) as a foreground service.
 *  2. Launch AlarmActivity via a full-screen intent so it appears over the lock screen.
 *  3. Post a high-priority heads-up notification as fallback when the device is active
 *     (full-screen intents are suppressed when the screen is on and the app is not foreground).
 *
 * This receiver handles:
 *   - action = ACTION_FIRE_ALARM  → play alarm for the given reminder
 */
public class AlarmReceiver extends BroadcastReceiver {

    public static final String ACTION_FIRE_ALARM = "com.rafiq.mobile.FIRE_ALARM";
    static final String CHANNEL_ID_HEADS_UP = "rafiq_reminder_channel";

    @Override
    public void onReceive(Context context, Intent intent) {
        if (!ACTION_FIRE_ALARM.equals(intent.getAction())) return;

        String reminderId   = intent.getStringExtra(AlarmActivity.EXTRA_REMINDER_ID);
        String reminderType = intent.getStringExtra(AlarmActivity.EXTRA_REMINDER_TYPE);
        String title        = intent.getStringExtra(AlarmActivity.EXTRA_TITLE);
        String body         = intent.getStringExtra(AlarmActivity.EXTRA_BODY);
        String scheduledAt  = intent.getStringExtra(AlarmActivity.EXTRA_SCHEDULED_AT);

        if (isBlank(reminderId)) return;

        title = valueOrDefault(title, "Reminder");
        body = valueOrDefault(body, "Time for your reminder");
        reminderType = valueOrDefault(reminderType, "Medication");

        // 1. Start foreground alarm service (sound + vibration)
        Intent serviceIntent = new Intent(context, AlarmService.class);
        serviceIntent.putExtra(AlarmActivity.EXTRA_REMINDER_ID, reminderId);
        serviceIntent.putExtra(AlarmActivity.EXTRA_REMINDER_TYPE, reminderType);
        serviceIntent.putExtra(AlarmActivity.EXTRA_TITLE, title);
        serviceIntent.putExtra(AlarmActivity.EXTRA_BODY, body);
        serviceIntent.putExtra(AlarmActivity.EXTRA_SCHEDULED_AT, scheduledAt);
        if (Build.VERSION.SDK_INT >= Build.VERSION_CODES.O) {
            context.startForegroundService(serviceIntent);
        } else {
            context.startService(serviceIntent);
        }

        // 2. Build full-screen intent → AlarmActivity over lock screen
        Intent alarmIntent = new Intent(context, AlarmActivity.class);
        alarmIntent.setFlags(Intent.FLAG_ACTIVITY_NEW_TASK | Intent.FLAG_ACTIVITY_CLEAR_TOP);
        alarmIntent.putExtra(AlarmActivity.EXTRA_REMINDER_ID, reminderId);
        alarmIntent.putExtra(AlarmActivity.EXTRA_REMINDER_TYPE, reminderType);
        alarmIntent.putExtra(AlarmActivity.EXTRA_TITLE, title);
        alarmIntent.putExtra(AlarmActivity.EXTRA_BODY, body);
        alarmIntent.putExtra(AlarmActivity.EXTRA_SCHEDULED_AT, scheduledAt);

        int piFlags = Build.VERSION.SDK_INT >= Build.VERSION_CODES.M
            ? PendingIntent.FLAG_IMMUTABLE | PendingIntent.FLAG_UPDATE_CURRENT
            : PendingIntent.FLAG_UPDATE_CURRENT;
        PendingIntent fullScreenPi = PendingIntent.getActivity(context, stableId(reminderId), alarmIntent, piFlags);

        // 3. Post heads-up notification with full-screen intent
        createHeadsUpChannel(context);
        Notification notification = new NotificationCompat.Builder(context, CHANNEL_ID_HEADS_UP)
            .setSmallIcon(android.R.drawable.ic_lock_silent_mode_off)
            .setContentTitle(title)
            .setContentText(body)
            .setPriority(NotificationCompat.PRIORITY_HIGH)
            .setCategory(NotificationCompat.CATEGORY_ALARM)
            .setVisibility(NotificationCompat.VISIBILITY_PUBLIC)
            .setFullScreenIntent(fullScreenPi, true)  // triggers lock-screen activity
            .setOngoing(true)
            .setAutoCancel(false)
            .build();

        NotificationManager nm = (NotificationManager) context.getSystemService(Context.NOTIFICATION_SERVICE);
        // Use reminderId hashCode as notification ID so each reminder has a unique notification
        if (nm != null) {
            nm.notify(stableId(reminderId), notification);
        }
    }

    // ── Static helpers called by AlarmSchedulerPlugin ─────────────────────────

    /**
     * Schedules an exact alarm for the given reminder.
     * Called from the Capacitor plugin bridge on the Angular side.
     */
    public static void scheduleAlarm(
            Context context,
            String reminderId,
            String reminderType,
            String title,
            String body,
            String scheduledAt,
            long triggerAtMillis) {

        if (isBlank(reminderId) || triggerAtMillis <= System.currentTimeMillis()) return;

        reminderType = valueOrDefault(reminderType, "Medication");
        title = valueOrDefault(title, "Reminder");
        body = valueOrDefault(body, "Time for your reminder");

        Intent intent = new Intent(context, AlarmReceiver.class);
        intent.setAction(ACTION_FIRE_ALARM);
        intent.putExtra(AlarmActivity.EXTRA_REMINDER_ID, reminderId);
        intent.putExtra(AlarmActivity.EXTRA_REMINDER_TYPE, reminderType);
        intent.putExtra(AlarmActivity.EXTRA_TITLE, title);
        intent.putExtra(AlarmActivity.EXTRA_BODY, body);
        intent.putExtra(AlarmActivity.EXTRA_SCHEDULED_AT, scheduledAt);

        int piFlags = Build.VERSION.SDK_INT >= Build.VERSION_CODES.M
            ? PendingIntent.FLAG_IMMUTABLE | PendingIntent.FLAG_UPDATE_CURRENT
            : PendingIntent.FLAG_UPDATE_CURRENT;

        PendingIntent pi = PendingIntent.getBroadcast(
            context, stableId(reminderId), intent, piFlags);

        AlarmManager am = (AlarmManager) context.getSystemService(Context.ALARM_SERVICE);
        if (am == null) return;

        Intent showIntent = new Intent(context, AlarmActivity.class);
        showIntent.setFlags(Intent.FLAG_ACTIVITY_NEW_TASK | Intent.FLAG_ACTIVITY_CLEAR_TOP);
        showIntent.putExtra(AlarmActivity.EXTRA_REMINDER_ID, reminderId);
        showIntent.putExtra(AlarmActivity.EXTRA_REMINDER_TYPE, reminderType);
        showIntent.putExtra(AlarmActivity.EXTRA_TITLE, title);
        showIntent.putExtra(AlarmActivity.EXTRA_BODY, body);
        showIntent.putExtra(AlarmActivity.EXTRA_SCHEDULED_AT, scheduledAt);
        PendingIntent showPi = PendingIntent.getActivity(
            context, stableId(reminderId), showIntent, piFlags);

        if (Build.VERSION.SDK_INT >= Build.VERSION_CODES.LOLLIPOP) {
            AlarmManager.AlarmClockInfo alarmClockInfo =
                new AlarmManager.AlarmClockInfo(triggerAtMillis, showPi);
            am.setAlarmClock(alarmClockInfo, pi);
        } else if (Build.VERSION.SDK_INT >= Build.VERSION_CODES.M) {
            am.setExactAndAllowWhileIdle(AlarmManager.RTC_WAKEUP, triggerAtMillis, pi);
        } else {
            am.setExact(AlarmManager.RTC_WAKEUP, triggerAtMillis, pi);
        }
    }

    /**
     * Cancels a previously scheduled alarm for a reminder.
     */
    public static void cancelAlarm(Context context, String reminderId) {
        if (isBlank(reminderId)) return;

        Intent intent = new Intent(context, AlarmReceiver.class);
        intent.setAction(ACTION_FIRE_ALARM);

        int piFlags = Build.VERSION.SDK_INT >= Build.VERSION_CODES.M
            ? PendingIntent.FLAG_IMMUTABLE | PendingIntent.FLAG_NO_CREATE
            : PendingIntent.FLAG_NO_CREATE;

        PendingIntent pi = PendingIntent.getBroadcast(
            context, stableId(reminderId), intent, piFlags);

        if (pi != null) {
            AlarmManager am = (AlarmManager) context.getSystemService(Context.ALARM_SERVICE);
            if (am != null) am.cancel(pi);
            pi.cancel();
        }
    }

    private static void createHeadsUpChannel(Context context) {
        if (Build.VERSION.SDK_INT >= Build.VERSION_CODES.O) {
            NotificationManager nm = (NotificationManager) context.getSystemService(Context.NOTIFICATION_SERVICE);
            if (nm.getNotificationChannel(CHANNEL_ID_HEADS_UP) != null) return;

            NotificationChannel channel = new NotificationChannel(
                CHANNEL_ID_HEADS_UP,
                "Rafiq Reminders",
                NotificationManager.IMPORTANCE_HIGH
            );
            channel.setDescription("Medication and appointment reminders");
            channel.setLockscreenVisibility(Notification.VISIBILITY_PUBLIC);
            nm.createNotificationChannel(channel);
        }
    }

    static int stableId(String reminderId) {
        int id = reminderId.hashCode() & 0x7fffffff;
        return id == 0 ? 1 : id;
    }

    private static String valueOrDefault(String value, String fallback) {
        return isBlank(value) ? fallback : value;
    }

    private static boolean isBlank(String value) {
        return value == null || value.trim().isEmpty();
    }
}
