import { CommonModule } from '@angular/common';
import { Component, HostListener, inject, signal } from '@angular/core';
import { Router, RouterOutlet } from '@angular/router';
import { NotificationService } from './Services/notification.service';
import { LocalizationService } from './Services/localization.service';
import { AiChatService } from './Services/ai-chat.service';
import { AiPanel } from './Components/ai-panel/ai-panel';
import { RafiqAssistantComponent } from './Components/rafiq-assistant/rafiq-assistant';
import { TourEngineService } from './core/assistant/services/tour-engine.service';
import { RatingPopup } from './Components/rating-popup/rating-popup';
import { DocumentAnalysisCardComponent } from './Components/document-analysis-card/document-analysis-card';
import { TourGlowRingDirective } from './core/assistant/directives/tour-glow-ring.directive';

@Component({
  selector: 'app-root',
  standalone: true,
  imports: [CommonModule, RouterOutlet, AiPanel, RafiqAssistantComponent, RatingPopup, DocumentAnalysisCardComponent, TourGlowRingDirective],
  templateUrl: './app.html',
  styleUrl: './app.css'
})
export class App {
  readonly notificationService = inject(NotificationService);
  readonly l10n = inject(LocalizationService);
  readonly tourEngine = inject(TourEngineService);
  readonly aiChatService = inject(AiChatService);
  private readonly router = inject(Router);
  readonly title = signal('RafiqAngular');

  // ── FAB drag state ───────────────────────────────
  readonly fabPos = signal({ top: window.innerHeight - 80, left: window.innerWidth - 200 });
  private _fabDragging = false;
  private _fabMoved = false;
  private _fabOffset = { x: 0, y: 0 };

  get showFab(): boolean {
    return this.router.url === '/dashboard' && !this.aiChatService.isPanelOpen();
  }

  onFabPointerDown(e: PointerEvent): void {
    this._fabDragging = true;
    this._fabMoved = false;
    this._fabOffset = { x: e.clientX - this.fabPos().left, y: e.clientY - this.fabPos().top };
    (e.currentTarget as HTMLElement).setPointerCapture(e.pointerId);
    e.preventDefault();
  }

  onFabPointerMove(e: PointerEvent): void {
    if (!this._fabDragging) return;
    this._fabMoved = true;
    const left = Math.max(0, Math.min(e.clientX - this._fabOffset.x, window.innerWidth - 180));
    const top  = Math.max(0, Math.min(e.clientY - this._fabOffset.y, window.innerHeight - 52));
    this.fabPos.set({ top, left });
  }

  onFabPointerUp(): void {
    const wasDrag = this._fabMoved;
    this._fabDragging = false;
    this._fabMoved = false;
    if (!wasDrag) {
      this.aiChatService.openPanel();
    }
  }

  @HostListener('document:keydown.escape')
  onEscape(): void {
    if (this.notificationService.notificationCenterOpen()) {
      this.notificationService.closeNotificationCenter();
    }
  }
}
