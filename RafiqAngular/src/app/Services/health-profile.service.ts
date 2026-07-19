import { HttpClient } from '@angular/common/http';
import { Injectable, inject } from '@angular/core';
import { Observable } from 'rxjs';
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
    );
  }

  /** Fetches the current user's health profile. */
  getMyProfile(): Observable<ApiResponse<PatientProfileResponse>> {
    return this.http.get<ApiResponse<PatientProfileResponse>>(
      `${this.baseUrl}/me`
    );
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
}
