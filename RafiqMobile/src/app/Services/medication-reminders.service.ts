import { HttpClient } from '@angular/common/http';
import { Injectable, inject } from '@angular/core';
import { Observable, firstValueFrom, map, of, shareReplay, switchMap, catchError } from 'rxjs';
import { environment } from '../Environments/Environment';
import { ApiResponse, ApiResponseBase } from '../Modles/api-response';
import { MedicationReminderLogDto } from '../Modles/medication-reminder.models';
import { AddUserMedicinePayload, CreateReminderPayload, MedicineReminder, UpdateReminderPayload, UpdateUserMedicinePayload, UserMedicine } from '../Modles/dashboard.models';
import { HealthProfileService } from './health-profile.service';

/**
 * Shared offline-sync contract. Mirrors the backend UpcomingReminderDto exactly.
 * Exported so OfflineReminderService and ReminderBootstrapService can import it
 * from a single source of truth.
 * Version has been intentionally omitted — UpdatedAt is the sync cursor.
 */
export interface UpcomingReminderDto {
  reminderId: string;                          // backend primary key
  entityId: string;                            // MedicineId or AppointmentId
  reminderType: 'Medication' | 'Appointment';
  title: string;
  body: string;
  scheduledAt: string;                         // ISO-8601 with UTC offset
  status: string;                              // Pending | Sent | Overdue | Missed | Confirmed | Skipped
  updatedAt: string;                           // ISO-8601 — primary sync cursor
  isDeleted: boolean;                          // true when soft-deleted on server
  payload?: unknown;                           // optional extensible metadata
}

export interface AllergyCheckResult {
  isSafe: boolean;
  riskLevel: 'None' | 'Low' | 'Medium' | 'High';
  triggeredAllergy?: string;
  explanation?: string;
}

@Injectable({ providedIn: 'root' })
export class MedicationRemindersService {
  private readonly http             = inject(HttpClient);
  private readonly healthProfileSvc = inject(HealthProfileService);
  private readonly base             = `${environment.apiUrl}/medication-reminders`;
  private readonly medBase          = `${environment.apiUrl}/user-medicines`;
  private readonly remBase          = `${environment.apiUrl}/medicine-reminders`;

  private readonly profileId$: Observable<string> =
    this.healthProfileSvc.getMyProfile().pipe(
      map(r => r.data?.id ?? ''),
      catchError(() => of('')),
      shareReplay(1),
    );

  getToday(profileId?: string): Observable<MedicationReminderLogDto[]> {
    const pid$ = profileId ? of(profileId) : this.profileId$;
    return pid$.pipe(
      switchMap(pid =>
        this.http.get<ApiResponse<MedicationReminderLogDto[]>>(
          `${this.base}/today?profileId=${pid}`
        )
      ),
      map(r => r.data ?? []),
      catchError(() => of([] as MedicationReminderLogDto[]))
    );
  }

  /**
   * GET /api/medication-reminders/upcoming
   * Read-only. Returns today's upcoming reminder occurrences mapped to the
   * shared UpcomingReminderDto contract for offline sync.
   */
  getUpcomingReminders(profileId: string): Promise<UpcomingReminderDto[]> {
    return firstValueFrom(
      this.http
        .get<ApiResponse<UpcomingReminderDto[]>>(`${this.base}/upcoming?profileId=${profileId}`)
        .pipe(map(r => r.data ?? []))
    );
  }

  getById(id: string): Observable<MedicationReminderLogDto> {
    return this.http
      .get<ApiResponse<MedicationReminderLogDto>>(`${this.base}/${id}`)
      .pipe(map(r => r.data));
  }

  confirm(id: string): Observable<ApiResponseBase> {
    return this.http.post<ApiResponseBase>(`${this.base}/${id}/confirm`, {});
  }

  skip(id: string): Observable<ApiResponseBase> {
    return this.http.post<ApiResponseBase>(`${this.base}/${id}/skip`, {});
  }

  snooze(id: string, snoozeMinutes: number): Observable<ApiResponseBase> {
    return this.http.post<ApiResponseBase>(`${this.base}/${id}/snooze`, { snoozeMinutes });
  }

  getHistory(date: string, profileId?: string): Observable<MedicationReminderLogDto[]> {
    const pid$ = profileId ? of(profileId) : this.profileId$;
    return pid$.pipe(
      switchMap(pid =>
        this.http.get<ApiResponse<MedicationReminderLogDto[]>>(
          `${this.base}/history?profileId=${pid}&date=${date}`
        )
      ),
      map(r => r.data ?? []),
      catchError(() => of([] as MedicationReminderLogDto[]))
    );
  }

  getUserMedicines(profileId?: string): Observable<UserMedicine[]> {
    const pid$ = profileId ? of(profileId) : this.profileId$;
    return pid$.pipe(
      switchMap(pid =>
        this.http.get<ApiResponse<UserMedicine[]>>(`${this.medBase}?profileId=${pid}`)
      ),
      map(r => r.data ?? []),
      catchError(() => of([] as UserMedicine[]))
    );
  }

  createReminder(medicineId: string, payload: CreateReminderPayload): Observable<ApiResponseBase> {
    return this.http.post<ApiResponseBase>(`${this.medBase}/${medicineId}/reminders`, payload);
  }

  getRemindersForMedicine(medicineId: string): Observable<MedicineReminder[]> {
    return this.http
      .get<ApiResponse<MedicineReminder[]>>(`${this.medBase}/${medicineId}/reminders`)
      .pipe(map(r => r.data ?? []));
  }

  toggleReminderStatus(reminderId: string): Observable<ApiResponseBase> {
    return this.http.patch<ApiResponseBase>(`${this.remBase}/${reminderId}/toggle-status`, {});
  }

  deleteReminder(reminderId: string): Observable<ApiResponseBase> {
    return this.http.delete<ApiResponseBase>(`${this.remBase}/${reminderId}`);
  }

  updateReminder(id: string, payload: UpdateReminderPayload): Observable<ApiResponseBase> {
    return this.http.put<ApiResponseBase>(`${this.remBase}/${id}`, payload);
  }

  createMedicine(payload: AddUserMedicinePayload, profileId?: string): Observable<ApiResponse<UserMedicine>> {
    const pid$ = profileId ? of(profileId) : this.profileId$;
    return pid$.pipe(
      switchMap(pid =>
        this.http.post<ApiResponse<UserMedicine>>(`${this.medBase}?profileId=${pid}`, payload)
      ),
    );
  }

  updateMedicine(id: string, payload: UpdateUserMedicinePayload): Observable<ApiResponse<UserMedicine>> {
    return this.http.put<ApiResponse<UserMedicine>>(`${this.medBase}/${id}`, payload);
  }

  deleteMedicine(id: string): Observable<ApiResponseBase> {
    return this.http.delete<ApiResponseBase>(`${this.medBase}/${id}`);
  }

  checkMedicationAllergy(medicationName: string, profileId?: string): Observable<ApiResponse<AllergyCheckResult>> {
    const pid$ = profileId ? of(profileId) : this.profileId$;
    return pid$.pipe(
      switchMap(pid =>
        this.http.post<ApiResponse<AllergyCheckResult>>(
          `${this.medBase}/check-allergy`,
          { profileId: pid, medicationName }
        )
      )
    );
  }
}
