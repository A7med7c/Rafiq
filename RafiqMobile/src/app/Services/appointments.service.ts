import { HttpClient } from '@angular/common/http';
import { Injectable, inject } from '@angular/core';
import { Observable, firstValueFrom, map, of, switchMap } from 'rxjs';
import { environment } from '../Environments/Environment';
import { ApiResponse } from '../Modles/api-response';
import {
  AppointmentDto,
  CreateAppointmentRequest,
  UpdateAppointmentRequest,
  AppointmentType
} from '../Modles/appointment.models';
import { HealthProfileService } from './health-profile.service';
import { ProfileSelectionService } from './profile-selection.service';
import { UpcomingReminderDto } from './medication-reminders.service';
import { LocalizationService } from './localization.service';

@Injectable({ providedIn: 'root' })
export class AppointmentsService {
  private readonly http               = inject(HttpClient);
  private readonly healthProfileSvc   = inject(HealthProfileService);
  private readonly profileSelectSvc   = inject(ProfileSelectionService);
  private readonly localization       = inject(LocalizationService);
  private readonly base               = `${environment.apiUrl}/appointments`;

  public lastHistoryTab: string = 'thisMonth';

  private getCurrentProfileId(): Observable<string> {
    const stored = this.profileSelectSvc.selectedProfileId;
    if (stored) return of(stored);
    return this.healthProfileSvc.getMyProfile().pipe(map(r => r.data.id));
  }

  private parseAppt(dto: AppointmentDto): AppointmentDto {
    if (typeof dto.appointmentType === 'string') {
      const num = Number(dto.appointmentType);
      if (!isNaN(num)) {
        dto.appointmentType = num as AppointmentType;
      } else if (dto.appointmentType in AppointmentType) {
        dto.appointmentType = (AppointmentType as any)[dto.appointmentType];
      } else {
        dto.appointmentType = AppointmentType.Other;
      }
    }
    return dto;
  }

  getAll(overrideProfileId?: string): Observable<AppointmentDto[]> {
    const pid$ = overrideProfileId ? of(overrideProfileId) : this.getCurrentProfileId();
    return pid$.pipe(
      switchMap(pid =>
        this.http.get<ApiResponse<AppointmentDto[]>>(`${this.base}?profileId=${pid}`)
      ),
      map(r => (r.data ?? []).map(d => this.parseAppt(d))),
    );
  }

  getUpcoming(overrideProfileId?: string): Observable<AppointmentDto[]> {
    const pid$ = overrideProfileId ? of(overrideProfileId) : this.getCurrentProfileId();
    return pid$.pipe(
      switchMap(pid =>
        this.http.get<ApiResponse<AppointmentDto[]>>(
          `${this.base}/upcoming?profileId=${pid}`
        )
      ),
      map(r => (r.data ?? []).map(d => this.parseAppt(d))),
    );
  }

  /**
   * GET /api/appointments/upcoming — the backend already returns the unified
   * UpcomingReminderDto[] offline-sync contract for this route (see
   * GetUpcomingAppointmentsQueryHandler): reminderId, reminderType='Appointment',
   * a server-computed scheduledAt (= AppointmentDateTime − ReminderOffsetMinutes),
   * updatedAt, isDeleted, etc. Consume it directly — exactly like
   * MedicationRemindersService.getUpcomingReminders().
   *
   * NOTE: the previous implementation typed this response as AppointmentDto[] and
   * re-derived scheduledAt from appt.appointmentDateTime. That field does not exist
   * on the UpcomingReminderDto this endpoint returns, so `new Date(undefined)` was
   * NaN and every appointment was dropped — which is why no appointment alarm was
   * ever scheduled.
   */
  getUpcomingForSync(profileId: string): Promise<UpcomingReminderDto[]> {
    return firstValueFrom(
      this.http
        .get<ApiResponse<UpcomingReminderDto[]>>(`${this.base}/upcoming?profileId=${profileId}`)
        .pipe(map(r => r.data ?? []))
    );
  }

  create(body: CreateAppointmentRequest, overrideProfileId?: string): Observable<AppointmentDto> {
    const pid$ = overrideProfileId ? of(overrideProfileId) : this.getCurrentProfileId();
    return pid$.pipe(
      switchMap(pid =>
        this.http.post<ApiResponse<AppointmentDto>>(`${this.base}?profileId=${pid}`, body)
      ),
      map(r => this.parseAppt(r.data)),
    );
  }

  update(id: string, body: UpdateAppointmentRequest): Observable<AppointmentDto> {
    return this.http
      .put<ApiResponse<AppointmentDto>>(`${this.base}/${id}`, body)
      .pipe(map(r => this.parseAppt(r.data)));
  }

  delete(id: string): Observable<unknown> {
    return this.http.delete(`${this.base}/${id}`);
  }

  complete(id: string): Observable<AppointmentDto> {
    return this.http
      .patch<ApiResponse<AppointmentDto>>(`${this.base}/${id}/complete`, {})
      .pipe(map(r => this.parseAppt(r.data)));
  }

  cancel(id: string): Observable<AppointmentDto> {
    return this.http
      .patch<ApiResponse<AppointmentDto>>(`${this.base}/${id}/cancel`, {})
      .pipe(map(r => this.parseAppt(r.data)));
  }
}
