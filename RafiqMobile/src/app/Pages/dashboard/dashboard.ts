import { Component, effect, inject, OnInit, OnDestroy, signal, computed, HostListener, ElementRef } from '@angular/core';
import { CommonModule } from '@angular/common';
import { Router, RouterLink, RouterLinkActive } from '@angular/router';
import { AuthService } from '../../Services/auth-service';
import { ProfileCacheService } from '../../Services/profile-cache.service';
import { DashboardService } from '../../Services/dashboard.service';
import { AiChatService } from '../../Services/ai-chat.service';
import { AppointmentsService } from '../../Services/appointments.service';
import { NotificationService } from '../../Services/notification.service';
import { LocalizationService } from '../../Services/localization.service';
import { MedicalRecord, ReminderDisplayItem } from '../../Modles/dashboard.models';
import { AppointmentDto, AppointmentStatus } from '../../Modles/appointment.models';
import { catchError, of, Subscription } from 'rxjs';
import { AccessibleProfileDto } from '../../Services/family-profiles.service';
import { HealthSummaryDto } from '../../Services/dashboard.service';
import { MedicalReportService, ReportType } from '../../Services/medical-report.service';
import { AssistantAnchorDirective } from '../../core/assistant/directives/assistant-anchor.directive';
import { AssistantOrchestratorService } from '../../core/assistant/services/assistant-orchestrator.service';
import { ReviewTrackingService } from '../../Services/review-tracking.service';
import { BottomNav } from '../../shared/bottom-nav/bottom-nav';
import { LanguageSwitcher } from '../../shared/language-switcher/language-switcher';
import { environment } from '../../Environments/Environment';
import { MedicationRemindersService } from '../../Services/medication-reminders.service';
import { MedicationReminderLogDto } from '../../Modles/medication-reminder.models';
import { DownloadService } from '../../Services/download.service';

@Component({
  selector: 'app-dashboard',
  standalone: true,
  imports: [CommonModule, RouterLink, AssistantAnchorDirective, BottomNav, LanguageSwitcher],
  templateUrl: './dashboard.html',
  styleUrl: './dashboard.css',
})
export class Dashboard implements OnInit, OnDestroy {
  private readonly authService        = inject(AuthService);
  protected readonly profileCache     = inject(ProfileCacheService);
  private readonly dashboardService   = inject(DashboardService);
  private readonly apptService        = inject(AppointmentsService);
  protected readonly notifService     = inject(NotificationService);
  protected readonly l10n             = inject(LocalizationService);
  protected readonly aiChatService    = inject(AiChatService);
  protected readonly t                = this.l10n.t;
  private readonly router             = inject(Router);
  private readonly elRef              = inject(ElementRef);
  private readonly medicalReportSvc   = inject(MedicalReportService);
  private readonly downloadSvc        = inject(DownloadService);
  private readonly assistantOrchestrator = inject(AssistantOrchestratorService);
  private readonly reviewTracking      = inject(ReviewTrackingService);
  private readonly medRemindersSvc     = inject(MedicationRemindersService);

  // ── Reactive effects ─────────────────────────────────────────────────────
  private readonly dashboardRefreshEffect = effect(() => {
    if (this.notifService.reminderDataRefreshTick() === 0) return;
    this.loadReminderData();
  });

  private readonly languageRefreshEffect = effect(() => {
    this.l10n.lang();
    this.summaryLoading.set(true);
    this.dashboardService.getHealthSummaryForSelf().subscribe({
      next: d => { this.healthSummary.set(d); this.summaryLoading.set(false); },
      error: () => { this.healthSummary.set(null); this.summaryLoading.set(false); },
    });
  });

  private readonly appointmentRefreshEffect = effect(() => {
    if (this.notifService.appointmentDataRefreshTick() === 0) return;
    this.loadAppointmentData();
  });

  // ── State signals ────────────────────────────────────────────────────────
  readonly records          = signal<MedicalRecord[]>([]);
  readonly activeRecordMenuId = signal<string | null>(null);
  readonly reminders        = signal<MedicationReminderLogDto[]>([]);
  readonly recordsLoading   = signal(true);
  readonly remindersLoading = signal(true);
  readonly sidebarCollapsed  = signal(false);
  readonly mobileSidebarOpen = signal(false);
  readonly dropdownOpen      = signal(false);

