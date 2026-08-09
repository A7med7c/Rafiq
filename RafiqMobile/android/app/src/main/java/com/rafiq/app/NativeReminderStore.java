package com.rafiq.mobile;

import android.content.Context;
import android.content.SharedPreferences;
import android.util.Log;

import org.json.JSONObject;

import java.util.ArrayList;
import java.util.List;
import java.util.Map;

/**
 * Durable record of every scheduled native alarm, used to re-arm alarms after a
 * device reboot (AlarmManager entries do not survive reboot).
 *
 * Keyed by the OCCURRENCE key ({@code reminderId|scheduledAt}) so that the
 * multiple daily occurrences of one medication reminder are stored — and can be
 * cancelled — independently. Each stored value also carries the plain
 * {@code reminderId} so all occurrences of a reminder can be found for a
 * reminder-level cancel.
 */
final class NativeReminderStore {

    private static final String TAG = AlarmDiagnostics.TAG;
    private static final String PREFS_NAME = "rafiq_native_alarms";

    private NativeReminderStore() {
    }

    static void saveAlarm(Context context, String occurrenceKey, String reminderId, String reminderType,
                          String title, String body, String scheduledAt, long triggerAtMillis) {
        if (isBlank(occurrenceKey) || isBlank(reminderId)) return;
        SharedPreferences prefs = context.getSharedPreferences(PREFS_NAME, Context.MODE_PRIVATE);
        try {
            JSONObject json = new JSONObject();
            json.put("reminderId", reminderId);
            json.put("reminderType", reminderType);
            json.put("title", title);
            json.put("body", body);
            json.put("scheduledAt", scheduledAt);
            json.put("triggerAtMillis", triggerAtMillis);
            prefs.edit().putString(occurrenceKey, json.toString()).apply();
        } catch (Exception e) {
            Log.e(TAG, "NativeReminderStore.saveAlarm failed for " + occurrenceKey, e);
        }
    }

    static void removeAlarm(Context context, String occurrenceKey) {
        if (isBlank(occurrenceKey)) return;
        SharedPreferences prefs = context.getSharedPreferences(PREFS_NAME, Context.MODE_PRIVATE);
        prefs.edit().remove(occurrenceKey).apply();
    }

    /** All stored scheduledAt values for a given reminderId (one per occurrence). */
    static List<String> getScheduledAtsForReminder(Context context, String reminderId) {
        List<String> times = new ArrayList<>();
        if (isBlank(reminderId)) return times;

        SharedPreferences prefs = context.getSharedPreferences(PREFS_NAME, Context.MODE_PRIVATE);
        for (Map.Entry<String, ?> entry : prefs.getAll().entrySet()) {
            try {
                JSONObject json = new JSONObject((String) entry.getValue());
                if (reminderId.equals(json.optString("reminderId"))) {
                    times.add(json.optString("scheduledAt"));
                }
            } catch (Exception ignored) {
            }
        }
        return times;
    }

    /** Removes every stored occurrence belonging to a reminderId. */
    static void removeAllForReminder(Context context, String reminderId) {
        if (isBlank(reminderId)) return;
        SharedPreferences prefs = context.getSharedPreferences(PREFS_NAME, Context.MODE_PRIVATE);
        SharedPreferences.Editor editor = prefs.edit();
        boolean changed = false;
        for (Map.Entry<String, ?> entry : prefs.getAll().entrySet()) {
            try {
                JSONObject json = new JSONObject((String) entry.getValue());
                if (reminderId.equals(json.optString("reminderId"))) {
                    editor.remove(entry.getKey());
                    changed = true;
                }
            } catch (Exception e) {
                editor.remove(entry.getKey());
                changed = true;
            }
        }
        if (changed) editor.apply();
    }

    static void restoreFutureAlarms(Context context) {
        SharedPreferences prefs = context.getSharedPreferences(PREFS_NAME, Context.MODE_PRIVATE);
        Map<String, ?> allEntries = prefs.getAll();
        long now = System.currentTimeMillis();

        SharedPreferences.Editor editor = prefs.edit();
        boolean hasChanges = false;
        int restored = 0;

        for (Map.Entry<String, ?> entry : allEntries.entrySet()) {
            try {
                JSONObject json = new JSONObject((String) entry.getValue());
                String reminderId = json.getString("reminderId");
                String reminderType = json.optString("reminderType", "Medication");
                String title = json.optString("title", "Reminder");
                String body = json.optString("body", "Time for your reminder");
                String scheduledAt = json.getString("scheduledAt");
                long triggerAtMillis = json.getLong("triggerAtMillis");

                if (triggerAtMillis > now) {
                    // AlarmReceiver.scheduleAlarm recomputes the occurrence key from
                    // reminderId + scheduledAt, so the re-armed PendingIntent matches.
                    AlarmReceiver.scheduleAlarm(
                        context, reminderId, reminderType, title, body, scheduledAt, triggerAtMillis);
                    restored++;
                } else {
                    editor.remove(entry.getKey());
                    hasChanges = true;
                }
            } catch (Exception e) {
                editor.remove(entry.getKey());
                hasChanges = true;
            }
        }

        if (hasChanges) editor.apply();
        Log.i(TAG, "BootReceiver restore: re-armed " + restored + " future alarm(s)");
    }

    private static boolean isBlank(String value) {
        return value == null || value.trim().isEmpty();
    }
}
