import { Component, effect, inject, OnInit, signal, computed, HostListener, ElementRef } from '@angular/core';
import { CommonModule } from '@angular/common';
import { Router, RouterLink, RouterLinkActive } from '@angular/router';
import { AuthService } from '../../Services/auth-service';
import { DashboardService } from '../../Services/dashboard.service';
import { AppointmentsService } from '../../Services/appointments.service';
import { NotificationService } from '../../Services/notification.service';
import { MedicalRecord, ReminderDisplayItem } from '../../Modles/dashboard.models';
import { AppointmentDto, AppointmentStatus } from '../../Modles/appointment.models';
import { catchError, of } from 'rxjs';
import { AccessibleProfileDto } from '../../Services/family-profiles.service';
import { HealthSummaryDto } from '../../Services/dashboard.service';

@Component({
  selector: 'app-dashboard',
  standalone: true,
  imports: [CommonModule, RouterLink, RouterLinkActive],
  templateUrl: './dashboard.html',
  styleUrl: './dashboard.css',
})
export class Dashboard implements OnInit {
  private readonly authService      = inject(AuthService);
  private readonly dashboardService = inject(DashboardService);
  private readonly apptService      = inject(AppointmentsService);
  protected readonly notifService     = inject(NotificationService);
  private readonly router           = inject(Router);
  private readonly elRef            = inject(ElementRef);

  private readonly dashboardRefreshEffect = effect(() => {
    if (this.notifService.reminderDataRefreshTick() === 0) {
      return;
    }

    this.loadReminderData();
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

  getTruncatedSummary(full: string): string {
    if (this.summaryExpanded() || full.length <= this.SUMMARY_CHAR_LIMIT) return full;
    return full.slice(0, this.SUMMARY_CHAR_LIMIT).trimEnd() + '…';
  }

  isSummaryTruncatable(full: string): boolean {
    return full.length > this.SUMMARY_CHAR_LIMIT;
  }



  readonly familySlots = computed(() => {
    const profiles = this.familyProfiles().slice(0, 4);
    const placeholderCount = Math.max(0, 4 - profiles.length);
    return [
      ...profiles.map(p => ({ type: 'profile' as const, data: p })),
      ...Array.from({ length: placeholderCount }, () => ({ type: 'add' as const, data: null as AccessibleProfileDto | null })),
    ];
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

  get greeting(): string {
    const h = new Date().getHours();
    if (h < 12) return 'Good morning';
    if (h < 17) return 'Good afternoon';
    return 'Good evening';
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

  goToAddAppointment(): void {
    this.router.navigate(['/appointments'], { queryParams: { openAdd: '1' } });
  }

  formatApptDate(dt: string): string {
    const d    = new Date(dt);
    const now  = new Date();
    const diff = Math.ceil((d.getTime() - now.getTime()) / 86_400_000);
    const time = d.toLocaleTimeString('en-US', { hour: 'numeric', minute: '2-digit', hour12: true });
    if (diff === 0) return `Today, ${time}`;
    if (diff === 1) return `Tomorrow, ${time}`;
    return `${d.toLocaleDateString('en-US', { month: 'short', day: 'numeric' })}, ${time}`;
  }

  formatApptRelative(dt: string): string {
    const diff = Math.ceil((new Date(dt).getTime() - Date.now()) / 86_400_000);
    if (diff <= 0) return 'Today';
    if (diff === 1) return 'In 1 day';
    return `In ${diff} days`;
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
}