  readonly apptLoading     = signal(true);
  readonly allAppointments = signal<AppointmentDto[]>([]);

  // ── System status tracking ────────────────────────────────────────────────
  readonly lastSyncAt    = signal<Date | null>(null);
  readonly hasLoadError  = signal(false);
  private _nowTick       = signal(Date.now());
  private _tickInterval: ReturnType<typeof setInterval> | null = null;

  readonly syncAgo = computed(() => {
    const now = this._nowTick();
    const t   = this.lastSyncAt();
    if (!t) return '—';
    const mins = Math.floor((now - t.getTime()) / 60_000);
    if (mins < 1)  return 'just now';
    if (mins === 1) return '1 min ago';
    if (mins < 60) return `${mins} mins ago`;
    const hrs = Math.floor(mins / 60);
    return hrs === 1 ? '1 hour ago' : `${hrs} hours ago`;
  });

  readonly aiStatus = computed(() => {
    if (this.summaryLoading()) return 'Loading…';
    return this.healthSummary() !== null ? 'Optimal' : 'Unavailable';
  });

  readonly systemOk = computed(() => !this.hasLoadError());

  // ── Today's schedule ──────────────────────────────────────────────────────
  readonly todaySchedule = computed(() => {
    const todayStr = new Date().toDateString();
    const today    = new Date(); today.setHours(0, 0, 0, 0);
    type ScheduleItem = { time: string; sortMs: number; title: string; subtitle: string; type: 'appointment' | 'medication' };
    const items: ScheduleItem[] = [];

    for (const a of this.allAppointments()) {
      if (a.status !== AppointmentStatus.Upcoming) continue;
      const d = new Date(a.appointmentDateTime);
      if (d.toDateString() !== todayStr) continue;
      items.push({
        time:    d.toLocaleTimeString([], { hour: '2-digit', minute: '2-digit' }),
        sortMs:  d.getTime(),
        title:   a.provider || a.title,
        subtitle: a.title,
        type:    'appointment',
      });
    }

    for (const r of this.reminders()) {
      if (r.status === 'Cancelled' || r.status === 'Skipped') continue;
      const [h, m]  = (r.scheduledTime || '08:00').split(':').map(Number);
      const timeDate = new Date(); timeDate.setHours(h, m, 0, 0);
      items.push({
        time:    this.formatTime(r.scheduledTime),
        sortMs:  timeDate.getTime(),
        title:   r.medicineName + (r.dosage ? ` ${r.dosage}` : ''),
        subtitle: this.t().dashboard.medicationReminder,
        type:    'medication',
      });
    }

    return items.sort((a, b) => a.sortMs - b.sortMs);
  });

  readonly familyProfiles  = signal<AccessibleProfileDto[]>([]);
  readonly familyLoading   = signal(true);
  readonly healthSummary   = signal<HealthSummaryDto | null>(null);
  readonly summaryLoading  = signal(true);
  readonly summaryExpanded = signal(false);

  readonly SUMMARY_CHAR_LIMIT = 260;

  // ── Medical Report dialog ─────────────────────────────────────────────────
  readonly profilePickerOpen       = signal(false);
  readonly reportDialogOpen        = signal(false);
  readonly reportCameFromPicker    = signal(false);
  readonly selectedReportType      = signal<ReportType>('DoctorSummary');
  readonly reportGenerating        = signal(false);
  readonly reportTargetProfileId   = signal<string | null>(null);
  readonly reportTargetProfileName = signal<string | null>(null);
  private _reportSub: Subscription | null = null;

  // ── Bottom-sheet swipe-to-dismiss (shared across all dashboard modals) ─────
  readonly sheetDragY = signal(0);
  private sheetDragStartY: number | null = null;
  private static readonly SHEET_DISMISS_THRESHOLD_PX = 90;

  toggleRecordMenu(id: string, event: Event) {
    event.stopPropagation();
    if (this.activeRecordMenuId() === id) {
      this.activeRecordMenuId.set(null);
    } else {
      this.activeRecordMenuId.set(id);
    }
  }

  @HostListener('document:click')
  onDocumentClickCloseRecordMenu() {
    this.activeRecordMenuId.set(null);
  }

  onSheetTouchStart(event: TouchEvent): void {
    this.sheetDragStartY = event.touches[0].clientY;
  }

