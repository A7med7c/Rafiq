import { CommonModule } from '@angular/common';
import { Component, HostListener, OnInit, OnDestroy, AfterViewInit, ViewChild, inject, signal } from '@angular/core';
import { Router, RouterOutlet } from '@angular/router';
import { App as CapacitorApp } from '@capacitor/app';
import { Capacitor, type PluginListenerHandle } from '@capacitor/core';
import { NotificationService } from './Services/notification.service';
import { LocalizationService } from './Services/localization.service';
import { AiChatService } from './Services/ai-chat.service';
import { AiPanel } from './Components/ai-panel/ai-panel';
import { RafiqAssistantComponent } from './Components/rafiq-assistant/rafiq-assistant';
import { TourEngineService } from './core/assistant/services/tour-engine.service';
import { RatingPopup } from './Components/rating-popup/rating-popup';
import { DocumentAnalysisCardComponent } from './Components/document-analysis-card/document-analysis-card';
import { TourGlowRingDirective } from './core/assistant/directives/tour-glow-ring.directive';
import { AuthService } from './Services/auth-service';
import { ReminderBootstrapService } from './Services/reminder-bootstrap.service';
import { AlarmSchedulerService } from './Services/alarm-scheduler.service';
import { NotificationPermissionDialogComponent } from './Components/notification-permission-dialog/notification-permission-dialog';
import { NotificationPermissionGuardService } from './Services/notification-permission-guard.service';
import { DocumentAnalysisStateService } from './Services/document-analysis-state.service';

@Component({
  selector: 'app-root',
  standalone: true,
  imports: [CommonModule, RouterOutlet, AiPanel, RafiqAssistantComponent, RatingPopup, DocumentAnalysisCardComponent, TourGlowRingDirective, NotificationPermissionDialogComponent],
  templateUrl: './app.html',
  styleUrl: './app.css'
})
export class App implements OnInit, AfterViewInit, OnDestroy {
  readonly notificationService = inject(NotificationService);
  readonly l10n = inject(LocalizationService);
  readonly tourEngine = inject(TourEngineService);
  readonly aiChatService = inject(AiChatService);
  readonly authService = inject(AuthService);
  readonly analysisState = inject(DocumentAnalysisStateService);
  private readonly router = inject(Router);
  private readonly reminderBootstrap = inject(ReminderBootstrapService);
  private readonly alarmScheduler = inject(AlarmSchedulerService);
  private readonly notifPermGuard = inject(NotificationPermissionGuardService);
  
  @ViewChild(NotificationPermissionDialogComponent) permDialog!: NotificationPermissionDialogComponent;

  readonly title = signal('RafiqAngular');

  onCompletionModalReview(): void {
    const modal = this.analysisState.completionModal();
    if (!modal) return;
    this.analysisState.acceptCompletionModal();
    const target = modal.profileId
      ? `/medical-records?profileId=${modal.profileId}`
      : '/medical-records';
    void this.router.navigateByUrl(target);
  }

  onCompletionModalLater(): void {
    this.analysisState.dismissCompletionModal();
  }

  onFailureModalRetry(): void {
    const doc = this.analysisState.failureModal();
    if (!doc) return;
    this.analysisState.retryAnalysis(doc);
  }

  onFailureModalGoToRecords(): void {
    const doc = this.analysisState.failureModal();
    this.analysisState.dismissFailureModal();
    if (!doc) return;
    const target = doc.profileId ? `/medical-records?profileId=${doc.profileId}` : '/medical-records';
    void this.router.navigateByUrl(target);
  }

  onCompletionModalOverlayClick(e: MouseEvent): void {
    if ((e.target as HTMLElement).classList.contains('acm-overlay')) {
      this.onCompletionModalLater();
    }
  }

