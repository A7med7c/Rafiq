package com.rafiq.mobile;

import android.app.Activity;
import android.app.KeyguardManager;
import android.app.NotificationManager;
import android.content.Context;
import android.content.Intent;
import android.graphics.Color;
import android.graphics.Typeface;
import android.os.Build;
import android.os.Bundle;
import android.view.Gravity;
import android.view.WindowManager;
import android.widget.Button;
import android.widget.LinearLayout;
import android.widget.TextView;

/**
 * Native full-screen alarm activity shown over the lock screen when a reminder fires.
 */
public class AlarmActivity extends Activity {

    public static final String EXTRA_REMINDER_ID   = "reminderId";
    public static final String EXTRA_REMINDER_TYPE = "reminderType";
    public static final String EXTRA_TITLE         = "title";
    public static final String EXTRA_BODY          = "body";
    public static final String EXTRA_SCHEDULED_AT  = "scheduledAt";
    public static final String EXTRA_ALARM_ACTION  = "alarmAction";

    public static final String ACTION_TAKE_MEDICINE = "takeMedicine";
    public static final String ACTION_SNOOZE        = "snooze";
    public static final String ACTION_DISMISS       = "dismiss";
    public static final String EXTRA_SNOOZE_MINUTES = "snoozeMinutes";

    private static final long SNOOZE_DELAY_MILLIS = 10 * 60 * 1000L;
    private static final int SNOOZE_MINUTES = 10;

    @Override
    protected void onCreate(Bundle savedInstanceState) {
        enableOverLockScreen();
        super.onCreate(savedInstanceState);
        renderAlarmUi();
    }

    private void enableOverLockScreen() {
        getWindow().addFlags(WindowManager.LayoutParams.FLAG_KEEP_SCREEN_ON);
        if (Build.VERSION.SDK_INT >= Build.VERSION_CODES.O_MR1) {
            setShowWhenLocked(true);
            setTurnScreenOn(true);
            KeyguardManager keyguardManager = (KeyguardManager) getSystemService(Context.KEYGUARD_SERVICE);
            if (keyguardManager != null) {
                keyguardManager.requestDismissKeyguard(this, null);
            }
        } else {
            //noinspection deprecation
            getWindow().addFlags(
                WindowManager.LayoutParams.FLAG_SHOW_WHEN_LOCKED
                    | WindowManager.LayoutParams.FLAG_TURN_SCREEN_ON
                    | WindowManager.LayoutParams.FLAG_DISMISS_KEYGUARD
            );
        }
    }

    private void renderAlarmUi() {
        Intent intent = getIntent();
        String title = valueOrDefault(intent.getStringExtra(EXTRA_TITLE), "Reminder");
        String body = valueOrDefault(intent.getStringExtra(EXTRA_BODY), "Time for your reminder");
        String reminderType = valueOrDefault(intent.getStringExtra(EXTRA_REMINDER_TYPE), "Medication");

        LinearLayout root = new LinearLayout(this);
        root.setOrientation(LinearLayout.VERTICAL);
        root.setGravity(Gravity.CENTER);
        root.setPadding(dp(28), dp(32), dp(28), dp(32));
        root.setBackgroundColor(Color.rgb(9, 23, 37));

        TextView typeView = new TextView(this);
        typeView.setText(reminderType.equalsIgnoreCase("Appointment") ? "Appointment Reminder" : "Medication Reminder");
        typeView.setTextColor(Color.rgb(130, 204, 221));
        typeView.setTextSize(16);
        typeView.setGravity(Gravity.CENTER);
        root.addView(typeView, matchWrap());

        TextView titleView = new TextView(this);
        titleView.setText(title);
        titleView.setTextColor(Color.WHITE);
        titleView.setTextSize(30);
        titleView.setTypeface(Typeface.DEFAULT_BOLD);
        titleView.setGravity(Gravity.CENTER);
        titleView.setPadding(0, dp(16), 0, dp(8));
        root.addView(titleView, matchWrap());

        TextView bodyView = new TextView(this);
        bodyView.setText(body);
        bodyView.setTextColor(Color.rgb(223, 232, 240));
        bodyView.setTextSize(18);
        bodyView.setGravity(Gravity.CENTER);
        bodyView.setPadding(0, 0, 0, dp(36));
        root.addView(bodyView, matchWrap());

        Button takeButton = actionButton("Take Medicine", Color.rgb(21, 128, 61), Color.WHITE);
        takeButton.setOnClickListener(v -> takeMedicine());
        root.addView(takeButton, buttonLayout());

        Button snoozeButton = actionButton("Snooze 10 min", Color.rgb(37, 99, 235), Color.WHITE);
        snoozeButton.setOnClickListener(v -> snooze());
        root.addView(snoozeButton, buttonLayout());

        Button dismissButton = actionButton("Dismiss", Color.rgb(51, 65, 85), Color.WHITE);
        dismissButton.setOnClickListener(v -> dismissOnly());
        root.addView(dismissButton, buttonLayout());

        setContentView(root);
    }

