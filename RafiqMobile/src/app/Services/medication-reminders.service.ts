import { HttpClient } from '@angular/common/http';
import { Injectable, inject } from '@angular/core';
import { Observable, catchError, map, of, shareReplay, switchMap } from 'rxjs';
import { environment } from '../Environments/Environment';
import { ApiResponse, ApiResponseBase } from '../Modles/api-response';
import { MedicationReminderLogDto } from '../Modles/medication-reminder.models';
import { AddUserMedicinePayload, CreateReminderPayload, MedicineReminder, UpdateReminderPayload, UpdateUserMedicinePayload, UserMedicine } from '../Modles/dashboard.models';
import { HealthProfileService } from './health-profile.service';

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
