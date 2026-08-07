package com.rafiq.mobile;

import android.content.BroadcastReceiver;
import android.content.Context;
import android.content.Intent;

/**
 * Receives BOOT_COMPLETED broadcast so alarms can be re-registered after a
 * device reboot. AlarmManager alarms do NOT survive reboots — this receiver
 * bridges that gap.
 *
 * On boot, it starts RafiqMobile (the main app process) in the background.
 * The Angular bootstrap sequence then runs ReminderBootstrapService which
 * restores SQLite-cached reminders and re-schedules their alarms via
 * AlarmSchedulerPlugin.scheduleAlarm().
 *
 * This receiver does NOT try to re-schedule alarms itself — it delegates to
 * the existing mobile bootstrap flow to keep a single source of truth.
 */
public class BootReceiver extends BroadcastReceiver {

    @Override
    public void onReceive(Context context, Intent intent) {
        String action = intent.getAction();
        if (!Intent.ACTION_BOOT_COMPLETED.equals(action)
                && !"android.intent.action.QUICKBOOT_POWERON".equals(action)) {
            return;
        }

        NativeReminderStore.restoreFutureAlarms(context);
    }
}
