import { Injectable, inject } from '@angular/core';
import { NotificationPermissionResult, NotificationPermissionService } from './notification-permission.service';
import { Capacitor } from '@capacitor/core';
import { NotificationPermissionDialogComponent } from '../Components/notification-permission-dialog/notification-permission-dialog';

@Injectable({
    providedIn: 'root'
})
export class NotificationPermissionGuardService {
    private readonly permissionService = inject(NotificationPermissionService);
    private globalDialog: NotificationPermissionDialogComponent | null = null;

    /**
     * Registers the global dialog instance. Usually called from app.ts on app load.
     */
    registerGlobalDialog(dialog: NotificationPermissionDialogComponent): void {
        this.globalDialog = dialog;
    }

    /**
     * Checks if notification permission is granted.
     * If not, shows the dialog and waits for user interaction.
     * Returns true if permission is finally granted, false otherwise.
     */
    async ensurePermission(): Promise<boolean> {
        if (!Capacitor.isNativePlatform()) {
            return true;
        }

        const currentStatus = await this.permissionService.checkPermission();
        if (currentStatus === NotificationPermissionResult.Granted) {
            return true;
        }

        if (!this.globalDialog) {
            console.error('NotificationPermissionGuardService: Global dialog is not registered.');
            return false;
        }

        // Show the dialog and await user interaction
        const result = await this.globalDialog.open('soft');
        
        return result === NotificationPermissionResult.Granted;
    }
}
