package com.rafiq.mobile;

import com.getcapacitor.BridgeActivity;
import android.content.Intent;
import android.os.Bundle;

public class MainActivity extends BridgeActivity {

    @Override
    protected void onCreate(Bundle savedInstanceState) {
        // Register the custom Capacitor plugin before the bridge is created.
        registerPlugin(AlarmSchedulerPlugin.class);
        AlarmSchedulerPlugin.recordAlarmAction(this, getIntent());
        super.onCreate(savedInstanceState);
    }

    @Override
    protected void onNewIntent(Intent intent) {
        super.onNewIntent(intent);
        setIntent(intent);
        AlarmSchedulerPlugin.recordAlarmAction(this, intent);
    }
}
