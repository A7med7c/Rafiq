import { HttpClient, HttpParams } from '@angular/common/http';
import { Injectable, inject } from '@angular/core';
import { Observable, map } from 'rxjs';
import { environment } from '../../../Environments/Environment';
import { ApiResponse, ApiResponseBase } from '../../../Modles/api-response';
import {
  AdminDashboard,
  AdminReview,
  AdminReviewQuery,
  AdminReviewsPage,
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
  ReviewCategory,
  ReviewOverview,
  ReviewStats,
  ReviewStatus,
  ReviewTrendPoint,
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

  // ── Reviews ──────────────────────────────────────────────────────────────

  private readonly reviewsBase = `${environment.apiUrl}/reviews`;

  getAdminReviews(query: AdminReviewQuery = {}): Observable<AdminReviewsPage> {
    let params = new HttpParams();
    for (const [key, value] of Object.entries(query)) {
      if (value !== undefined && value !== null && value !== '') {
        params = params.set(key, String(value));
      }
    }
    return this.http
      .get<ApiResponse<AdminReviewsPage>>(`${this.reviewsBase}/admin`, { params })
      .pipe(map(r => r.data));
  }

  getReviewOverview(): Observable<ReviewOverview> {
    return this.http
      .get<ApiResponse<ReviewOverview>>(`${this.reviewsBase}/overview`)
      .pipe(map(r => r.data));
  }

  getReviewTrends(months = 6): Observable<ReviewTrendPoint[]> {
    const params = new HttpParams().set('months', months);
    return this.http
      .get<ApiResponse<ReviewTrendPoint[]>>(`${this.reviewsBase}/trends`, { params })
      .pipe(map(r => r.data));
  }

  getReviewStats(): Observable<ReviewStats> {
    return this.http
      .get<ApiResponse<ReviewStats>>(`${this.reviewsBase}/stats`)
      .pipe(map(r => r.data));
  }

  deleteReview(id: string): Observable<ApiResponseBase> {
    return this.http.delete<ApiResponseBase>(`${this.reviewsBase}/${id}`);
  }

  toggleReviewVisibility(id: string, isVisible: boolean): Observable<ApiResponseBase> {
    return this.http.patch<ApiResponseBase>(`${this.reviewsBase}/${id}/visibility`, { isVisible });
  }

  updateReviewStatus(id: string, status: ReviewStatus): Observable<ApiResponseBase> {
    return this.http.patch<ApiResponseBase>(`${this.reviewsBase}/${id}/status`, { status });
  }

  updateReviewCategory(id: string, category: ReviewCategory): Observable<ApiResponseBase> {
    return this.http.patch<ApiResponseBase>(`${this.reviewsBase}/${id}/category`, { category });
  }

  updateAdminNotes(id: string, notes: string | null): Observable<ApiResponseBase> {
    return this.http.patch<ApiResponseBase>(`${this.reviewsBase}/${id}/notes`, { notes });
  }

  replyToReview(id: string, reply: string | null): Observable<ApiResponseBase> {
    return this.http.post<ApiResponseBase>(`${this.reviewsBase}/${id}/reply`, { reply });
  }
}
