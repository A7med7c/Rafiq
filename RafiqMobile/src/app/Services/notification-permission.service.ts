import { Injectable } from '@angular/core';
import { LocalNotifications } from '@capacitor/local-notifications';
import { Capacitor } from '@capacitor/core';
import { NativeSettings, AndroidSettings, IOSSettings } from 'capacitor-native-settings';

export enum NotificationPermissionResult {
    Granted,
    Denied,
    PermanentlyDenied,
    Cancelled
}

@Injectable({
    providedIn: 'root'
})
export class NotificationPermissionService {
    private readonly PROMPT_SHOWN_KEY = 'rafiq_notification_prompt_shown';

    /**
     * Checks the current system notification permission state.
     */
    async checkPermission(): Promise<NotificationPermissionResult> {
        if (!Capacitor.isNativePlatform()) {
            return NotificationPermissionResult.Granted;
        }

        try {
            const status = await LocalNotifications.checkPermissions();
            
            if (status.display === 'granted') {
                return NotificationPermissionResult.Granted;
            } else if (status.display === 'denied' || status.display === 'prompt-with-rationale') {
                // On some platforms, 'denied' might just be the default before prompted.
                // We'll treat denied initially as just Denied. We'll differentiate permanence later if needed.
                return NotificationPermissionResult.Denied;
            } else {
                return NotificationPermissionResult.Denied;
            }
        } catch (error) {
            console.error('Error checking notification permission', error);
            return NotificationPermissionResult.Denied;
        }
    }

    /**
     * Directly requests the system notification permission.
     * Should only be called after user intention is captured (e.g. they tapped 'Enable').
     */
    async requestPermission(): Promise<NotificationPermissionResult> {
        if (!Capacitor.isNativePlatform()) {
            return NotificationPermissionResult.Granted;
        }

        try {
            const currentStatus = await this.checkPermission();
            if (currentStatus === NotificationPermissionResult.Granted) {
                return NotificationPermissionResult.Granted;
            }
            
            const requestStatus = await LocalNotifications.requestPermissions();
            
            if (requestStatus.display === 'granted') {
                this.logEvent('permission_granted');
                return NotificationPermissionResult.Granted;
            } else {
                this.logEvent('permission_denied');
                return NotificationPermissionResult.PermanentlyDenied;
            }
        } catch (error) {
            console.error('Error requesting notification permission', error);
            return NotificationPermissionResult.Denied;
        }
    }

    /**
     * Opens the application's native settings screen if permission was permanently denied.
     */
    async openSettings(): Promise<void> {
        if (!Capacitor.isNativePlatform()) {
            return;
        }
        
        try {
            await NativeSettings.open({
              optionAndroid: AndroidSettings.ApplicationDetails,
              optionIOS: IOSSettings.App
            });
            this.logEvent('opened_settings');
        } catch (error) {
            console.error('Error opening settings', error);
        }
    }

    /**
     * Ensures permission is checked, returning the latest state.
     */
    async ensurePermission(): Promise<NotificationPermissionResult> {
        return await this.checkPermission();
    }

    /**
     * Checks if the soft prompt has already been shown to the user on this device.
     */
    hasPromptBeenShown(): boolean {
        return localStorage.getItem(this.PROMPT_SHOWN_KEY) === 'true';
    }

    /**
     * Marks the soft prompt as shown so we do not bother the user again on startup.
     */
    markPromptAsShown(): void {
        localStorage.setItem(this.PROMPT_SHOWN_KEY, 'true');
    }

    /**
     * Optional analytics logging hook.
     */
    private logEvent(action: string): void {
        console.log(`[NotificationPermission] Action: ${action}`);
    }
}