    private void takeMedicine() {
        Intent current = getIntent();
        stopAlarmAndNotification(current.getStringExtra(EXTRA_REMINDER_ID));

        Intent launchIntent = getPackageManager().getLaunchIntentForPackage(getPackageName());
        if (launchIntent != null) {
            launchIntent.addFlags(Intent.FLAG_ACTIVITY_NEW_TASK | Intent.FLAG_ACTIVITY_CLEAR_TOP);
            launchIntent.putExtra(EXTRA_ALARM_ACTION, ACTION_TAKE_MEDICINE);
            launchIntent.putExtra(EXTRA_REMINDER_ID, current.getStringExtra(EXTRA_REMINDER_ID));
            launchIntent.putExtra(EXTRA_REMINDER_TYPE, current.getStringExtra(EXTRA_REMINDER_TYPE));
            startActivity(launchIntent);
        }

        finish();
    }

    private void snooze() {
        Intent current = getIntent();
        String reminderId = current.getStringExtra(EXTRA_REMINDER_ID);
        stopAlarmAndNotification(reminderId);

        if (reminderId != null) {
            long triggerAtMillis = System.currentTimeMillis() + SNOOZE_DELAY_MILLIS;
            NativeReminderStore.updateSnooze(this, reminderId, triggerAtMillis);
            current.putExtra(EXTRA_ALARM_ACTION, ACTION_SNOOZE);
            current.putExtra(EXTRA_SNOOZE_MINUTES, SNOOZE_MINUTES);
            AlarmSchedulerPlugin.recordAlarmAction(this, current);
            AlarmReceiver.scheduleAlarm(
                this,
                reminderId,
                current.getStringExtra(EXTRA_REMINDER_TYPE),
                current.getStringExtra(EXTRA_TITLE),
                current.getStringExtra(EXTRA_BODY),
                java.time.Instant.ofEpochMilli(triggerAtMillis).toString(),
                triggerAtMillis
            );
            launchAngularAction(current);
        }

        finish();
    }

    private void launchAngularAction(Intent current) {
        Intent launchIntent = getPackageManager().getLaunchIntentForPackage(getPackageName());
        if (launchIntent == null) return;

        launchIntent.addFlags(Intent.FLAG_ACTIVITY_NEW_TASK | Intent.FLAG_ACTIVITY_CLEAR_TOP);
        launchIntent.putExtra(EXTRA_ALARM_ACTION, current.getStringExtra(EXTRA_ALARM_ACTION));
        launchIntent.putExtra(EXTRA_REMINDER_ID, current.getStringExtra(EXTRA_REMINDER_ID));
        launchIntent.putExtra(EXTRA_REMINDER_TYPE, current.getStringExtra(EXTRA_REMINDER_TYPE));
        launchIntent.putExtra(EXTRA_SNOOZE_MINUTES, current.getIntExtra(EXTRA_SNOOZE_MINUTES, 0));
        startActivity(launchIntent);
    }

    private void dismissOnly() {
        stopAlarmAndNotification(getIntent().getStringExtra(EXTRA_REMINDER_ID));
        finish();
    }

    private void stopAlarmAndNotification(String reminderId) {
        stopService(new Intent(this, AlarmService.class));

        NotificationManager notificationManager =
            (NotificationManager) getSystemService(Context.NOTIFICATION_SERVICE);
        if (notificationManager != null && reminderId != null) {
            notificationManager.cancel(AlarmReceiver.stableId(reminderId));
        }
    }

    private Button actionButton(String text, int backgroundColor, int textColor) {
        Button button = new Button(this);
        button.setText(text);
        button.setTextColor(textColor);
        button.setTextSize(18);
        button.setTypeface(Typeface.DEFAULT_BOLD);
        button.setBackgroundColor(backgroundColor);
        button.setAllCaps(false);
        return button;
    }

    private LinearLayout.LayoutParams buttonLayout() {
        LinearLayout.LayoutParams params = new LinearLayout.LayoutParams(
            LinearLayout.LayoutParams.MATCH_PARENT,
            dp(56)
        );
        params.setMargins(0, dp(8), 0, dp(8));
        return params;
    }

    private LinearLayout.LayoutParams matchWrap() {
        return new LinearLayout.LayoutParams(
            LinearLayout.LayoutParams.MATCH_PARENT,
            LinearLayout.LayoutParams.WRAP_CONTENT
        );
    }

    private int dp(int value) {
        return (int) (value * getResources().getDisplayMetrics().density);
    }

    private String valueOrDefault(String value, String fallback) {
        return value == null || value.trim().isEmpty() ? fallback : value;
    }
}