  onFailureModalOverlayClick(e: MouseEvent): void {
    if ((e.target as HTMLElement).classList.contains('acm-overlay')) {
      this.analysisState.dismissFailureModal();
    }
  }
  /**
   * Handle for the appStateChange listener registered in ngOnInit. Native alarm
   * actions (Take Medicine tapped from AlarmActivity/notification) are recorded to
   * SharedPreferences and only reconciled with the backend by
   * AlarmSchedulerService.consumePendingNativeAction() — ngOnInit alone only runs
   * once per Angular bootstrap, so if the webview is still alive in the background
   * when the user acts on the alarm, that reconciliation would otherwise never fire
   * until the next cold start. Listening for the app returning to the foreground
   * closes that gap without touching the native alarm pipeline itself.
   */
  private appStateListener: PluginListenerHandle | null = null;

  ngOnInit(): void {
    // Bootstrap offline reminders once per app start.
    // ReminderBootstrapService is idempotent and auth-guarded:
    //   • Returns immediately if the user is not logged in.
    //   • Returns immediately on subsequent calls (bootstrapDone flag).
    //   • Concurrent calls share the same in-flight Promise.
    void this.reminderBootstrap.bootstrap();
    // consumePendingNativeAction first: if the user tapped "Take Medicine" from a
    // native alarm while the app was killed, confirm it before checkMissedReminders
    // reads isActionable — otherwise the log would still be actionable and the popup
    // would appear for a dose the user already confirmed.
    void this.alarmScheduler.consumePendingNativeAction()
      .then(() => this.notificationService.checkMissedReminders())
      .catch(e => console.error('[App] startup reminder check failed', e));

    // On Android, check once per session whether the app is excluded from battery
    // optimizations.  If not, open the system exemption dialog immediately so
    // AlarmManager alarms survive the app being killed from Recents on OEM devices
    // (Samsung OneUI, Xiaomi MIUI, etc. cancel alarms for non-exempt apps).
    // Delayed 3 s so it doesn't compete with the splash / login animation.
    if (Capacitor.getPlatform() === 'android') {
      setTimeout(() => {
        void this.alarmScheduler.checkAndRequestBatteryOptimization();
      }, 3000);
    }

    if (Capacitor.isNativePlatform()) {
      void CapacitorApp.addListener('appStateChange', ({ isActive }) => {
        if (!isActive) return;
        // consumePendingNativeAction() already de-dupes concurrent/redundant calls via
        // its own actionInFlight guard, and is a safe no-op when nothing is pending —
        // it resolves false in that case, so a plain app switch never triggers a refresh.
        this.alarmScheduler.consumePendingNativeAction()
          .then(actionConsumed => {
            if (actionConsumed) {
              // Reuse the existing reminder-data refresh signal (already consumed by
              // dashboard.ts / medications.ts) so a dose confirmed while the app sat in
              // the background is reflected immediately if either page is mounted —
              // no new refresh mechanism, no extra API calls beyond what confirm already made.
              this.notificationService.notifyReminderChanged();
            }
            // Always check for reminders that became due while the app was in the
            // background or killed — SignalR does not re-deliver missed events.
            return this.notificationService.checkMissedReminders();
          })
          .catch(e => console.error('[App] resume reminder check failed', e));
      }).then(handle => { this.appStateListener = handle; });
    }
  }

  ngAfterViewInit(): void {
    if (this.permDialog) {
      this.notifPermGuard.registerGlobalDialog(this.permDialog);
    }
  }

  ngOnDestroy(): void {
    void this.appStateListener?.remove();
    this.appStateListener = null;
  }

  private static readonly PUBLIC_ROUTES = ['/', '/login', '/register', '/forgot-password', '/verify-account', '/welcome', '/tour'];

  // ── FAB drag state ───────────────────────────────
  readonly fabPos = signal({ top: window.innerHeight - 80, left: window.innerWidth - 200 });
  private _fabDragging = false;
  private _fabMoved = false;
  private _fabOffset = { x: 0, y: 0 };

  get showFab(): boolean {
    if (!this.authService.isLoggedIn) return false;
    const url = this.router.url;
    if (App.PUBLIC_ROUTES.some(r => url === r || url.startsWith(r + '?'))) return false;
    return !this.aiChatService.isPanelOpen();
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
