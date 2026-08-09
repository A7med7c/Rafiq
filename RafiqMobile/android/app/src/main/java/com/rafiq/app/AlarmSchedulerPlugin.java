package com.rafiq.mobile;

import android.content.Context;
import android.content.Intent;
import android.content.SharedPreferences;
import android.net.Uri;
import android.os.Build;
import android.os.PowerManager;
import android.provider.Settings;
import android.util.Log;

import com.getcapacitor.JSObject;
import com.getcapacitor.Plugin;
import com.getcapacitor.PluginCall;
import com.getcapacitor.PluginMethod;
import com.getcapacitor.annotation.CapacitorPlugin;

/**
 * Capacitor plugin that exposes Android alarm scheduling to the Angular layer.
 *
 * Angular calls:
 *   AlarmSchedulerPlugin.scheduleAlarm({ reminderId, reminderType, title, body, scheduledAt })
 *   AlarmSchedulerPlugin.cancelAlarm({ reminderId })
 *   AlarmSchedulerPlugin.dismissAlarm({ reminderId })
 *
 * These delegate to AlarmReceiver.scheduleAlarm / cancelAlarm (native Android
 * AlarmManager) so alarms fire even when the app is killed or the device is
 * in Doze mode.
 */
@CapacitorPlugin(name = "AlarmScheduler")
public class AlarmSchedulerPlugin extends Plugin {

    private static final String PREFS_NAME = "rafiq_alarm_actions";
    private static final String KEY_ACTION = "action";
    private static final String KEY_REMINDER_ID = "reminderId";
    private static final String KEY_REMINDER_TYPE = "reminderType";
    private static final String KEY_SNOOZE_MINUTES = "snoozeMinutes";

    static void recordAlarmAction(Context context, Intent intent) {
        if (context == null || intent == null) return;

        String action = intent.getStringExtra(AlarmActivity.EXTRA_ALARM_ACTION);
        String reminderId = intent.getStringExtra(AlarmActivity.EXTRA_REMINDER_ID);
        if (isBlank(action) || isBlank(reminderId)) return;

        context.getSharedPreferences(PREFS_NAME, Context.MODE_PRIVATE)
            .edit()
            .putString(KEY_ACTION, action)
            .putString(KEY_REMINDER_ID, reminderId)
            .putString(KEY_REMINDER_TYPE, intent.getStringExtra(AlarmActivity.EXTRA_REMINDER_TYPE))
            .putInt(KEY_SNOOZE_MINUTES, intent.getIntExtra(KEY_SNOOZE_MINUTES, 0))
            .apply();
    }

    @PluginMethod
    public void scheduleAlarm(PluginCall call) {
        String reminderId   = call.getString("reminderId");
        String reminderType = call.getString("reminderType", "Medication");
        String title        = call.getString("title", "Reminder");
        String body         = call.getString("body", "Time for your reminder");
        // scheduledAt is an ISO-8601 string; convert to epoch millis
        String scheduledAt  = call.getString("scheduledAt");

        if (reminderId == null || scheduledAt == null) {
            call.reject("reminderId and scheduledAt are required");
            return;
        }

        long triggerAtMillis;
        try {
            // OffsetDateTime handles both "Z" and "+00:00" UTC suffixes.
            // Instant.parse() only accepts the "Z" form; the Angular layer sends "+00:00",
            // which caused every alarm to be silently dropped with a parse exception.
            triggerAtMillis = java.time.OffsetDateTime.parse(scheduledAt).toInstant().toEpochMilli();
        } catch (Exception e) {
            // Do not silently swallow — surface the exact reminder + value that failed.
            Log.e(AlarmDiagnostics.TAG, "scheduleAlarm parse FAILED reminderId=" + reminderId
                + " scheduledAt=" + scheduledAt, e);
            call.reject("Invalid scheduledAt format: " + scheduledAt);
            return;
        }

        if (triggerAtMillis <= System.currentTimeMillis()) {
            // Past reminder — do not schedule; signal success so the mobile layer
            // can treat it as already fired and clean up.
            JSObject result = new JSObject();
            result.put("skipped", true);
            result.put("reason", "scheduledAt is in the past");
            call.resolve(result);
            return;
        }

        String occurrenceKey = AlarmDiagnostics.occurrenceKey(reminderId, scheduledAt);
        int occId = AlarmDiagnostics.occurrenceId(reminderId, scheduledAt);

        Log.i(AlarmDiagnostics.TAG, "Plugin.scheduleAlarm reminderId=" + reminderId
            + " type=" + reminderType + " scheduledAt=" + scheduledAt + " occId=" + occId);

        AlarmReceiver.scheduleAlarm(
            getContext(), reminderId, reminderType, title, body, scheduledAt, triggerAtMillis);

        NativeReminderStore.saveAlarm(
            getContext(), occurrenceKey, reminderId, reminderType, title, body, scheduledAt, triggerAtMillis);

        JSObject result = new JSObject();
        result.put("scheduled", true);
        result.put("reminderId", reminderId);
        call.resolve(result);
    }

    @PluginMethod
    public void cancelAlarm(PluginCall call) {
        String reminderId = call.getString("reminderId");
        if (reminderId == null) {
            call.reject("reminderId is required");
            return;
        }

        // Cancels every occurrence PendingIntent and clears the NativeReminderStore
        // entries for this reminder in one pass.
        AlarmReceiver.cancelAlarm(getContext(), reminderId);

        JSObject result = new JSObject();
        result.put("cancelled", true);
        result.put("reminderId", reminderId);
        call.resolve(result);
    }

