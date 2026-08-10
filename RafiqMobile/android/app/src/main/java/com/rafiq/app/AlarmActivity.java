package com.rafiq.mobile;

import android.app.Activity;
import android.app.KeyguardManager;
import android.content.Context;
import android.content.Intent;
import android.graphics.Color;
import android.graphics.Typeface;
import android.graphics.drawable.GradientDrawable;
import android.graphics.drawable.StateListDrawable;
import android.os.Build;
import android.os.Bundle;
import android.view.Gravity;
import android.view.WindowManager;
import android.widget.Button;
import android.widget.LinearLayout;
import android.widget.RelativeLayout;
import android.widget.TextView;

import java.util.Locale;

/**
 * Native full-screen alarm activity shown over the lock screen when a reminder fires.
 * Styled matching Rafiq's design system (Cyan #0EAFD7, Green #16A34A, Dark Glassmorphism Card).
 * Fully supports Arabic & English based on content text or system/app locale.
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

    @Override
    protected void onCreate(Bundle savedInstanceState) {
        enableOverLockScreen();
        super.onCreate(savedInstanceState);
        android.util.Log.i(AlarmDiagnostics.TAG, "AlarmActivity LAUNCHED reminderId="
            + getIntent().getStringExtra(EXTRA_REMINDER_ID)
            + " type=" + getIntent().getStringExtra(EXTRA_REMINDER_TYPE));
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

    private boolean isArabicText(String text) {
        if (text == null) return false;
        for (int i = 0; i < text.length(); i++) {
            char c = text.charAt(i);
            if (c >= 0x0600 && c <= 0x06FF) {
                return true;
            }
        }
        return false;
    }

    private void renderAlarmUi() {
        Intent intent = getIntent();
        String title = valueOrDefault(intent.getStringExtra(EXTRA_TITLE), "Reminder");
        String body = valueOrDefault(intent.getStringExtra(EXTRA_BODY), "Time for your reminder");
        String reminderType = valueOrDefault(intent.getStringExtra(EXTRA_REMINDER_TYPE), "Medication");
        boolean isAppointment = reminderType.equalsIgnoreCase("Appointment");
        boolean isArabic = isArabicText(title) || isArabicText(body) || Locale.getDefault().getLanguage().startsWith("ar");

        String typeLabel;
        String primaryActionLabel;
        String snoozeLabel;
        String dismissLabel;

        if (isArabic) {
            typeLabel = isAppointment ? "تَنْبِيه المَوْعِد" : "تَنْبِيه الدَّواء";
            primaryActionLabel = isAppointment ? "تَأْكِيد الحُضُور" : "أَخَذْتُ الدَّواء";
            snoozeLabel = "تَأْجِيل ١٠ دَقائِق";
            dismissLabel = "إِلْغاء";
        } else {
            typeLabel = isAppointment ? "APPOINTMENT REMINDER" : "MEDICATION REMINDER";
            primaryActionLabel = isAppointment ? "Confirm Attendance" : "Take Medicine";
            snoozeLabel = "Snooze 10 min";
            dismissLabel = "Dismiss";
        }

        // 1. Root Screen Container with Dark Gradient Background
        RelativeLayout root = new RelativeLayout(this);
        root.setPadding(dp(20), dp(36), dp(20), dp(36));

        GradientDrawable bgGradient = new GradientDrawable(
            GradientDrawable.Orientation.TOP_BOTTOM,
            new int[]{Color.rgb(10, 22, 38), Color.rgb(5, 11, 20)}
        );
        root.setBackground(bgGradient);

        // 2. Glassmorphism Card Container
        LinearLayout card = new LinearLayout(this);
        card.setOrientation(LinearLayout.VERTICAL);
        card.setGravity(Gravity.CENTER_HORIZONTAL);
        card.setPadding(dp(24), dp(28), dp(24), dp(24));

        GradientDrawable cardBg = new GradientDrawable();
        cardBg.setShape(GradientDrawable.RECTANGLE);
        cardBg.setColor(Color.rgb(19, 35, 55)); // #132337 dark navy card
        cardBg.setCornerRadius(dp(24));
        cardBg.setStroke(dp(1), Color.rgb(29, 53, 84)); // #1D3554 subtle border
        card.setBackground(cardBg);

        // 3. Circular Icon Badge Header
        TextView iconBadge = new TextView(this);
        iconBadge.setText(isAppointment ? "📅" : "💊");
        iconBadge.setTextSize(30);
        iconBadge.setGravity(Gravity.CENTER);

        GradientDrawable badgeBg = new GradientDrawable();
        badgeBg.setShape(GradientDrawable.OVAL);
        badgeBg.setColor(isAppointment ? Color.rgb(14, 175, 215) : Color.rgb(22, 163, 74));
        badgeBg.setAlpha(45);
        iconBadge.setBackground(badgeBg);

        LinearLayout.LayoutParams badgeParams = new LinearLayout.LayoutParams(dp(68), dp(68));
        badgeParams.gravity = Gravity.CENTER_HORIZONTAL;
        badgeParams.bottomMargin = dp(16);
        card.addView(iconBadge, badgeParams);

        // 4. Type Header Label
        TextView typeView = new TextView(this);
        typeView.setText(typeLabel);
        typeView.setTextColor(Color.rgb(14, 175, 215)); // #0EAFD7 Rafiq Cyan
        typeView.setTextSize(14);
        typeView.setTypeface(getCustomTypeface(true));
        typeView.setGravity(Gravity.CENTER);
        card.addView(typeView, matchWrap());

        // 5. Main Title Text (e.g. "تحليل أو فحص دم")
        TextView titleView = new TextView(this);
        titleView.setText(title);
        titleView.setTextColor(Color.WHITE);
        titleView.setTextSize(24);
        titleView.setTypeface(getCustomTypeface(true));
        titleView.setGravity(Gravity.CENTER);
        titleView.setPadding(0, dp(12), 0, dp(6));
        card.addView(titleView, matchWrap());

        // 6. Body Subtitle (e.g. "Kff — 8/9/2026 6:38 PM")
        TextView bodyView = new TextView(this);
        bodyView.setText(body);
        bodyView.setTextColor(Color.rgb(156, 163, 175)); // #9CA3AF Muted Gray
        bodyView.setTextSize(15);
        bodyView.setTypeface(getCustomTypeface(false));
        bodyView.setGravity(Gravity.CENTER);
        bodyView.setPadding(0, 0, 0, dp(28));
        card.addView(bodyView, matchWrap());

        // 7. Action Buttons
        Button takeButton = createRoundedButton(
            primaryActionLabel,
            Color.rgb(22, 163, 74), // #16A34A Rafiq Green
            Color.rgb(21, 128, 61), // Pressed Green
            Color.WHITE,
            14
        );
        takeButton.setOnClickListener(v -> takeMedicine());
        card.addView(takeButton, buttonLayout(dp(10)));

        Button snoozeButton = createRoundedButton(
            snoozeLabel,
            Color.rgb(14, 175, 215), // #0EAFD7 Rafiq Cyan
            Color.rgb(9, 147, 182),  // Pressed Cyan
            Color.WHITE,
            14
        );
        snoozeButton.setOnClickListener(v -> snooze());
        card.addView(snoozeButton, buttonLayout(dp(10)));

        Button dismissButton = createBorderedButton(
            dismissLabel,
            Color.rgb(30, 41, 59),  // #1E293B Dark Slate
            Color.rgb(51, 65, 85),  // #334155 Border & Pressed
            Color.rgb(226, 232, 240), // #E2E8F0 Text
            14
        );
        dismissButton.setOnClickListener(v -> dismissOnly());
        card.addView(dismissButton, buttonLayout(0));

        RelativeLayout.LayoutParams cardParams = new RelativeLayout.LayoutParams(
            RelativeLayout.LayoutParams.MATCH_PARENT,
            RelativeLayout.LayoutParams.WRAP_CONTENT
        );
        cardParams.addRule(RelativeLayout.CENTER_IN_PARENT);
        root.addView(card, cardParams);

        setContentView(root);
    }

    private void takeMedicine() {
        Intent current = getIntent();
        AlarmActionReceiver.handleTake(
            this,
            current.getStringExtra(EXTRA_REMINDER_ID),
            current.getStringExtra(EXTRA_REMINDER_TYPE));
        finish();
    }

    private void snooze() {
        Intent current = getIntent();
        AlarmActionReceiver.handleSnooze(
            this,
            current.getStringExtra(EXTRA_REMINDER_ID),
            current.getStringExtra(EXTRA_REMINDER_TYPE),
            current.getStringExtra(EXTRA_TITLE),
            current.getStringExtra(EXTRA_BODY),
            current.getStringExtra(EXTRA_SCHEDULED_AT));
        finish();
    }

    private void dismissOnly() {
        AlarmActionReceiver.stopAlarmAndNotification(this, getIntent().getStringExtra(EXTRA_REMINDER_ID));
        finish();
    }

    private Typeface getCustomTypeface(boolean isBold) {
        try {
            String fontPath = isBold ? "public/Amiri-Bold.ttf" : "public/Amiri-Regular.ttf";
            return Typeface.createFromAsset(getAssets(), fontPath);
        } catch (Exception ignored) {
            return Typeface.create("sans-serif-medium", isBold ? Typeface.BOLD : Typeface.NORMAL);
        }
    }

    private Button createRoundedButton(String text, int normalColor, int pressedColor, int textColor, int cornerRadiusDp) {
        Button button = new Button(this);
        button.setText(text);
        button.setTextColor(textColor);
        button.setTextSize(16);
        button.setTypeface(getCustomTypeface(true));
        button.setAllCaps(false);

        GradientDrawable normalBg = new GradientDrawable();
        normalBg.setShape(GradientDrawable.RECTANGLE);
        normalBg.setColor(normalColor);
        normalBg.setCornerRadius(dp(cornerRadiusDp));

        GradientDrawable pressedBg = new GradientDrawable();
        pressedBg.setShape(GradientDrawable.RECTANGLE);
        pressedBg.setColor(pressedColor);
        pressedBg.setCornerRadius(dp(cornerRadiusDp));

        StateListDrawable states = new StateListDrawable();
        states.addState(new int[]{android.R.attr.state_pressed}, pressedBg);
        states.addState(new int[]{}, normalBg);

        button.setBackground(states);
        return button;
    }

    private Button createBorderedButton(String text, int backgroundColor, int strokeColor, int textColor, int cornerRadiusDp) {
        Button button = new Button(this);
        button.setText(text);
        button.setTextColor(textColor);
        button.setTextSize(16);
        button.setTypeface(getCustomTypeface(true));
        button.setAllCaps(false);

        GradientDrawable normalBg = new GradientDrawable();
        normalBg.setShape(GradientDrawable.RECTANGLE);
        normalBg.setColor(backgroundColor);
        normalBg.setCornerRadius(dp(cornerRadiusDp));
        normalBg.setStroke(dp(1), strokeColor);

        GradientDrawable pressedBg = new GradientDrawable();
        pressedBg.setShape(GradientDrawable.RECTANGLE);
        pressedBg.setColor(strokeColor);
        pressedBg.setCornerRadius(dp(cornerRadiusDp));
        pressedBg.setStroke(dp(1), strokeColor);

        StateListDrawable states = new StateListDrawable();
        states.addState(new int[]{android.R.attr.state_pressed}, pressedBg);
        states.addState(new int[]{}, normalBg);

        button.setBackground(states);
        return button;
    }

    private LinearLayout.LayoutParams buttonLayout(int bottomMarginDp) {
        LinearLayout.LayoutParams params = new LinearLayout.LayoutParams(
            LinearLayout.LayoutParams.MATCH_PARENT,
            dp(52)
        );
        params.setMargins(0, 0, 0, dp(bottomMarginDp));
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
