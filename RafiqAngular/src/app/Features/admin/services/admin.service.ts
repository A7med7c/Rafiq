import { HttpClient, HttpParams } from '@angular/common/http';
import { Injectable, inject } from '@angular/core';
import { Observable, map } from 'rxjs';
import { environment } from '../../../Environments/Environment';
import { ApiResponse, ApiResponseBase } from '../../../Modles/api-response';
import {
  AdminDashboard,
  AdminUser,
  AdminUserQuery,
  AiFeedbackItem,
  AiFeedbackQuery,
  AiInsights,
  AiOverview,
  AiPerformance,
  AiRequestDetail,
  AiRequestItem,
  AiRequestQuery,
  PagedResult,
  UpdateAiFeedbackRequest
} from '../models/admin.models';

@Injectable({ providedIn: 'root' })
export class AdminService {
  private readonly http = inject(HttpClient);
  private readonly baseUrl = `${environment.apiUrl}/v1/admin`;

  getDashboard(): Observable<AdminDashboard> {
    return this.http
      .get<ApiResponse<AdminDashboard>>(`${this.baseUrl}/dashboard`)
      .pipe(map(response => response.data));
  }

  getUsers(query: AdminUserQuery): Observable<PagedResult<AdminUser>> {
    let params = new HttpParams();

    for (const [key, value] of Object.entries(query)) {
      if (value !== undefined && value !== null && value !== '') {
        params = params.set(key, String(value));
      }
    }

    return this.http
      .get<ApiResponse<PagedResult<AdminUser>>>(`${this.baseUrl}/users`, { params })
      .pipe(map(response => response.data));
  }

  setUserStatus(userId: string, isActive: boolean): Observable<ApiResponseBase> {
    return this.http.patch<ApiResponseBase>(
      `${this.baseUrl}/users/${userId}/status`,
      { isActive }
    );
  }

  // ── AI Operations ────────────────────────────────────────────────────────

  getAiOverview(): Observable<AiOverview> {
    return this.http
      .get<ApiResponse<AiOverview>>(`${this.baseUrl}/ai/overview`)
      .pipe(map(r => r.data));
  }

  getAiRequests(query: AiRequestQuery): Observable<PagedResult<AiRequestItem>> {
    let params = new HttpParams();
    for (const [key, value] of Object.entries(query)) {
      if (value !== undefined && value !== null && value !== '') {
        params = params.set(key, String(value));
      }
    }
    return this.http
      .get<ApiResponse<PagedResult<AiRequestItem>>>(`${this.baseUrl}/ai/requests`, { params })
      .pipe(map(r => r.data));
  }

  getAiRequestDetail(id: string): Observable<AiRequestDetail> {
    return this.http
      .get<ApiResponse<AiRequestDetail>>(`${this.baseUrl}/ai/requests/${id}`)
      .pipe(map(r => r.data));
  }

  getAiFeedback(query: AiFeedbackQuery): Observable<PagedResult<AiFeedbackItem>> {
    let params = new HttpParams();
    for (const [key, value] of Object.entries(query)) {
      if (value !== undefined && value !== null && value !== '') {
        params = params.set(key, String(value));
      }
    }
    return this.http
      .get<ApiResponse<PagedResult<AiFeedbackItem>>>(`${this.baseUrl}/ai/feedback`, { params })
      .pipe(map(r => r.data));
  }

  updateAiFeedback(id: string, body: UpdateAiFeedbackRequest): Observable<ApiResponseBase> {
    return this.http.patch<ApiResponseBase>(`${this.baseUrl}/ai/feedback/${id}`, body);
  }

  getAiPerformance(days = 30): Observable<AiPerformance> {
    return this.http
      .get<ApiResponse<AiPerformance>>(`${this.baseUrl}/ai/performance`, {
        params: new HttpParams().set('days', String(days))
      })
      .pipe(map(r => r.data));
  }

  getAiInsights(): Observable<AiInsights> {
    return this.http
      .get<ApiResponse<AiInsights>>(`${this.baseUrl}/ai/insights`)
      .pipe(map(r => r.data));
  }
}