  onSheetTouchMove(event: TouchEvent): void {
    if (this.sheetDragStartY === null) return;
    const delta = event.touches[0].clientY - this.sheetDragStartY;
    if (delta > 0) this.sheetDragY.set(delta);
  }

  onSheetTouchEnd(closeFn: () => void): void {
    if (this.sheetDragStartY === null) return;
    const dragged = this.sheetDragY();
    this.sheetDragStartY = null;
    this.sheetDragY.set(0);
    if (dragged > Dashboard.SHEET_DISMISS_THRESHOLD_PX) closeFn();
  }

  // ── Robot speech bubble ───────────────────────────────────────────────────
  readonly robotBubbleVisible = signal(false);
  private _bubbleHideTimer:     ReturnType<typeof setTimeout> | null = null;
  private _inactivityTimer:     ReturnType<typeof setTimeout> | null = null;
  private static readonly BUBBLE_DURATION_MS  = 5_000;
  private static readonly INACTIVITY_DELAY_MS = 3 * 60 * 1000;

  // ── Family member AI summary modal ────────────────────────────────────────
  readonly familySummaryOpen    = signal(false);
  readonly familySummaryProfile = signal<AccessibleProfileDto | null>(null);
  readonly familySummaryLoading = signal(false);
  readonly familySummaryData    = signal<HealthSummaryDto | null>(null);

  // ── AI health-ring (derived from qualitative overallStatus; no fabricated numeric score) ──
  readonly healthRingPercent = computed(() => {
    const status = this.healthSummary()?.overallStatus;
    if (status === 'Stable') return 60;
    if (status === 'NeedsAttention') return 32;
    return 90;
  });

  readonly healthRingTone = computed<'good' | 'stable' | 'warn'>(() => {
    const status = this.healthSummary()?.overallStatus;
    if (status === 'Stable') return 'stable';
    if (status === 'NeedsAttention') return 'warn';
    return 'good';
  });

  readonly healthRingIcon = computed(() => {
    const tone = this.healthRingTone();
    return tone === 'good' ? 'fa-face-smile' : tone === 'stable' ? 'fa-face-meh' : 'fa-triangle-exclamation';
  });

  readonly healthRingLabel = computed(() => {
    const s = this.healthSummary();
    if (!s || !s.hasData) return '';
    if (s.overallStatus === 'Stable') return this.t().dashboard.summaryStatusStable;
    if (s.overallStatus === 'NeedsAttention') return this.t().dashboard.summaryStatusNeedsAttention;
    return this.t().dashboard.summaryStatusGood;
  });

  // ── Medications today (soonest first) ───────────────────────────────────────
  readonly todaysReminders = computed(() => {
    return [...this.reminders()]
      .filter(r => r.status !== 'Cancelled' && r.status !== 'Skipped')
      .sort((a, b) => (a.scheduledTime || '').localeCompare(b.scheduledTime || ''))
      .slice(0, 3);
  });

  readonly nextReminder = computed(() => this.todaysReminders()[0] ?? null);

  // ── Computed ─────────────────────────────────────────────────────────────
  readonly familyAvatars = computed(() => this.familyProfiles().slice(0, 4));

  readonly nextAppointment = computed(() => {
    const now = Date.now();
    return this.allAppointments()
      .filter(a => a.status === AppointmentStatus.Upcoming && new Date(a.appointmentDateTime).getTime() > now)
      .sort((a, b) => new Date(a.appointmentDateTime).getTime() - new Date(b.appointmentDateTime).getTime())[0]
      ?? null;
  });

  readonly unreadNotifCount = this.notifService.unreadCount;
  readonly today = new Date();

  // ── Getters ───────────────────────────────────────────────────────────────
  get displayName(): string {
    const u = this.authService.currentUser;
    if (!u) return 'there';
    return u.firstName?.trim() || u.email;
  }

  get userEmail(): string {
    return this.authService.currentUser?.email ?? '';
  }

  get avatarUrl(): string {
    const cachedUrl = this.profileCache.profileImageUrl();
    if (cachedUrl) return `${environment.fileBaseUrl}${cachedUrl}`;
    return this.authService.avatarUrl;
  }

  resolveProfileImageUrl(path: string | null | undefined): string | null {
    if (!path) return null;
    if (path.startsWith('http')) return path;
    return `${environment.fileBaseUrl}${path}`;
  }

