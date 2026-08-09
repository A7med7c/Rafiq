package com.rafiq.mobile;

/**
 * Single source of truth for the alarm-pipeline log tag and the deterministic
 * occurrence identity shared across the plugin, receiver, service, activity and
 * boot-restore paths.
 *
 * Occurrence identity = {@code reminderId|scheduledAt}. This keys the
 * AlarmManager PendingIntent request code and the NativeReminderStore entry, so
 * every occurrence of a reminder is scheduled and cancelled independently while
 * remaining deterministic across syncs and reboots.
 */
final class AlarmDiagnostics {

    static final String TAG = "RafiqAlarm";

    private AlarmDiagnostics() {
    }

    /** Deterministic occurrence key. */
    static String occurrenceKey(String reminderId, String scheduledAt) {
        return (reminderId == null ? "" : reminderId) + "|" + (scheduledAt == null ? "" : scheduledAt);
    }

    /** Stable non-negative 31-bit hash of an arbitrary key (matches the TS side). */
    static int stableId(String key) {
        int hash = 0;
        if (key != null) {
            for (int i = 0; i < key.length(); i++) {
                hash = 31 * hash + key.charAt(i);
            }
        }
        int id = hash & 0x7fffffff;
        return id == 0 ? 1 : id;
    }

    /** Deterministic occurrence id used as the AlarmManager/PendingIntent request code. */
    static int occurrenceId(String reminderId, String scheduledAt) {
        return stableId(occurrenceKey(reminderId, scheduledAt));
    }
}
