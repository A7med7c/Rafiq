import { HttpClient, HttpParams } from '@angular/common/http';
import { Injectable, inject } from '@angular/core';
import { Observable, map } from 'rxjs';
import { environment } from '../../../Environments/Environment';
import { ApiResponse, ApiResponseBase } from '../../../Modles/api-response';
import {
  AdminDashboard,
  AdminUser,
  AdminUserQuery,
  PagedResult
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
}
