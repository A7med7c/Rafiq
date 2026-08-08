import { Component, inject, signal } from '@angular/core';
import { CommonModule } from '@angular/common';
import { NotificationPermissionResult, NotificationPermissionService } from '../../Services/notification-permission.service';
import { LocalizationService } from '../../Services/localization.service';

@Component({
  selector: 'app-notification-permission-dialog',
  standalone: true,
  imports: [CommonModule],
  templateUrl: './notification-permission-dialog.html',
  styleUrl: './notification-permission-dialog.css'
})
export class NotificationPermissionDialogComponent {
  private readonly permissionService = inject(NotificationPermissionService);
  readonly l10n = inject(LocalizationService);

  readonly isOpen = signal(false);
  readonly promptType = signal<'soft' | 'denied'>('soft');
  
  private currentResolver: ((result: NotificationPermissionResult) => void) | null = null;

  /**
   * Opens the dialog and returns a promise that resolves when the user makes a choice.
   */
  async open(type: 'soft' | 'denied'): Promise<NotificationPermissionResult> {
    this.promptType.set(type);
    this.isOpen.set(true);

    return new Promise<NotificationPermissionResult>((resolve) => {
      this.currentResolver = resolve;
    });
  }

  async handleAction(action: 'enable' | 'later' | 'settings' | 'cancel') {
    this.isOpen.set(false);
    const resolve = this.currentResolver;
    this.currentResolver = null;

    if (!resolve) return;

    if (action === 'enable') {
      // Actually request permission via OS dialog
      const result = await this.permissionService.requestPermission();
      resolve(result);
    } else if (action === 'settings') {
      await this.permissionService.openSettings();
      // Settings opened, we can't be sure if they enabled it without a resume event.
      // We will resolve as Denied for now, and on resume the app will re-check if needed.
      resolve(NotificationPermissionResult.Denied);
    } else if (action === 'later' || action === 'cancel') {
      resolve(NotificationPermissionResult.Cancelled);
    }
  }
}
