import { HttpClient } from '@angular/common/http';
import { Injectable, inject } from '@angular/core';
import { Observable } from 'rxjs';
import { map } from 'rxjs/operators';
import { environment } from '../Environments/Environment';
import { ApiResponse } from '../Modles/api-response';

export interface PersistedNotification {
  id: string;
  title: string;
  body: string;
  titleAr?: string;
  bodyAr?: string;
  type: string;
  isRead: boolean;
  readAt: string | null;
  createdAt: string;
}

export interface PersistedNotificationsResult {
  items: PersistedNotification[];
  unreadCount: number;
  page: number;
  pageSize: number;
}

@Injectable({ providedIn: 'root' })
export class PersistedNotificationsService {
  private readonly http = inject(HttpClient);
  private readonly base = `${environment.apiUrl}/notifications`;

  getAll(page = 1, pageSize = 30): Observable<PersistedNotificationsResult> {
    return this.http
      .get<ApiResponse<PersistedNotificationsResult>>(this.base, { params: { page, pageSize } })
      .pipe(map(r => r.data));
  }

  markRead(id: string): Observable<void> {
    return this.http
      .patch<ApiResponse<boolean>>(`${this.base}/${id}/read`, {})
      .pipe(map(() => void 0));
  }

  markAllRead(): Observable<void> {
    return this.http
      .patch<ApiResponse<boolean>>(`${this.base}/read-all`, {})
      .pipe(map(() => void 0));
  }
}
