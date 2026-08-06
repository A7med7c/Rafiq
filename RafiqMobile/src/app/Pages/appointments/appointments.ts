import { Component, inject, OnInit, signal, computed, ViewChild, effect, untracked } from '@angular/core';
import { CommonModule } from '@angular/common';
import { Router, RouterLink, RouterLinkActive, ActivatedRoute } from '@angular/router';
import { AppointmentsService } from '../../Services/appointments.service';
import { LocalizationService } from '../../Services/localization.service';
import { NotificationService } from '../../Services/notification.service';
import { AiChatService } from '../../Services/ai-chat.service';
import { AppointmentDto, AppointmentStatus, AppointmentType, APPOINTMENT_TYPE_LABELS, APPT_TYPE_KEYS } from '../../Modles/appointment.models';
import { BottomNav } from '../../shared/bottom-nav/bottom-nav';
import { MobileHeader } from '../../shared/mobile-header/mobile-header';
import { AppointmentsContentComponent } from '../../Components/appointments-content/appointments-content';
import { catchError, of } from 'rxjs';

import { AssistantAnchorDirective } from '../../core/assistant/directives/assistant-anchor.directive';

import { TourEngineService } from '../../core/assistant/services/tour-engine.service';

const DEMO_TOUR_APPOINTMENT: AppointmentDto = {
  id: 'demo-tour-appt',
  title: 'زيارة طبيب (توضيحي)',
  provider: 'د. سارة أحمد',
  appointmentType: AppointmentType.DoctorVisit,
  appointmentDateTime: new Date(Date.now() + 86400000).toISOString(),
  reminderOffsetMinutes: 30,
  status: AppointmentStatus.Upcoming,
  createdAt: new Date().toISOString(),
};

const DEMO_COMPLETED_APPOINTMENT: AppointmentDto = {
  id: 'demo-completed-appt',
  title: 'فحص دوري (توضيحي)',
  provider: 'د. محمد علي',
  appointmentType: AppointmentType.LabTest,
  appointmentDateTime: new Date(Date.now() - 86400000 * 3).toISOString(),
  reminderOffsetMinutes: undefined,
  status: AppointmentStatus.Completed,
  createdAt: new Date(Date.now() - 86400000 * 5).toISOString(),
};

@Component({
  selector: 'app-appointments',
  standalone: true,
  imports: [CommonModule, RouterLink, AssistantAnchorDirective, BottomNav, MobileHeader, AppointmentsContentComponent],
  templateUrl: './appointments.html',
  styleUrl: './appointments.css',
})
export class Appointments implements OnInit {
  @ViewChild('apptContent') apptContent!: AppointmentsContentComponent;
  private readonly apptSvc = inject(AppointmentsService);
  protected readonly notifSvc = inject(NotificationService);
  protected readonly l10n = inject(LocalizationService);
  protected readonly aiChatService = inject(AiChatService);
  private readonly tourEngine = inject(TourEngineService);
  private readonly router = inject(Router);

  protected readonly t = this.l10n.t;

  readonly appointments = signal<AppointmentDto[]>([]);
  readonly loading = signal(true);
  readonly activeHistoryTab = signal<string>(this.apptSvc.lastHistoryTab);

  // ── Appointment Details Modal ──
  readonly viewingAppt = signal<AppointmentDto | null>(null);

  constructor() {
    effect(() => {
      // Re-fetch when appointment data is refreshed via push notifications / reminder confirmations
      this.notifSvc.appointmentDataRefreshTick();
      
      untracked(() => {
        // We don't want to fetch if we haven't loaded initially (ngOnInit does the first load)
        if (this.appointments().length > 0) {
          this.loadAppointments();
        }
      });
    });

    effect(() => {
      this.apptSvc.lastHistoryTab = this.activeHistoryTab();
    });
  }

  ngOnInit(): void {
    this.loadAppointments();
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

  readonly upcomingAppointments = computed(() => {
    const real = this.appointments()
      .filter(a => a.status === AppointmentStatus.Upcoming)
      .sort((a, b) => new Date(a.appointmentDateTime).getTime() - new Date(b.appointmentDateTime).getTime());

    if (real.length === 0 && this.tourEngine.isPlaying()) {
      return [DEMO_TOUR_APPOINTMENT];
    }

    return real;
  });

  readonly previousAppointments = computed(() => {
    const real = this.appointments()
      .filter(a => a.status !== AppointmentStatus.Upcoming)
      .sort((a, b) => new Date(b.appointmentDateTime).getTime() - new Date(a.appointmentDateTime).getTime());

    if (real.length === 0 && this.tourEngine.isPlaying()) {
      return [DEMO_COMPLETED_APPOINTMENT];
    }

    return real;
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

  readonly activeHistoryAppointmentsPreview = computed(() => {
    return this.activeHistoryAppointments().slice(0, 5);
  });

  readonly hasMoreHistory = computed(() => {
    return this.activeHistoryAppointments().length > 5;
  });

  readonly nextAppointment = computed(() => {
    const upcoming = this.upcomingAppointments();
    return upcoming.length > 0 ? upcoming[0] : null;
  });

  readonly todaysReminder = computed(() => {
    // Return an appointment for today if one exists and has a reminder set
    const today = new Date();
    const upcoming = this.upcomingAppointments();
    return upcoming.find(a => {
      const apptDate = new Date(a.appointmentDateTime);
      return apptDate.getFullYear() === today.getFullYear() &&
        apptDate.getMonth() === today.getMonth() &&
        apptDate.getDate() === today.getDate() &&
        a.reminderOffsetMinutes !== null;
    }) || null;
  });

  readonly upcomingCount = computed(() => this.upcomingAppointments().length);
  readonly completedCount = computed(() => this.appointments().filter(a => a.status === AppointmentStatus.Completed).length);
  readonly missedCount = computed(() => this.appointments().filter(a => a.status === AppointmentStatus.Missed).length);
  readonly cancelledCount = computed(() => this.appointments().filter(a => a.status === AppointmentStatus.Cancelled).length);

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

  // ── Reminder Settings State ──
  readonly masterReminderEnabled = signal(true);
  readonly notificationEnabled = signal(true);
  readonly soundEnabled = signal(true);
  readonly reminderTimeMins = signal<number | null>(30);

  toggleMasterReminder(event: Event): void {
    const input = event.target as HTMLInputElement;
    this.masterReminderEnabled.set(input.checked);
  }

  toggleNotification(): void {
    if (!this.masterReminderEnabled()) return;
    this.notificationEnabled.set(!this.notificationEnabled());
  }

  toggleSound(): void {
    if (!this.masterReminderEnabled()) return;
    this.soundEnabled.set(!this.soundEnabled());
  }

  readonly isCustomTimeActive = signal(false);

  setReminderTime(mins: number | null): void {
    if (!this.masterReminderEnabled()) return;
    this.isCustomTimeActive.set(false);
    this.reminderTimeMins.set(mins);
  }

  enableCustomMode(): void {
    if (!this.masterReminderEnabled()) return;
    this.isCustomTimeActive.set(true);
  }

  updateCustomTime(event: Event): void {
    if (!this.masterReminderEnabled()) return;
    const input = event.target as HTMLInputElement;
    const val = parseInt(input.value, 10);
    if (!isNaN(val) && val > 0) {
      this.reminderTimeMins.set(val);
    }
  }

  addAppointment(): void {
    if (this.apptContent) {
      this.apptContent.openAdd();
    }
  }
}
