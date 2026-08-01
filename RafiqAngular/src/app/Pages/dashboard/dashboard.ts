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

@Component({
  selector: 'app-dashboard',
  standalone: true,
  imports: [CommonModule, RouterLink, RouterLinkActive, AssistantAnchorDirective],
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
  private readonly assistantOrchestrator = inject(AssistantOrchestratorService);

  // ── Reactive effects ─────────────────────────────────────────────────────
  private readonly dashboardRefreshEffect = effect(() => {
    if (this.notifService.reminderDataRefreshTick() === 0) return;
    this.loadReminderData();
  });

  private readonly languageRefreshEffect = effect(() => {
    this.l10n.lang();
    this.summaryLoading.set(true);
    this.dashboardService.getHealthSummary().subscribe({
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
  readonly reminders        = signal<ReminderDisplayItem[]>([]);
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
      if (!r.isEnabled) continue;
      const [h, m]  = (r.reminderTime || '08:00').split(':').map(Number);
      const timeDate = new Date(); timeDate.setHours(h, m, 0, 0);
      items.push({
        time:    timeDate.toLocaleTimeString([], { hour: '2-digit', minute: '2-digit' }),
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

  // ── Computed ─────────────────────────────────────────────────────────────
  readonly familySlots = computed(() => {
    const profiles = this.familyProfiles().slice(0, 4);
    const slots: { type: 'profile' | 'add' | 'empty'; data: AccessibleProfileDto | null }[] = [];

    profiles.forEach(p => slots.push({ type: 'profile', data: p }));
    if (slots.length < 4) slots.push({ type: 'add', data: null });
    while (slots.length < 4) slots.push({ type: 'empty', data: null });

    return slots;
  });

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
    return this.authService.avatarUrl;
  }

  get hasProfileImage(): boolean {
    return !!this.authService.currentUser?.profileImageUrl;
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

    this.dashboardService.getMedicinesWithReminders().subscribe({
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

    this.dashboardService.getHealthSummary().subscribe({
      next: d => { this.healthSummary.set(d); this.summaryLoading.set(false); },
      error: () => { this.healthSummary.set(null); this.summaryLoading.set(false); },
    });
  }

  private loadReminderData(): void {
    this.remindersLoading.set(true);
    this.dashboardService.getMedicinesWithReminders().subscribe({
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
        const url = URL.createObjectURL(blob);
        const a   = document.createElement('a');
        a.href     = url;
        a.download = `${typeLabel}${safeName}.pdf`;
        a.click();
        URL.revokeObjectURL(url);
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
}