  get hasProfileImage(): boolean {
    return !!(this.profileCache.profileImageUrl() || this.authService.currentUser?.profileImageUrl);
  }

  get userInitials(): string {
    const u = this.authService.currentUser;
    if (!u) return '?';
    const f = (u.firstName ?? '')[0] ?? '';
    const l = (u.lastName ?? '')[0] ?? '';
    return (f + l).toUpperCase() || (u.email ?? '?')[0].toUpperCase();
  }

  get avatarBgColor(): string {
    const palette = ['#0EAFD7', '#7C3AED', '#16A34A', '#EA580C', '#0D9488'];
    const seed = this.displayName || this.userEmail || 'U';
    let h = 0;
    for (let i = 0; i < seed.length; i++) h = seed.charCodeAt(i) + ((h << 5) - h);
    return palette[Math.abs(h) % palette.length];
  }

  get greeting(): string {
    const h = new Date().getHours();
    if (h < 12) return this.t().dashboard.goodMorning;
    if (h < 17) return this.t().dashboard.goodAfternoon;
    return this.t().dashboard.goodEvening;
  }

  // ── Lifecycle ─────────────────────────────────────────────────────────────
  ngOnInit(): void {
    this.profileCache.ensure();
    this.applyResponsiveSidebar();
    this.loadDashboardData();
    this._tickInterval = setInterval(() => this._nowTick.set(Date.now()), 60_000);
    // Auto-trigger full welcome tour on first visit after login/onboarding
    setTimeout(() => {
      const tourDone = typeof localStorage !== 'undefined'
        ? localStorage.getItem('rafiq_tour_completed')
        : null;
      if (!tourDone && !this.assistantOrchestrator.tourEngine.isPlaying()) {
        this.startWelcomeTour();
      } else {
        this.showRobotBubble();
      }
    }, 1_800);
  }

  ngOnDestroy(): void {
    if (this._bubbleHideTimer)  clearTimeout(this._bubbleHideTimer);
    if (this._inactivityTimer)  clearTimeout(this._inactivityTimer);
    if (this._tickInterval)     clearInterval(this._tickInterval);
    this._reportSub?.unsubscribe();
  }

  // ── Private helpers ───────────────────────────────────────────────────────
  private showRobotBubble(): void {
    if (this._bubbleHideTimer) clearTimeout(this._bubbleHideTimer);
    this.robotBubbleVisible.set(true);
    this._bubbleHideTimer = setTimeout(() => {
      this.robotBubbleVisible.set(false);
      this.scheduleInactivityBubble();
    }, Dashboard.BUBBLE_DURATION_MS);
  }

  private scheduleInactivityBubble(): void {
    if (this._inactivityTimer) clearTimeout(this._inactivityTimer);
    this._inactivityTimer = setTimeout(
      () => this.showRobotBubble(),
      Dashboard.INACTIVITY_DELAY_MS
    );
  }

  private resetInactivityTimer(): void {
    if (!this._inactivityTimer) return;
    this.scheduleInactivityBubble();
  }

  private applyResponsiveSidebar(): void {
    this.sidebarCollapsed.set(window.innerWidth <= 1024);
    if (window.innerWidth > 768) {
      this.mobileSidebarOpen.set(false);
    }
  }

  private loadDashboardData(): void {
    this.recordsLoading.set(true);
    this.remindersLoading.set(true);
    this.apptLoading.set(true);
    this.familyLoading.set(true);
    this.summaryLoading.set(true);
    this.lastSyncAt.set(new Date());
    this.hasLoadError.set(false);

    this.dashboardService.getMedicalRecords().subscribe({
      next: d => { this.records.set(d); this.recordsLoading.set(false); },
      error: () => { this.records.set([]); this.recordsLoading.set(false); this.hasLoadError.set(true); },
    });

    this.medRemindersSvc.getHistory(this.localToday()).subscribe({
      next: d => { this.reminders.set(d); this.remindersLoading.set(false); },
      error: () => { this.reminders.set([]); this.remindersLoading.set(false); this.hasLoadError.set(true); },
    });

    this.apptService.getAll().pipe(
      catchError(() => { this.hasLoadError.set(true); return of([] as AppointmentDto[]); })
    ).subscribe(data => {
      this.allAppointments.set(data);
      this.apptLoading.set(false);
    });

    this.dashboardService.getFamilyProfiles().subscribe({
      next: d => { this.familyProfiles.set(d); this.familyLoading.set(false); },
      error: () => { this.familyProfiles.set([]); this.familyLoading.set(false); },
    });

    this.dashboardService.getHealthSummaryForSelf().subscribe({
      next: d => { this.healthSummary.set(d); this.summaryLoading.set(false); },
      error: () => { this.healthSummary.set(null); this.summaryLoading.set(false); },
    });
  }

