import { Component, inject, OnInit, signal, computed, ViewChild } from '@angular/core';
import { CommonModule } from '@angular/common';
import { Router, RouterLink, RouterLinkActive, ActivatedRoute } from '@angular/router';
import { AppointmentsService } from '../../Services/appointments.service';
import { LocalizationService } from '../../Services/localization.service';
import { NotificationService } from '../../Services/notification.service';
import { AiChatService } from '../../Services/ai-chat.service';
import { AppointmentDto, AppointmentStatus, AppointmentType, APPOINTMENT_TYPE_LABELS } from '../../Modles/appointment.models';
import { BottomNav } from '../../shared/bottom-nav/bottom-nav';
import { MobileHeader } from '../../shared/mobile-header/mobile-header';
import { AppointmentsContentComponent } from '../../Components/appointments-content/appointments-content';
import { catchError, of } from 'rxjs';

@Component({
  selector: 'app-appointments',
  standalone: true,
  imports: [CommonModule, RouterLink, BottomNav, MobileHeader, AppointmentsContentComponent],
  templateUrl: './appointments.html',
  styleUrl: './appointments.css',
})
export class Appointments implements OnInit {
  @ViewChild('apptContent') apptContent!: AppointmentsContentComponent;
  private readonly apptSvc = inject(AppointmentsService);
  protected readonly notifSvc = inject(NotificationService);
  protected readonly l10n = inject(LocalizationService);
  protected readonly aiChatService = inject(AiChatService);
  private readonly router = inject(Router);

  protected readonly t = this.l10n.t;

  readonly appointments = signal<AppointmentDto[]>([]);
  readonly loading = signal(true);
  readonly activeHistoryTab = signal<string>('This Month');

  // ── Appointment Details Modal ──
  readonly viewingAppt = signal<AppointmentDto | null>(null);

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
    return this.appointments()
      .filter(a => a.status === AppointmentStatus.Upcoming)
      .sort((a, b) => new Date(a.appointmentDateTime).getTime() - new Date(b.appointmentDateTime).getTime());
  });

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
      { title: 'This Month', appointments: [] },
      { title: 'Last Month', appointments: [] },
      { title: 'Earlier', appointments: [] }
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

  getApptTypeLabel(type: AppointmentType): string {
    return APPOINTMENT_TYPE_LABELS[type] || 'Appointment';
  }

  formatDate(dateString: string): string {
    if (!dateString) return '';
    const d = new Date(dateString);
    return d.toLocaleDateString(undefined, { weekday: 'short', month: 'short', day: 'numeric', year: 'numeric' });
  }

  formatTime(dateString: string): string {
    if (!dateString) return '';
    const d = new Date(dateString);
    return d.toLocaleTimeString(undefined, { hour: '2-digit', minute: '2-digit' });
  }

  reminderLabel(mins: number | null | undefined): string {
    if (mins == null) return 'No reminder';
    if (mins === 0) return 'At time of event';
    if (mins === 15) return '15 minutes before';
    if (mins === 30) return '30 minutes before';
    if (mins === 60) return '1 hour before';
    if (mins === 1440) return '1 day before';
    if (mins === 2880) return '2 days before';
    return `${mins} min before`;
  }

  openView(appt: AppointmentDto): void {
    this.viewingAppt.set(appt);
  }

  closeView(): void {
    this.viewingAppt.set(null);
  }

  markAttended(appt: AppointmentDto): void {
    // In a real app, this would call the API to update the status to Completed
    const updated = this.appointments().map(a =>
      a.id === appt.id ? { ...a, status: AppointmentStatus.Completed } : a
    );
    this.appointments.set(updated);
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
