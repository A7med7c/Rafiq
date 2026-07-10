import { HttpClient } from '@angular/common/http';
import { Injectable, inject } from '@angular/core';
import { Observable, map, shareReplay, switchMap } from 'rxjs';
import { environment } from '../Environments/Environment';
import { ApiResponse } from '../Modles/api-response';
import {
  AppointmentDto,
  CreateAppointmentRequest,
  UpdateAppointmentRequest,
} from '../Modles/appointment.models';
import { HealthProfileService } from './health-profile.service';

@Injectable({ providedIn: 'root' })
export class AppointmentsService {
  private readonly http               = inject(HttpClient);
  private readonly healthProfileSvc   = inject(HealthProfileService);
  private readonly base               = `${environment.apiUrl}/appointments`;

  private readonly profileId$: Observable<string> =
    this.healthProfileSvc.getMyProfile().pipe(
      map(r => r.data.id),
      shareReplay(1),
    );

  getAll(): Observable<AppointmentDto[]> {
    return this.profileId$.pipe(
      switchMap(pid =>
        this.http.get<ApiResponse<AppointmentDto[]>>(`${this.base}?profileId=${pid}`)
      ),
      map(r => r.data ?? []),
    );
  }

  getUpcoming(): Observable<AppointmentDto[]> {
    return this.profileId$.pipe(
      switchMap(pid =>
        this.http.get<ApiResponse<AppointmentDto[]>>(
          `${this.base}/upcoming?profileId=${pid}`
        )
      ),
      map(r => r.data ?? []),
    );
  }

  create(body: CreateAppointmentRequest): Observable<AppointmentDto> {
    return this.profileId$.pipe(
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
