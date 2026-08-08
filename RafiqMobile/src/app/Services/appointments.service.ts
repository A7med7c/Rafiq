import { HttpClient } from '@angular/common/http';
import { Injectable, inject } from '@angular/core';
import { Observable, firstValueFrom, map, of, switchMap } from 'rxjs';
import { environment } from '../Environments/Environment';
import { ApiResponse } from '../Modles/api-response';
import {
  AppointmentDto,
  CreateAppointmentRequest,
  UpdateAppointmentRequest,
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

  getAll(overrideProfileId?: string): Observable<AppointmentDto[]> {
    const pid$ = overrideProfileId ? of(overrideProfileId) : this.getCurrentProfileId();
    return pid$.pipe(
      switchMap(pid =>
        this.http.get<ApiResponse<AppointmentDto[]>>(`${this.base}?profileId=${pid}`)
      ),
      map(r => r.data ?? []),
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
      map(r => r.data ?? []),
    );
  }

  /**
   * GET /api/appointments/upcoming — returns the unified UpcomingReminderDto
   * contract for offline sync. Separate from getUpcoming() which returns the
   * domain-specific AppointmentDto used by the UI.
   */
  getUpcomingForSync(profileId: string): Promise<UpcomingReminderDto[]> {
    return firstValueFrom(
      this.http
        .get<ApiResponse<AppointmentDto[]>>(`${this.base}/upcoming?profileId=${profileId}`)
        .pipe(
          map(r => r.data ?? []),
          map(appts => appts.map(appt => {
            const nc = this.localization.t().notifications;
            
            let scheduledAt = appt.appointmentDateTime;
            if (appt.reminderOffsetMinutes) {
              const dt = new Date(appt.appointmentDateTime);
              dt.setMinutes(dt.getMinutes() - appt.reminderOffsetMinutes);
              scheduledAt = dt.toISOString();
            }

            return {
              reminderId: appt.id,
              title: appt.title,
              body: appt.notes || nc.upcomingAppointmentBody.replace('{title}', appt.title).replace('{provider}', appt.provider),
              reminderType: 'Appointment',
              scheduledAt: scheduledAt,
              updatedAt: appt.updatedAt || appt.createdAt,
              isDeleted: appt.status === 'Cancelled'
            } as UpcomingReminderDto;
          }))
        )
    );
  }

  create(body: CreateAppointmentRequest, overrideProfileId?: string): Observable<AppointmentDto> {
    const pid$ = overrideProfileId ? of(overrideProfileId) : this.getCurrentProfileId();
    return pid$.pipe(
      switchMap(pid =>
        this.http.post<ApiResponse<AppointmentDto>>(`${this.base}?profileId=${pid}`, body)
      ),
      map(r => r.data),
    );
  }

  update(id: string, body: UpdateAppointmentRequest): Observable<AppointmentDto> {
    return this.http
      .put<ApiResponse<AppointmentDto>>(`${this.base}/${id}`, body)
      .pipe(map(r => r.data));
  }

  delete(id: string): Observable<unknown> {
    return this.http.delete(`${this.base}/${id}`);
  }

  complete(id: string): Observable<AppointmentDto> {
    return this.http
      .patch<ApiResponse<AppointmentDto>>(`${this.base}/${id}/complete`, {})
      .pipe(map(r => r.data));
  }

  cancel(id: string): Observable<AppointmentDto> {
    return this.http
      .patch<ApiResponse<AppointmentDto>>(`${this.base}/${id}/cancel`, {})
      .pipe(map(r => r.data));
  }
}
