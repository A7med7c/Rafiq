import { HttpClient } from '@angular/common/http';
import { Injectable, inject } from '@angular/core';
import { Observable, map, shareReplay, switchMap } from 'rxjs';
import { environment } from '../Environments/Environment';
import { ApiResponse, ApiResponseBase } from '../Modles/api-response';
import { MedicationReminderLogDto } from '../Modles/medication-reminder.models';
import { UserMedicine } from '../Modles/dashboard.models';
import { HealthProfileService } from './health-profile.service';

@Injectable({ providedIn: 'root' })
export class MedicationRemindersService {
  private readonly http             = inject(HttpClient);
  private readonly healthProfileSvc = inject(HealthProfileService);
  private readonly base             = `${environment.apiUrl}/medication-reminders`;
  private readonly medBase          = `${environment.apiUrl}/user-medicines`;

  private readonly profileId$: Observable<string> =
    this.healthProfileSvc.getMyProfile().pipe(
      map(r => r.data.id),
      shareReplay(1),
    );

  getToday(): Observable<MedicationReminderLogDto[]> {
    return this.profileId$.pipe(
      switchMap(pid =>
        this.http.get<ApiResponse<MedicationReminderLogDto[]>>(
          `${this.base}/today?profileId=${pid}`
        )
      ),
      map(r => r.data ?? []),
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

  getUserMedicines(): Observable<UserMedicine[]> {
    return this.profileId$.pipe(
      switchMap(pid =>
        this.http.get<ApiResponse<UserMedicine[]>>(`${this.medBase}?profileId=${pid}`)
      ),
      map(r => r.data ?? []),
    );
  }
}
