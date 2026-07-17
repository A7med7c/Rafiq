import { CommonModule } from '@angular/common';
import { Component, HostListener, inject, signal } from '@angular/core';
import { RouterOutlet } from '@angular/router';
import { NotificationService } from './Services/notification.service';
import { LocalizationService } from './Services/localization.service';

@Component({
  selector: 'app-root',
  standalone: true,
  imports: [CommonModule, RouterOutlet],
  templateUrl: './app.html',
  styleUrl: './app.css'
})
export class App {
  readonly notificationService = inject(NotificationService);
  readonly l10n = inject(LocalizationService);
  readonly title = signal('RafiqAngular');

  @HostListener('document:keydown.escape')
  onEscape(): void {
    if (this.notificationService.notificationCenterOpen()) {
      this.notificationService.closeNotificationCenter();
    }
  }
}
