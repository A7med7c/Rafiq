package com.rafiq.mobile;

import android.content.Context;
import android.content.SharedPreferences;

import org.json.JSONObject;

import java.time.Instant;
import java.util.Map;

final class NativeReminderStore {

    private static final String PREFS_NAME = "rafiq_native_alarms";

    private NativeReminderStore() {
    }

    static void saveAlarm(Context context, String reminderId, String reminderType, String title, String body, String scheduledAt, long triggerAtMillis) {
        if (isBlank(reminderId)) return;
        SharedPreferences prefs = context.getSharedPreferences(PREFS_NAME, Context.MODE_PRIVATE);
        try {
            JSONObject json = new JSONObject();
            json.put("reminderId", reminderId);
            json.put("reminderType", reminderType);
            json.put("title", title);
            json.put("body", body);
            json.put("scheduledAt", scheduledAt);
            json.put("triggerAtMillis", triggerAtMillis);
            prefs.edit().putString(reminderId, json.toString()).apply();
        } catch (Exception ignored) {
        }
    }

    static void removeAlarm(Context context, String reminderId) {
        if (isBlank(reminderId)) return;
        SharedPreferences prefs = context.getSharedPreferences(PREFS_NAME, Context.MODE_PRIVATE);
        prefs.edit().remove(reminderId).apply();
    }

    static void restoreFutureAlarms(Context context) {
        SharedPreferences prefs = context.getSharedPreferences(PREFS_NAME, Context.MODE_PRIVATE);
        Map<String, ?> allEntries = prefs.getAll();
        long now = System.currentTimeMillis();

        SharedPreferences.Editor editor = prefs.edit();
        boolean hasChanges = false;

        for (Map.Entry<String, ?> entry : allEntries.entrySet()) {
            try {
                String jsonStr = (String) entry.getValue();
                JSONObject json = new JSONObject(jsonStr);
                String reminderId = json.getString("reminderId");
                String reminderType = json.optString("reminderType", "Medication");
                String title = json.optString("title", "Reminder");
                String body = json.optString("body", "Time for your reminder");
                String scheduledAt = json.getString("scheduledAt");
                long triggerAtMillis = json.getLong("triggerAtMillis");

                if (triggerAtMillis > now) {
                    AlarmReceiver.scheduleAlarm(
                        context,
                        reminderId,
                        reminderType,
                        title,
                        body,
                        scheduledAt,
                        triggerAtMillis
                    );
                } else {
                    editor.remove(reminderId);
                    hasChanges = true;
                }
            } catch (Exception e) {
                editor.remove(entry.getKey());
                hasChanges = true;
            }
        }

        if (hasChanges) {
            editor.apply();
        }
    }

    static void updateSnooze(Context context, String reminderId, long triggerAtMillis) {
        if (isBlank(reminderId)) return;
        SharedPreferences prefs = context.getSharedPreferences(PREFS_NAME, Context.MODE_PRIVATE);
        String jsonStr = prefs.getString(reminderId, null);
        if (jsonStr != null) {
            try {
                JSONObject json = new JSONObject(jsonStr);
                String nextTime = Instant.ofEpochMilli(triggerAtMillis).toString();
                json.put("scheduledAt", nextTime);
                json.put("triggerAtMillis", triggerAtMillis);
                prefs.edit().putString(reminderId, json.toString()).apply();
            } catch (Exception ignored) {
            }
        }
    }

    private static boolean isBlank(String value) {
        return value == null || value.trim().isEmpty();
    }
}
