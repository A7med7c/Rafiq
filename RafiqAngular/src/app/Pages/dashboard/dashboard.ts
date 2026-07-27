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

@Component({
  selector: 'app-dashboard',
  standalone: true,
  imports: [CommonModule, RouterLink, RouterLinkActive],
  templateUrl: './dashboard.html',
  styleUrl: './dashboard.css',
})
export class Dashboard implements OnInit, OnDestroy {
  private readonly authService        = inject(AuthService);
  protected readonly profileCache     = inject(ProfileCacheService);
  private readonly dashboardService   = inject(DashboardService);
  private readonly apptService        = inject(AppointmentsService);
  protected readonly notifService     = inject(NotificationService);
  protected readonly l10n           = inject(LocalizationService);
  protected readonly aiChatService  = inject(AiChatService);
  protected readonly t              = this.l10n.t;
  private readonly router             = inject(Router);
  private readonly elRef              = inject(ElementRef);
  private readonly medicalReportSvc   = inject(MedicalReportService);

  private readonly dashboardRefreshEffect = effect(() => {
    if (this.notifService.reminderDataRefreshTick() === 0) {
      return;
    }

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
    if (this.notifService.appointmentDataRefreshTick() === 0) {
      return;
    }

    this.loadAppointmentData();
  });

  readonly records          = signal<MedicalRecord[]>([]);
  readonly reminders        = signal<ReminderDisplayItem[]>([]);
  readonly recordsLoading   = signal(true);
  readonly remindersLoading = signal(true);
  readonly sidebarCollapsed  = signal(false);
  readonly mobileSidebarOpen = signal(false);
  readonly dropdownOpen      = signal(false);

  readonly apptLoading     = signal(true);
  readonly allAppointments = signal<AppointmentDto[]>([]);

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
  private _bubbleHideTimer:      ReturnType<typeof setTimeout> | null = null;
  private _inactivityTimer:      ReturnType<typeof setTimeout> | null = null;
  private static readonly BUBBLE_DURATION_MS  = 5_000;   // visible for 5 s
  private static readonly INACTIVITY_DELAY_MS = 3 * 60 * 1000; // 3 min

  // ── Family member AI summary modal ────────────────────────────────────────
  readonly familySummaryOpen      = signal(false);
  readonly familySummaryProfile   = signal<AccessibleProfileDto | null>(null);
  readonly familySummaryLoading   = signal(false);
  readonly familySummaryData      = signal<HealthSummaryDto | null>(null);

  getTruncatedSummary(full: string): string {
    if (this.summaryExpanded() || full.length <= this.SUMMARY_CHAR_LIMIT) return full;
    return full.slice(0, this.SUMMARY_CHAR_LIMIT).trimEnd() + '…';
  }

  isSummaryTruncatable(full: string): boolean {
    return full.length > this.SUMMARY_CHAR_LIMIT;
  }



  readonly familySlots = computed(() => {
    const profiles = this.familyProfiles().slice(0, 4);
    const slots: { type: 'profile' | 'add' | 'empty'; data: AccessibleProfileDto | null }[] = [];
    
    profiles.forEach(p => {
      slots.push({ type: 'profile', data: p });
    });

    if (slots.length < 4) {
      slots.push({ type: 'add', data: null });
    }

    while (slots.length < 4) {
      slots.push({ type: 'empty', data: null });
    }

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

  get hasProfileImage(): boolean { return !!this.authService.currentUser?.profileImageUrl; }

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

  ngOnInit(): void {
    this.profileCache.ensure();
    this.applyResponsiveSidebar();
    this.loadDashboardData();
    // Show greeting bubble 1.5 s after load, then repeat after long inactivity.
    setTimeout(() => this.showRobotBubble(), 1_500);
  }

  ngOnDestroy(): void {
    if (this._bubbleHideTimer)  clearTimeout(this._bubbleHideTimer);
    if (this._inactivityTimer)  clearTimeout(this._inactivityTimer);
    this._reportSub?.unsubscribe();
  }

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

  openVoiceMode(): void {
    this.aiChatService.openPanelInVoiceMode();
  }

  @HostListener('window:resize')
  onWindowResize(): void {
    this.applyResponsiveSidebar();
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

    this.dashboardService.getMedicalRecords().subscribe({
      next: d => { this.records.set(d); this.recordsLoading.set(false); },
      error: () => { this.records.set([]); this.recordsLoading.set(false); },
    });

    this.dashboardService.getMedicinesWithReminders().subscribe({
      next: d => { this.reminders.set(d); this.remindersLoading.set(false); },
      error: () => { this.reminders.set([]); this.remindersLoading.set(false); },
    });

    this.apptService.getAll().pipe(
      catchError(() => of([] as AppointmentDto[]))
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

  toggleSidebar(): void {
    this.sidebarCollapsed.update(v => !v);
  }

  toggleMobileSidebar(): void {
    this.mobileSidebarOpen.update(v => !v);
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

  closeReportDialog(): void { if (!this.reportGenerating()) this.reportDialogOpen.set(false); }

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

  closeFamilySummary(): void { this.familySummaryOpen.set(false); }

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
  if (!relationship) {
    return this.t().family.self;
  }

  const key = relationship.toLowerCase();

  return (this.t().family as any)[key] ?? relationship;
}

getGenderLabel(gender: string | null | undefined): string {
  if (!gender) {
    return '-';
  }

  const key = gender.toLowerCase();

  return (this.t().common as any)[key] ?? gender;
}
}