  private loadReminderData(): void {
    this.remindersLoading.set(true);
    this.medRemindersSvc.getHistory(this.localToday()).subscribe({
      next: d => { this.reminders.set(d); this.remindersLoading.set(false); },
      error: () => { this.reminders.set([]); this.remindersLoading.set(false); },
    });
  }

  private loadAppointmentData(): void {
    this.apptLoading.set(true);
    this.apptService.getAll().pipe(
      catchError(() => of([] as AppointmentDto[]))
    ).subscribe(data => {
      this.allAppointments.set(data);
      this.apptLoading.set(false);
    });
  }

  // ── Public methods ────────────────────────────────────────────────────────
  toggleSidebar(): void {
    this.sidebarCollapsed.update(v => !v);
  }

  toggleMobileSidebar(): void {
    this.mobileSidebarOpen.update(v => !v);
  }

  toggleDropdown(): void {
    this.dropdownOpen.update(v => !v);
  }

  logout(): void {
    this.dropdownOpen.set(false);
    this.authService.logout().subscribe();
  }

  openRatingPopup(): void { this.dropdownOpen.set(false); this.reviewTracking.openManually(); }

  goToMyProfile(): void {
    this.dropdownOpen.set(false);
    this.router.navigate(['/my-profile']);
  }

  goToAddAppointment(): void {
    this.router.navigate(['/appointments'], { queryParams: { openAdd: '1' } });
  }

  openVoiceMode(): void {
    this.aiChatService.openPanelInVoiceMode();
  }

  startWelcomeTour(): void {
    if (this.assistantOrchestrator.tourEngine.isPlaying()) return;
    this.assistantOrchestrator.startTour('welcome-tour');
  }

  // ── Host Listeners ────────────────────────────────────────────────────────
  @HostListener('window:resize')
  onWindowResize(): void {
    this.applyResponsiveSidebar();
  }

  @HostListener('document:click', ['$event'])
  onDocumentClick(event: MouseEvent): void {
    const target = event.target as HTMLElement;
    if (!target.closest('.hdr-user')) {
      this.dropdownOpen.set(false);
    }
    this.resetInactivityTimer();
  }

  @HostListener('document:keydown')
  @HostListener('document:touchstart')
  onUserActivity(): void {
    this.resetInactivityTimer();
  }

  // ── Formatting helpers ────────────────────────────────────────────────────
  getTruncatedSummary(full: string): string {
    if (this.summaryExpanded() || full.length <= this.SUMMARY_CHAR_LIMIT) return full;
    return full.slice(0, this.SUMMARY_CHAR_LIMIT).trimEnd() + '…';
  }

  isSummaryTruncatable(full: string): boolean {
    return full.length > this.SUMMARY_CHAR_LIMIT;
  }

  formatApptDate(dt: string): string {
    const d    = new Date(dt);
    const now  = new Date();
    const diff = Math.ceil((d.getTime() - now.getTime()) / 86_400_000);
    const time = d.toLocaleTimeString(this.l10n.lang() === 'ar' ? 'ar-EG' : 'en-US', { hour: 'numeric', minute: '2-digit', hour12: true });
    if (diff === 0) return `${this.t().aiAssistant.today}, ${time}`;
    if (diff === 1) return `${this.t().appointments.nextAppointment}, ${time}`;
    return `${d.toLocaleDateString(this.l10n.lang() === 'ar' ? 'ar-EG' : 'en-US', { month: 'short', day: 'numeric' })}, ${time}`;
  }

  formatApptRelative(dt: string): string {
    const diff = Math.ceil((new Date(dt).getTime() - Date.now()) / 86_400_000);
    if (diff <= 0) return this.t().aiAssistant.today;
    if (diff === 1) return this.l10n.lang() === 'ar' ? 'بكره' : 'Tomorrow';
    return this.l10n.lang() === 'ar' ? `بعد ${diff} أيام` : `In ${diff} days`;
  }

