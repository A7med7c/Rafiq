import { HttpClient } from '@angular/common/http';
import { Injectable, inject } from '@angular/core';
import { Observable } from 'rxjs';
import { environment } from '../Environments/Environment';
import { ApiResponse } from '../Modles/api-response';

export interface EmergencyContactResponse {
  id: string;
  userId: string;
  name: string;
  phoneNumber: string;
  relation: string;
}

@Injectable({
  providedIn: 'root'
})
export class EmergencyContactService {

  private readonly http = inject(HttpClient);
  private readonly baseUrl = `${environment.apiUrl}/emergency-contacts`;

  createEmergencyContact(contact: { name: string, phoneNumber: string, relation: string }): Observable<ApiResponse<EmergencyContactResponse>> {
    return this.http.post<ApiResponse<EmergencyContactResponse>>(this.baseUrl, contact);
  }

  getEmergencyContacts(): Observable<ApiResponse<EmergencyContactResponse[]>> {
    return this.http.get<ApiResponse<EmergencyContactResponse[]>>(this.baseUrl);
  }

  deleteEmergencyContact(id: string): Observable<ApiResponse<string>> {
    return this.http.delete<ApiResponse<string>>(`${this.baseUrl}/${id}`);
  }
}
