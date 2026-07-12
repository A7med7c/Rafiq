import { HttpClient } from '@angular/common/http';
import { Injectable, inject } from '@angular/core';
import { Observable, map, of } from 'rxjs';
import { environment } from '../Environments/Environment';
import { ApiResponse } from '../Modles/api-response';

export interface AccessibleProfileDto {
  userHealthProfileId: string;
  userId: string | null;
  firstName: string;
  lastName: string;
  dateOfBirth: string;
  gender: string;
  bloodType: string | null;
  height: number | null;
  weight: number | null;
  accessRole: string;
  relationship: string | null;
  accessOrigin: string;
  profileType: string;
  isSelf: boolean;
}

export interface PatientProfileDetailDto {
  id: string;
  userId: string | null;
  firstName: string;
  lastName: string;
  dateOfBirth: string;
  gender: string;
  bloodType: string | null;
  height: number | null;
  weight: number | null;
  allergies: { id: string; name: string; severity: string }[];
  chronicDiseases: { id: string; name: string; status: string; diagnosedAt: string | null }[];
  createdAt: string;
  updatedAt: string | null;
}

export interface ReceivedInvitationDto {
  id: string;
  userHealthProfileId: string;
  profileFirstName: string;
  profileLastName: string;
  profileType: string;
  role: string;
  status: string;
  invitedByUserId: string | null;
  inviterFirstName: string | null;
  inviterLastName: string | null;
  inviterEmail: string | null;
  createdAt: string;
}

export interface ProfileMemberDto {
  accessId: string;
  granteeUserId: string;
  firstName: string;
  lastName: string;
  email: string;
  role: string;
  status: string;
  origin: string;
  isCurrentUser: boolean;
  createdAt: string;
}

export interface CreateManagedProfileRequest {
  firstName: string;
  lastName: string;
  dateOfBirth: string;
  gender: string;
  bloodType: string | null;
  height: number | null;
  weight: number | null;
  relationship: string;
  allergies: { name: string; severity: string }[];
  chronicDiseases: { name: string; diagnosedAt: string | null; status: string }[];
}

@Injectable({ providedIn: 'root' })
export class FamilyProfilesService {
  private readonly http = inject(HttpClient);
  private readonly base = `${environment.apiUrl}/patient-profiles`;

  getAccessible(): Observable<AccessibleProfileDto[]> {
    return this.http
      .get<ApiResponse<AccessibleProfileDto[]>>(`${this.base}/accessible`)
      .pipe(map(r => r.data ?? []));
  }

  getById(id: string): Observable<PatientProfileDetailDto> {
    return this.http
      .get<ApiResponse<PatientProfileDetailDto>>(`${this.base}/${id}`)
      .pipe(map(r => r.data));
  }

  createManaged(body: CreateManagedProfileRequest): Observable<PatientProfileDetailDto> {
    return this.http
      .post<ApiResponse<PatientProfileDetailDto>>(`${this.base}/managed`, body)
      .pipe(map(r => r.data));
  }

  sendInvitation(profileId: string, email: string, role: string): Observable<unknown> {
    return this.http.post(`${this.base}/${profileId}/invitations`, { email, role });
  }

  getReceivedInvitations(): Observable<ReceivedInvitationDto[]> {
    return this.http
      .get<ApiResponse<ReceivedInvitationDto[]>>(`${this.base}/invitations/received`)
      .pipe(map(r => r.data ?? []));
  }

  acceptInvitation(id: string): Observable<unknown> {
    return this.http.post(`${this.base}/invitations/${id}/accept`, {});
  }

  rejectInvitation(id: string): Observable<unknown> {
    return this.http.post(`${this.base}/invitations/${id}/reject`, {});
  }

  cancelInvitation(id: string): Observable<unknown> {
    return this.http.post(`${this.base}/invitations/${id}/cancel`, {});
  }

  getProfileMembers(profileId: string): Observable<ProfileMemberDto[]> {
    return this.http
      .get<ApiResponse<ProfileMemberDto[]>>(`${this.base}/${profileId}/members`)
      .pipe(map(r => r.data ?? []));
  }

  changeMemberRole(profileId: string, accessId: string, role: string): Observable<unknown> {
    return this.http.patch(`${this.base}/${profileId}/members/${accessId}/role`, { role });
  }

  revokeMemberAccess(profileId: string, accessId: string): Observable<unknown> {
    return this.http.post(`${this.base}/${profileId}/members/${accessId}/revoke`, {});
  }

  leaveProfile(profileId: string): Observable<unknown> {
    return this.http.post(`${this.base}/${profileId}/leave`, {});
  }
}
