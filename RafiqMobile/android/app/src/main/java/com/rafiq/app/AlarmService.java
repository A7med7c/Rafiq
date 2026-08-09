package com.rafiq.mobile;

import android.app.Notification;
import android.app.NotificationChannel;
import android.app.NotificationManager;
import android.app.PendingIntent;
import android.app.Service;
import android.content.Intent;
import android.media.AudioAttributes;
import android.media.MediaPlayer;
import android.net.Uri;
import android.os.Build;
import android.os.IBinder;
import android.os.VibrationEffect;
import android.os.Vibrator;
import android.os.VibratorManager;

import androidx.core.app.NotificationCompat;

/**
 * Foreground service responsible for playing the looping alarm sound and
 * continuous vibration when a reminder fires.
 *
 * Started by AlarmReceiver when the reminder time arrives. Stopped by
 * AlarmActivity.dismissAlarm() when the user takes an action.
 *
 * Running as a foreground service ensures the alarm continues even when:
 *   - the app is in the background
 *   - the device is in Doze mode (partial wake lock held by the foreground notification)
 *   - the screen is off
 */
public class AlarmService extends Service {

    // v2: bumped so the channel is re-created cleanly alongside the reminder channel.
    public static final String CHANNEL_ID_ALARM = "rafiq_alarm_channel_v2";
    static final int FOREGROUND_NOTIFICATION_ID = 9001;

    private MediaPlayer mediaPlayer;
    private Vibrator vibrator;
    private Intent alarmIntent;

    @Override
    public int onStartCommand(Intent intent, int flags, int startId) {
        alarmIntent = intent;
        String reminderId = intent != null ? intent.getStringExtra(AlarmActivity.EXTRA_REMINDER_ID) : null;
        android.util.Log.i(AlarmDiagnostics.TAG, "AlarmService.onStartCommand REACHED reminderId=" + reminderId);
        createAlarmChannel();
        // Use the SAME notification id AlarmReceiver's heads-up notification used
        // (stableId(reminderId), not a hardcoded constant). Android's notification id
        // namespace is shared between NotificationManager.notify() and
        // Service.startForeground() for the same app, so this REPLACES the heads-up
        // notification instead of stacking a second, separate one — this was the cause
        // of two Rafiq notifications appearing for a single reminder fire.
        int notificationId = reminderId != null ? AlarmReceiver.stableId(reminderId) : FOREGROUND_NOTIFICATION_ID;
        startForeground(notificationId, buildForegroundNotification());
        startSound();
        startVibration();
        return START_STICKY; // Restart if killed by OS (Doze recovery)
    }

    @Override
    public void onDestroy() {
        if (Build.VERSION.SDK_INT >= Build.VERSION_CODES.N) {
            stopForeground(STOP_FOREGROUND_REMOVE);
        } else {
            //noinspection deprecation
            stopForeground(true);
        }
        stopSound();
        stopVibration();
        super.onDestroy();
    }

    @Override
    public IBinder onBind(Intent intent) {
        return null; // Not a bound service
    }

    // ── Sound ─────────────────────────────────────────────────────────────────

    private void startSound() {
        try {
            // Use the default alarm sound from the device.
            // Override with a custom asset URI when a branded sound is added.
            Uri alarmSound = android.provider.Settings.System.DEFAULT_ALARM_ALERT_URI;

            mediaPlayer = new MediaPlayer();
            mediaPlayer.setAudioAttributes(
                new AudioAttributes.Builder()
                    .setUsage(AudioAttributes.USAGE_ALARM)
                    .setContentType(AudioAttributes.CONTENT_TYPE_SONIFICATION)
                    .build()
            );
            mediaPlayer.setDataSource(getApplicationContext(), alarmSound);
            mediaPlayer.setLooping(true);
            mediaPlayer.prepare();
            mediaPlayer.start();
        } catch (Exception e) {
            // Fallback: continue without sound if URI is unavailable
        }
    }

    private void stopSound() {
        if (mediaPlayer != null) {
            if (mediaPlayer.isPlaying()) mediaPlayer.stop();
            mediaPlayer.release();
            mediaPlayer = null;
        }
    }

    // ── Vibration ─────────────────────────────────────────────────────────────

    private static final long[] VIBRATE_PATTERN = { 0, 1000, 500 }; // off, on, off (repeating)