  getRecordIcon(type: string): string {
    switch (type) {
      case 'lab':          return 'fa-flask';
      case 'imaging':      return 'fa-x-ray';
      case 'prescription': return 'fa-prescription-bottle-medical';
      default:             return 'fa-file-medical';
    }
  }

  getRecordIconClass(type: string): string {
    switch (type) {
      case 'lab':          return 'rec-ico-blue';
      case 'imaging':      return 'rec-ico-teal';
      case 'prescription': return 'rec-ico-purple';
      default:             return 'rec-ico-gray';
    }
  }

  getMedIconClass(index: number): string {
    const classes = ['med-ico-orange', 'med-ico-yellow', 'med-ico-pink', 'med-ico-blue', 'med-ico-teal'];
    return classes[index % classes.length];
  }

  getInitial(firstName: string): string {
    return firstName?.charAt(0).toUpperCase() ?? '?';
  }

  getAge(dateOfBirth: string): number {
    const today = new Date();
    const birth = new Date(dateOfBirth);
    let age = today.getFullYear() - birth.getFullYear();
    const m = today.getMonth() - birth.getMonth();
    if (m < 0 || (m === 0 && today.getDate() < birth.getDate())) age--;
    return age;
  }

  // ── Medical Report methods ────────────────────────────────────────────────

  /** Entry-point from the Download button. Shows profile picker when the user has
   *  family members; goes straight to the file-type dialog when only self exists. */
  openDownloadFlow(): void {
    const nonSelf = this.familyProfiles().filter(p => !p.isSelf);
    if (!this.familyLoading() && nonSelf.length > 0) {
      this.profilePickerOpen.set(true);
    } else {
      this.openReportDialog();
    }
  }

  /** Called when the user picks a profile in the picker. */
  selectProfileAndContinue(profileId: string): void {
    this.profilePickerOpen.set(false);
    this.selectedReportType.set('DoctorSummary');
    this.reportTargetProfileId.set(profileId);
    this.reportTargetProfileName.set(this.getProfileDisplayName(profileId));
    this.reportCameFromPicker.set(true);
    this.reportDialogOpen.set(true);
  }

  openReportDialog(profileId?: string): void {
    this.reportCameFromPicker.set(false);
    if (profileId) {
      this.reportTargetProfileId.set(profileId);
      this.reportTargetProfileName.set(this.getProfileDisplayName(profileId));
      this.reportDialogOpen.set(true);
    } else {
      this.dashboardService.getActiveProfileId().subscribe(id => {
        this.reportTargetProfileId.set(id);
        this.reportTargetProfileName.set(this.getProfileDisplayName(id));
        this.reportDialogOpen.set(true);
      });
    }
  }

  backToProfilePicker(): void {
    this.reportDialogOpen.set(false);
    this.profilePickerOpen.set(true);
  }

  closeReportDialog(): void {
    if (!this.reportGenerating()) this.reportDialogOpen.set(false);
  }

  cancelReport(): void {
    this._reportSub?.unsubscribe();
    this._reportSub = null;
    this.reportGenerating.set(false);
    this.reportDialogOpen.set(false);
  }

  getProfileAvatarColor(name: string): string {
    const palette = ['#0EAFD7', '#7C3AED', '#16A34A', '#EA580C', '#0D9488'];
    let h = 0;
    for (let i = 0; i < name.length; i++) h = name.charCodeAt(i) + ((h << 5) - h);
    return palette[Math.abs(h) % palette.length];
  }

  private getProfileDisplayName(profileId: string): string | null {
    const p = this.familyProfiles().find(p => p.userHealthProfileId === profileId);
    return p ? `${p.firstName} ${p.lastName}` : null;
  }

  generateReport(): void {
    const profileId = this.reportTargetProfileId();
    if (!profileId) return;

    this.reportGenerating.set(true);
    this._reportSub = this.medicalReportSvc.generateReport(profileId, this.selectedReportType()).subscribe({
      next: (blob) => {
        const name = this.reportTargetProfileName();
        const safeName = name
          ? '_' + name.trim().replace(/\s+/g, '_').replace(/[^a-zA-Z0-9_؀-ۿ-]/g, '')
          : '';
        const typeLabel = this.selectedReportType() === 'DoctorSummary' ? 'Medical_Summary' : 'Medical_Record';
        this.downloadSvc.download(blob, `${typeLabel}${safeName}.pdf`)
          .catch(err => console.error('Download failed', err));
        this._reportSub = null;
        this.reportGenerating.set(false);
        this.reportDialogOpen.set(false);
      },
      error: () => {
        this._reportSub = null;
        this.reportGenerating.set(false);
      }
    });
  }

