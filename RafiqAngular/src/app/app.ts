import { CommonModule } from '@angular/common';
import { Component, HostListener, inject, signal } from '@angular/core';
import { RouterOutlet } from '@angular/router';
import { NotificationService } from './Services/notification.service';
import { LocalizationService } from './Services/localization.service';
import { AiPanel } from './Components/ai-panel/ai-panel';
import { RafiqAssistantComponent } from './Components/rafiq-assistant/rafiq-assistant';
import { TourEngineService } from './core/assistant/services/tour-engine.service';

@Component({
  selector: 'app-root',
  standalone: true,
  imports: [CommonModule, RouterOutlet, AiPanel, RafiqAssistantComponent],
  templateUrl: './app.html',
  styleUrl: './app.css'
})
export class App {
  readonly notificationService = inject(NotificationService);
  readonly l10n = inject(LocalizationService);
  readonly tourEngine = inject(TourEngineService);
  readonly title = signal('RafiqAngular');

  @HostListener('document:keydown.escape')
  onEscape(): void {
    if (this.notificationService.notificationCenterOpen()) {
      this.notificationService.closeNotificationCenter();
    }
  }
}
