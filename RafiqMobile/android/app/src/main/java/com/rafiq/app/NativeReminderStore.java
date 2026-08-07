package com.rafiq.mobile;

import android.content.ContentValues;
import android.content.Context;
import android.database.Cursor;
import android.database.sqlite.SQLiteDatabase;

import java.io.File;
import java.time.Instant;

final class NativeReminderStore {

    private static final String TABLE_NAME = "reminders";
    private static final String[] DATABASE_NAMES = {
        "offline_remindersSQLite.db",
        "offline_reminders.db",
        "offline_reminders"
    };

    private NativeReminderStore() {
    }

    static void restoreFutureAlarms(Context context) {
        SQLiteDatabase db = openDatabase(context);
        if (db == null) return;

        try (Cursor cursor = db.query(
            TABLE_NAME,
            new String[] { "serverId", "title", "body", "type", "reminderTime" },
            "status = ?",
            new String[] { "scheduled" },
            null,
            null,
            null
        )) {
            long now = System.currentTimeMillis();
            while (cursor.moveToNext()) {
                String serverId = cursor.getString(0);
                String title = cursor.getString(1);
                String body = cursor.getString(2);
                String type = cursor.getString(3);
                String reminderTime = cursor.getString(4);
                long triggerAtMillis = parseMillis(reminderTime);

                if (isBlank(serverId) || triggerAtMillis <= now) {
                    continue;
                }

                AlarmReceiver.scheduleAlarm(
                    context,
                    serverId,
                    "appointment".equalsIgnoreCase(type) ? "Appointment" : "Medication",
                    title,
                    body,
                    reminderTime,
                    triggerAtMillis
                );
            }
        } catch (Exception ignored) {
        } finally {
            db.close();
        }
    }

    static void updateSnooze(Context context, String reminderId, long triggerAtMillis) {
        if (isBlank(reminderId)) return;

        SQLiteDatabase db = openDatabase(context);
        if (db == null) return;

        try {
            ContentValues values = new ContentValues();
            String nextTime = Instant.ofEpochMilli(triggerAtMillis).toString();
            values.put("reminderTime", nextTime);
            values.put("lastUpdated", nextTime);
            values.put("status", "scheduled");
            db.update(TABLE_NAME, values, "serverId = ?", new String[] { reminderId });
        } catch (Exception ignored) {
        } finally {
            db.close();
        }
    }

    private static SQLiteDatabase openDatabase(Context context) {
        for (String name : DATABASE_NAMES) {
            File file = context.getDatabasePath(name);
            if (!file.exists()) continue;

            try {
                return SQLiteDatabase.openDatabase(
                    file.getAbsolutePath(),
                    null,
                    SQLiteDatabase.OPEN_READWRITE
                );
            } catch (Exception ignored) {
            }
        }

        return null;
    }

    private static long parseMillis(String value) {
        if (isBlank(value)) return -1;

        try {
            return Instant.parse(value).toEpochMilli();
        } catch (Exception ignored) {
            return -1;
        }
    }

    private static boolean isBlank(String value) {
        return value == null || value.trim().isEmpty();
    }
}