    private void startVibration() {
        if (Build.VERSION.SDK_INT >= Build.VERSION_CODES.S) {
            VibratorManager vm = (VibratorManager) getSystemService(VIBRATOR_MANAGER_SERVICE);
            if (vm != null) vibrator = vm.getDefaultVibrator();
        } else {
            //noinspection deprecation
            vibrator = (Vibrator) getSystemService(VIBRATOR_SERVICE);
        }

        if (vibrator == null) return;

        if (Build.VERSION.SDK_INT >= Build.VERSION_CODES.O) {
            VibrationEffect effect = VibrationEffect.createWaveform(VIBRATE_PATTERN, 0); // 0 = repeat from start
            vibrator.vibrate(effect);
        } else {
            //noinspection deprecation
            vibrator.vibrate(VIBRATE_PATTERN, 0);
        }
    }

    private void stopVibration() {
        if (vibrator != null) {
            vibrator.cancel();
            vibrator = null;
        }
    }

    // ── Foreground notification ───────────────────────────────────────────────

    private void createAlarmChannel() {
        if (Build.VERSION.SDK_INT >= Build.VERSION_CODES.O) {
            NotificationManager nm = getSystemService(NotificationManager.class);
            if (nm.getNotificationChannel(CHANNEL_ID_ALARM) != null) return;

            NotificationChannel channel = new NotificationChannel(
                CHANNEL_ID_ALARM,
                "Rafiq Alarms",
                NotificationManager.IMPORTANCE_HIGH
            );
            channel.setDescription("Medication and appointment alarm notifications");
            channel.enableVibration(false); // Vibration handled by AlarmService itself
            channel.setSound(null, null);   // Sound handled by MediaPlayer
            channel.setLockscreenVisibility(Notification.VISIBILITY_PUBLIC);
            nm.createNotificationChannel(channel);
        }
    }

    private Notification buildForegroundNotification() {
        // Tap the notification body (not an action button) → bring AlarmActivity to front.
        Intent openIntent = new Intent(this, AlarmActivity.class);
        openIntent.setFlags(Intent.FLAG_ACTIVITY_NEW_TASK | Intent.FLAG_ACTIVITY_CLEAR_TOP);
        String reminderId = null;
        String reminderType = "Medication";
        String title = "Reminder";
        String body = "Tap to view your reminder";
        String scheduledAt = null;
        if (alarmIntent != null) {
            reminderId = alarmIntent.getStringExtra(AlarmActivity.EXTRA_REMINDER_ID);
            reminderType = valueOrDefault(alarmIntent.getStringExtra(AlarmActivity.EXTRA_REMINDER_TYPE), reminderType);
            title = valueOrDefault(alarmIntent.getStringExtra(AlarmActivity.EXTRA_TITLE), title);
            body = valueOrDefault(alarmIntent.getStringExtra(AlarmActivity.EXTRA_BODY), body);
            scheduledAt = alarmIntent.getStringExtra(AlarmActivity.EXTRA_SCHEDULED_AT);
            openIntent.putExtras(alarmIntent);
        }
        int flags = Build.VERSION.SDK_INT >= Build.VERSION_CODES.M
            ? PendingIntent.FLAG_IMMUTABLE | PendingIntent.FLAG_UPDATE_CURRENT
            : PendingIntent.FLAG_UPDATE_CURRENT;
        PendingIntent pi = PendingIntent.getActivity(
            this,
            reminderId != null ? AlarmReceiver.stableId(reminderId) : FOREGROUND_NOTIFICATION_ID,
            openIntent,
            flags
        );

        boolean isAppointment = "Appointment".equalsIgnoreCase(reminderType);
        String takeLabel = isAppointment ? "I Attended" : "Taken";

        NotificationCompat.Builder builder = new NotificationCompat.Builder(this, CHANNEL_ID_ALARM)
            .setSmallIcon(android.R.drawable.ic_lock_silent_mode_off)
            .setContentTitle(title)
            .setContentText(body)
            .setContentIntent(pi)
            .setOngoing(true)
            .setPriority(NotificationCompat.PRIORITY_HIGH)
            .setVisibility(NotificationCompat.VISIBILITY_PUBLIC)
            .setCategory(NotificationCompat.CATEGORY_ALARM);

        // This notification replaces AlarmReceiver's heads-up notification (same id) once
        // the foreground service starts, so it carries the SAME actions — otherwise the
        // buttons the user actually sees would disappear the moment the service starts.
        if (reminderId != null) {
            builder.addAction(0, takeLabel, AlarmActionReceiver.buildTakePendingIntent(this, reminderId, reminderType));
            if (!isAppointment) {
                builder.addAction(0, "Snooze 10 min",
                    AlarmActionReceiver.buildSnoozePendingIntent(this, reminderId, reminderType, title, body, scheduledAt));
            }
        }

        return builder.build();
    }

    private String valueOrDefault(String value, String fallback) {
        return value == null || value.trim().isEmpty() ? fallback : value;
    }
}
