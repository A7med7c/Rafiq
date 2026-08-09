import { HttpClient } from '@angular/common/http';
import { Injectable, inject } from '@angular/core';
import { Observable } from 'rxjs';
import { environment } from '../Environments/Environment';

@Injectable({ providedIn: 'root' })
export class MedicalReportService {
  private readonly http = inject(HttpClient);
  private readonly base = environment.apiUrl;

  generateReport(profileId: string): Observable<Blob> {
    return this.http.get(
      `${this.base}/medical-report/${profileId}`,
      { responseType: 'blob' }
    );
  }
}