  // ── Family summary methods ────────────────────────────────────────────────
  openSelfSummary(): void {
    this.familySummaryProfile.set(null);
    this.familySummaryData.set(this.healthSummary());
    this.familySummaryLoading.set(this.summaryLoading());
    this.familySummaryOpen.set(true);
  }

  openFamilySummary(profile: AccessibleProfileDto): void {
    this.familySummaryProfile.set(profile);
    this.familySummaryData.set(null);
    this.familySummaryOpen.set(true);
    this.familySummaryLoading.set(true);

    this.dashboardService.getHealthSummaryForProfile(profile.userHealthProfileId).subscribe({
      next: (d) => { this.familySummaryData.set(d); this.familySummaryLoading.set(false); },
      error: ()  => { this.familySummaryData.set(null); this.familySummaryLoading.set(false); }
    });
  }

  closeFamilySummary(): void {
    this.familySummaryOpen.set(false);
  }

  summaryText(s: HealthSummaryDto): string {
    const parts: string[] = [`Status: ${s.overallStatus}${s.overallStatusNote ? ' — ' + s.overallStatusNote : ''}`];
    if (s.conditions.length) parts.push(`Conditions: ${s.conditions.join(', ')}`);
    if (s.allergies.length) parts.push(`Allergies: ${s.allergies.map(a => `${a.name} (${a.severity})`).join(', ')}`);
    parts.push(`Medications: ${s.medications.count} active${s.medications.hasIssues && s.medications.issueNote ? ' — ' + s.medications.issueNote : ''}`);
    parts.push(`Lab results: ${s.labResults.status}${s.labResults.abnormalCount > 0 ? ` (${s.labResults.abnormalCount} abnormal)` : ''}`);
    if (s.insights.length) parts.push(`Insights: ${s.insights.join('; ')}`);
    if (s.recommendations.length) parts.push(`Recommendations: ${s.recommendations.join('; ')}`);
    return parts.join('\n');
  }

  getRelationshipLabel(relationship: string | null | undefined): string {
    if (!relationship) return this.t().family.self;
    const key = relationship.toLowerCase();
    return (this.t().family as any)[key] ?? relationship;
  }

  getGenderLabel(gender: string | null | undefined): string {
    if (!gender) return '-';
    const key = gender.toLowerCase();
    return (this.t().common as any)[key] ?? gender;
  }

  getMedStatus(rem: MedicationReminderLogDto): 'taken' | 'upcoming' | 'missed' {
    if (rem.status === 'Confirmed') return 'taken';
    if (rem.status === 'Missed') return 'missed';
    return 'upcoming';
  }

  getMedStatusText(rem: MedicationReminderLogDto): string {
    const isAr = this.l10n.lang() === 'ar';
    if (rem.status === 'Confirmed') return isAr ? 'تم التناول' : 'Taken';
    if (rem.status === 'Missed') return isAr ? 'لم يتم' : 'Missed';
    return isAr ? 'قادم' : 'Upcoming';
  }

  formatTime(timeStr: string): string {
    if (!timeStr) return '';
    const parts = timeStr.split(':');
    if (parts.length < 2) return timeStr;
    const date = new Date();
    date.setHours(Number(parts[0]), Number(parts[1]));
    return date.toLocaleTimeString(this.l10n.lang() === 'ar' ? 'ar-EG' : 'en-US', {
      hour: 'numeric',
      minute: '2-digit',
      hour12: true,
    });
  }

  private localToday(): string {
    const d = new Date();
    const pad = (n: number) => String(n).padStart(2, '0');
    return `${d.getFullYear()}-${pad(d.getMonth() + 1)}-${pad(d.getDate())}`;
  }

  getRecordBadgeType(type: string): string {
    if (type === 'imaging') return 'IMG';
    if (type === 'lab') return 'LAB';
    return 'PDF';
  }

  getRecordBadgeColor(type: string): string {
    if (type === 'imaging') return 'blue';
    if (type === 'lab') return 'teal';
    return 'red';
  }
}
