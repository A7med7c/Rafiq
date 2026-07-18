import { Component, effect, inject, OnInit, signal, computed, HostListener, ElementRef } from '@angular/core';
import { CommonModule } from '@angular/common';
import { Router, RouterLink, RouterLinkActive } from '@angular/router';
import { AuthService } from '../../Services/auth-service';
import { DashboardService } from '../../Services/dashboard.service';
import { AppointmentsService } from '../../Services/appointments.service';
import { NotificationService } from '../../Services/notification.service';
import { LocalizationService } from '../../Services/localization.service';
import { MedicalRecord, ReminderDisplayItem } from '../../Modles/dashboard.models';
import { AppointmentDto, AppointmentStatus } from '../../Modles/appointment.models';
import { catchError, of } from 'rxjs';
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
export class Dashboard implements OnInit {
  private readonly authService        = inject(AuthService);
  private readonly dashboardService   = inject(DashboardService);
  private readonly apptService        = inject(AppointmentsService);
  protected readonly notifService     = inject(NotificationService);
  protected readonly l10n           = inject(LocalizationService);
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
  readonly reportDialogOpen      = signal(false);
  readonly selectedReportType    = signal<ReportType>('DoctorSummary');
  readonly reportGenerating      = signal(false);
  readonly reportTargetProfileId = signal<string | null>(null);

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

  get greeting(): string {
    const h = new Date().getHours();
    if (h < 12) return this.t().dashboard.goodMorning;
    if (h < 17) return this.t().dashboard.goodAfternoon;
    return this.t().dashboard.goodEvening;
  }

  ngOnInit(): void {
    this.applyResponsiveSidebar();
    this.loadDashboardData();
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
  openReportDialog(profileId?: string): void {
    if (profileId) {
      this.reportTargetProfileId.set(profileId);
      this.reportDialogOpen.set(true);
    } else {
      this.dashboardService.getActiveProfileId().subscribe(id => {
        this.reportTargetProfileId.set(id);
        this.reportDialogOpen.set(true);
      });
    }
  }

  closeReportDialog(): void { if (!this.reportGenerating()) this.reportDialogOpen.set(false); }

  generateReport(): void {
    const profileId = this.reportTargetProfileId();
    if (!profileId) return;

    this.reportGenerating.set(true);
    this.medicalReportSvc.generateReport(profileId, this.selectedReportType()).subscribe({
      next: (blob) => {
        const url = URL.createObjectURL(blob);
        const a   = document.createElement('a');
        a.href    = url;
        a.download = `RafiqMedicalReport_${this.selectedReportType()}_${new Date().toISOString().slice(0, 10)}.pdf`;
        a.click();
        URL.revokeObjectURL(url);
        this.reportGenerating.set(false);
        this.reportDialogOpen.set(false);
      },
      error: () => { this.reportGenerating.set(false); }
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

