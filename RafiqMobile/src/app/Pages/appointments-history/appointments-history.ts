import { Component, inject, OnInit, signal, computed, ViewChild, effect, untracked } from '@angular/core';
import { CommonModule, Location } from '@angular/common';
import { Router, ActivatedRoute, RouterLink } from '@angular/router';
import { AppointmentsService } from '../../Services/appointments.service';
import { LocalizationService } from '../../Services/localization.service';
import { NotificationService } from '../../Services/notification.service';
import { AppointmentDto, AppointmentStatus, AppointmentType, APPOINTMENT_TYPE_LABELS, APPT_TYPE_KEYS } from '../../Modles/appointment.models';
import { MobileHeader } from '../../shared/mobile-header/mobile-header';
import { AppointmentsContentComponent } from '../../Components/appointments-content/appointments-content';
import { catchError, of } from 'rxjs';

@Component({
  selector: 'app-appointments-history',
  standalone: true,
  imports: [CommonModule, RouterLink, MobileHeader, AppointmentsContentComponent],
  templateUrl: './appointments-history.html',
  styleUrl: './appointments-history.css',
})
export class AppointmentsHistory implements OnInit {
  @ViewChild('apptContent') apptContent!: AppointmentsContentComponent;
  private readonly apptSvc = inject(AppointmentsService);
  protected readonly notifSvc = inject(NotificationService);
  protected readonly l10n = inject(LocalizationService);
  private readonly router = inject(Router);
  private readonly route = inject(ActivatedRoute);
  private readonly location = inject(Location);

  protected readonly t = this.l10n.t;

  readonly appointments = signal<AppointmentDto[]>([]);
  readonly loading = signal(true);
  readonly activeHistoryTab = signal<string>(this.apptSvc.lastHistoryTab);

  // ── Appointment Details Modal ──
  readonly viewingAppt = signal<AppointmentDto | null>(null);

  constructor() {
    effect(() => {
      this.notifSvc.appointmentDataRefreshTick();
      untracked(() => {
        if (this.appointments().length > 0) {
          this.loadAppointments();
        }
      });
    });
  }

  ngOnInit(): void {
    // Read the filter from query params, otherwise default to what's in the service
    this.route.queryParams.subscribe(params => {
      if (params['tab']) {
        this.activeHistoryTab.set(params['tab']);
        this.apptSvc.lastHistoryTab = params['tab'];
      }
    });

    this.loadAppointments();
  }

  setFilter(filter: string) {
    this.activeHistoryTab.set(filter);
    this.apptSvc.lastHistoryTab = filter;
    
    // Update the URL without reloading the page, so back button returns to this page with the right filter
    const url = this.router.createUrlTree([], {
      relativeTo: this.route,
      queryParams: { tab: filter },
      queryParamsHandling: 'merge'
    }).toString();
    this.location.replaceState(url);
  }

  loadAppointments(): void {
    this.loading.set(true);
    this.apptSvc.getAll().pipe(
      catchError(() => of([] as AppointmentDto[]))
    ).subscribe(data => {
      this.appointments.set(data);
      this.loading.set(false);
    });
  }

  readonly previousAppointments = computed(() => {
    return this.appointments()
      .filter(a => a.status !== AppointmentStatus.Upcoming)
      .sort((a, b) => new Date(b.appointmentDateTime).getTime() - new Date(a.appointmentDateTime).getTime());
  });

  readonly groupedPreviousAppointments = computed(() => {
    const previous = this.previousAppointments();
    const today = new Date();
    const currentMonth = today.getMonth();
    const currentYear = today.getFullYear();

    const groups: { title: string, appointments: AppointmentDto[] }[] = [
      { title: 'thisMonth', appointments: [] },
      { title: 'lastMonth', appointments: [] },
      { title: 'earlier', appointments: [] }
    ];

    for (const appt of previous) {
      const d = new Date(appt.appointmentDateTime);
      const m = d.getMonth();
      const y = d.getFullYear();

      if (y === currentYear && m === currentMonth) {
        groups[0].appointments.push(appt);
      } else if ((y === currentYear && m === currentMonth - 1) || (currentMonth === 0 && y === currentYear - 1 && m === 11)) {
        groups[1].appointments.push(appt);
      } else {
        groups[2].appointments.push(appt);
      }
    }

    return groups.filter(g => g.appointments.length > 0);
  });

  readonly activeHistoryAppointments = computed(() => {
    const groups = this.groupedPreviousAppointments();
    const activeGroup = groups.find(g => g.title === this.activeHistoryTab());
    return activeGroup ? activeGroup.appointments : [];
  });

  private get locale(): string {
    return this.l10n.lang() === 'ar' ? 'ar-EG' : 'en-US';
  }

  getApptTypeLabel(type: AppointmentType): string {
    const key = APPT_TYPE_KEYS[type];
    const translated = key ? key.split('.').reduce((obj: any, part) => obj?.[part], this.t()) : undefined;
    return translated || APPOINTMENT_TYPE_LABELS[type] || this.t().appointments.appointmentFallback;
  }

  formatDate(dateString: string): string {
    if (!dateString) return '';
    const d = new Date(dateString);
    return d.toLocaleDateString(this.locale, { weekday: 'short', month: 'short', day: 'numeric', year: 'numeric', numberingSystem: 'latn' });
  }

  formatTime(dateString: string): string {
    if (!dateString) return '';
    const d = new Date(dateString);
    return d.toLocaleTimeString(this.locale, { hour: '2-digit', minute: '2-digit', numberingSystem: 'latn' });
  }

  reminderLabel(mins: number | null | undefined): string {
    const ap = this.t().appointments;
    if (mins == null) return ap.noReminder;
    if (mins === 0) return ap.atTimeOfEvent;
    if (mins === 15) return ap.reminder15Before;
    if (mins === 30) return ap.reminder30Before;
    if (mins === 60) return ap.reminder1hrBefore;
    if (mins === 1440) return ap.reminder1dayBefore;
    if (mins === 2880) return ap.reminder2daysBefore;
    return ap.minBeforeFormat.replace('{mins}', String(mins));
  }

  statusLabel(status: AppointmentStatus | string): string {
    const ap = this.t().appointments;
    const map: Record<string, string> = {
      [AppointmentStatus.Upcoming]: ap.upcomingStatus,
      [AppointmentStatus.Completed]: ap.completedStatus,
      [AppointmentStatus.Cancelled]: ap.cancelledStatus,
      [AppointmentStatus.Missed]: ap.missedStatus,
    };
    return map[status] ?? String(status);
  }

  openView(appt: AppointmentDto): void {
    this.viewingAppt.set(appt);
  }

  closeView(): void {
    this.viewingAppt.set(null);
  }

  markAttended(appt: AppointmentDto): void {
    this.apptSvc.complete(appt.id).subscribe({
      next: (updatedAppt) => {
        const updated = this.appointments().map(a =>
          a.id === appt.id ? updatedAppt : a
        );
        this.appointments.set(updated);
      },
      error: (err) => {
        console.error('Failed to mark appointment as attended', err);
      }
    });
  }

  goBack(): void { this.location.back(); }

  addAppointment(): void {
    if (this.apptContent) {
      this.apptContent.openAdd();
    }
  }
}
