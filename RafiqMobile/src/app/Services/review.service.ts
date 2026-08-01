import { HttpClient } from '@angular/common/http';
import { Injectable, inject } from '@angular/core';
import { Observable, catchError, of } from 'rxjs';
import { environment } from '../Environments/Environment';
import { ApiResponse } from '../Modles/api-response';

export interface PublicReviewDto {
  displayName: string;
  stars: number;
  comment?: string;
  createdAt: string;
}

@Injectable({ providedIn: 'root' })
export class ReviewService {
  private readonly http = inject(HttpClient);
  private readonly base = `${environment.apiUrl}/reviews`;

  submit(stars: number, comment: string): Observable<ApiResponse<boolean>> {
    return this.http.post<ApiResponse<boolean>>(this.base, { stars, comment });
  }

  getPublic(limit = 20): Observable<ApiResponse<PublicReviewDto[]>> {
    return this.http.get<ApiResponse<PublicReviewDto[]>>(`${this.base}/public`, { params: { limit } }).pipe(
      catchError(() => of({ success: true, data: [], message: '', errors: [] } as unknown as ApiResponse<PublicReviewDto[]>))
    );
  }
}