    @PluginMethod
    public void dismissAlarm(PluginCall call) {
        String reminderId = call.getString("reminderId");

        // Stop the alarm service (sound + vibration)
        Context ctx = getContext();
        Intent stopIntent = new Intent(ctx, AlarmService.class);
        ctx.stopService(stopIntent);

        // Cancel the ongoing notification posted by AlarmReceiver
        if (reminderId != null) {
            android.app.NotificationManager nm =
                (android.app.NotificationManager) ctx.getSystemService(Context.NOTIFICATION_SERVICE);
            if (nm != null) {
                nm.cancel(AlarmReceiver.stableId(reminderId));
            }
        }

        JSObject result = new JSObject();
        result.put("dismissed", true);
        call.resolve(result);
    }

    @PluginMethod
    public void consumePendingAction(PluginCall call) {
        SharedPreferences prefs = getContext().getSharedPreferences(PREFS_NAME, Context.MODE_PRIVATE);
        String action = prefs.getString(KEY_ACTION, null);
        String reminderId = prefs.getString(KEY_REMINDER_ID, null);

        JSObject result = new JSObject();
        if (isBlank(action) || isBlank(reminderId)) {
            result.put("hasAction", false);
            call.resolve(result);
            return;
        }

        result.put("hasAction", true);
        result.put("action", action);
        result.put("reminderId", reminderId);
        result.put("reminderType", prefs.getString(KEY_REMINDER_TYPE, "Medication"));
        result.put("snoozeMinutes", prefs.getInt(KEY_SNOOZE_MINUTES, 0));

        call.resolve(result);
    }

    @PluginMethod
    public void completePendingAction(PluginCall call) {
        String reminderId = call.getString("reminderId");
        String action = call.getString("action");

        SharedPreferences prefs = getContext().getSharedPreferences(PREFS_NAME, Context.MODE_PRIVATE);
        String storedReminderId = prefs.getString(KEY_REMINDER_ID, null);
        String storedAction = prefs.getString(KEY_ACTION, null);

        if (!isBlank(reminderId)
            && !isBlank(action)
            && reminderId.equals(storedReminderId)
            && action.equals(storedAction)) {
            prefs.edit().clear().apply();
        }

        JSObject result = new JSObject();
        result.put("completed", true);
        call.resolve(result);
    }

    /**
     * Returns whether the app is currently excluded from battery optimizations.
     *
     * On Android 6+ (API 23+), OEM battery managers can cancel AlarmManager alarms
     * when the app is killed unless the app is on the "unrestricted" battery list.
     * This method lets Angular check the state so it can prompt the user once.
     *
     * Result: { isIgnoring: boolean }
     */
    @PluginMethod
    public void checkBatteryOptimization(PluginCall call) {
        JSObject result = new JSObject();
        if (Build.VERSION.SDK_INT >= Build.VERSION_CODES.M) {
            PowerManager pm = (PowerManager) getContext().getSystemService(Context.POWER_SERVICE);
            boolean isIgnoring = pm != null
                && pm.isIgnoringBatteryOptimizations(getContext().getPackageName());
            result.put("isIgnoring", isIgnoring);
        } else {
            // Pre-API 23: battery optimization does not exist; always report exempt.
            result.put("isIgnoring", true);
        }
        call.resolve(result);
    }

    /**
     * Opens the system dialog asking the user to exclude this app from battery
     * optimizations.  Requires android.permission.REQUEST_IGNORE_BATTERY_OPTIMIZATIONS
     * in the manifest (declared in AndroidManifest.xml).
     *
     * On Android < 6 (API 23) this is a no-op (battery optimization does not exist).
     * Call checkBatteryOptimization() first and only call this when isIgnoring=false.
     *
     * Result: { requested: boolean }
     */
    @PluginMethod
    public void requestBatteryOptimizationExemption(PluginCall call) {
        if (Build.VERSION.SDK_INT >= Build.VERSION_CODES.M) {
            try {
                Intent intent = new Intent(Settings.ACTION_REQUEST_IGNORE_BATTERY_OPTIMIZATIONS);
                intent.setData(Uri.parse("package:" + getContext().getPackageName()));
                intent.addFlags(Intent.FLAG_ACTIVITY_NEW_TASK);
                getContext().startActivity(intent);
                JSObject result = new JSObject();
                result.put("requested", true);
                call.resolve(result);
            } catch (Exception e) {
                Log.w(AlarmDiagnostics.TAG, "requestBatteryOptimizationExemption failed, "
                    + "falling back to app settings", e);
                // Fallback: open app details settings so the user can find Battery
                // manually. Some OEMs disable ACTION_REQUEST_IGNORE_BATTERY_OPTIMIZATIONS.
                try {
                    Intent fallback = new Intent(Settings.ACTION_APPLICATION_DETAILS_SETTINGS);
                    fallback.setData(Uri.parse("package:" + getContext().getPackageName()));
                    fallback.addFlags(Intent.FLAG_ACTIVITY_NEW_TASK);
                    getContext().startActivity(fallback);
                } catch (Exception ignored) {
                }
                JSObject result = new JSObject();
                result.put("requested", false);
                call.resolve(result);
            }
        } else {
            JSObject result = new JSObject();
            result.put("requested", false);
            call.resolve(result);
        }
    }

    private static boolean isBlank(String value) {
        return value == null || value.trim().isEmpty();
    }
}
