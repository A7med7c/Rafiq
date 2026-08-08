import { HttpClient } from '@angular/common/http';
import { Injectable, inject } from '@angular/core';
import { Observable, of } from 'rxjs';
import { map, catchError, tap } from 'rxjs/operators';
import { environment } from '../Environments/Environment';
import { ApiResponse } from '../Modles/api-response';
import { CreatePatientProfileRequest } from '../Modles/health-profile-request';

/** Shape of the PatientProfileDto returned by the backend */
export interface PatientProfileResponse {
  id: string;
  userId: string;
  dateOfBirth: string;
  gender: string;
  bloodType: string;
  height: number;
  weight: number;
  profileImageUrl: string | null;
  allergies: { id: string; name: string; severity: string }[];
  chronicDiseases: { id: string; name: string; diagnosedAt: string | null; status: string }[];
  createdAt: string;
  updatedAt: string | null;
}

@Injectable({
  providedIn: 'root'
})
export class HealthProfileService {

  private readonly http = inject(HttpClient);
  private readonly baseUrl = `${environment.apiUrl}/patient-profiles`;
  private hasProfileCache: boolean | null = null;

  /**
   * Creates a new patient health profile.
   * Requires the user to be authenticated (JWT is attached automatically
   * by the authInterceptor).
   */
  createProfile(
    request: CreatePatientProfileRequest
  ): Observable<ApiResponse<PatientProfileResponse>> {
    return this.http.post<ApiResponse<PatientProfileResponse>>(
      this.baseUrl,
      request
    ).pipe(
      tap(() => this.setHasProfileCache(true))
    );
  }

  /** Fetches the current user's health profile. */
  getMyProfile(): Observable<ApiResponse<PatientProfileResponse>> {
    return this.http.get<ApiResponse<PatientProfileResponse>>(
      `${this.baseUrl}/me`
    ).pipe(
      tap(res => {
        if (res?.data?.id) {
          this.setHasProfileCache(true);
        }
      })
    );
  }

  /** Checks if the authenticated user has a completed patient health profile. */
  hasProfile(): Observable<boolean> {
    if (this.hasProfileCache !== null) {
      return of(this.hasProfileCache);
    }
    return this.getMyProfile().pipe(
      map(res => {
        const exists = !!res?.data?.id;
        this.hasProfileCache = exists;
        return exists;
      }),
      catchError(err => {
        this.hasProfileCache = false;
        return of(false);
      })
    );
  }

  setHasProfileCache(val: boolean): void {
    this.hasProfileCache = val;
  }

  clearProfileCache(): void {
    this.hasProfileCache = null;
  }

  /** Uploads (or replaces) the profile picture for the given patient profile. */
  uploadProfileImage(profileId: string, file: File): Observable<ApiResponse<PatientProfileResponse>> {
    const formData = new FormData();
    formData.append('profileImage', file, file.name);
    return this.http.post<ApiResponse<PatientProfileResponse>>(
      `${this.baseUrl}/${profileId}/image`,
      formData
    );
  }

  /** Removes the profile picture by posting RemoveImage=true (no file). */
  deleteProfileImage(profileId: string): Observable<ApiResponse<PatientProfileResponse>> {
    const formData = new FormData();
    formData.append('removeImage', 'true');
    return this.http.post<ApiResponse<PatientProfileResponse>>(
      `${this.baseUrl}/${profileId}/image`,
      formData
    );
  }
}
