import { HttpClient } from '@angular/common/http';
import { Injectable, inject } from '@angular/core';
import { Observable, map, forkJoin, catchError, of, switchMap } from 'rxjs';
import { environment } from '../Environments/Environment';
import { ApiResponse } from '../Modles/api-response';
import {
  LabReport,
  ImagingReport,
  Prescription,
  UserMedicine,
  MedicalRecord,
  GeneralMedicalDocument,
} from '../Modles/dashboard.models';
import { HealthProfileService } from './health-profile.service';
import { ProfileSelectionService } from './profile-selection.service';

export interface UnifiedMedicalRecord {
  id: string;
  name: string;
  type: 'lab' | 'imaging' | 'prescription' | 'medicine' | 'general';
  typeLabel: string;
  date: string;
  uploadedBy: string;
  hasAiSummary: boolean;
  summary: string;
  rawRecord: any;
}

@Injectable({ providedIn: 'root' })
export class MedicalRecordsService {
  private readonly http = inject(HttpClient);
  private readonly healthProfileSvc = inject(HealthProfileService);
  private readonly profileSelectSvc = inject(ProfileSelectionService);
  private readonly base = environment.apiUrl;

  private getCurrentProfileId(): Observable<string> {
    const stored = this.profileSelectSvc.selectedProfileId;
    if (stored) return of(stored);
    return this.healthProfileSvc.getMyProfile().pipe(map(r => r.data.id));
  }

  // Fetch all categories. Pass overrideProfileId to filter by a specific family profile.
  getAllData(overrideProfileId?: string): Observable<{
    labs: LabReport[];
    imaging: ImagingReport[];
    prescriptions: Prescription[];
    medicines: UserMedicine[];
    generalDocuments: GeneralMedicalDocument[];
  }> {
    const profileId$ = overrideProfileId ? of(overrideProfileId) : this.getCurrentProfileId();

    return profileId$.pipe(
      switchMap(profileId => {
        const pid = `?profileId=${profileId}`;
        const labs$ = this.http.get<ApiResponse<LabReport[]>>(`${this.base}/documents/labs${pid}`).pipe(
          map(r => r.data ?? []),
          catchError(() => of([] as LabReport[]))
        );
        const imaging$ = this.http.get<ApiResponse<ImagingReport[]>>(`${this.base}/documents/imaging${pid}`).pipe(
          map(r => r.data ?? []),
          catchError(() => of([] as ImagingReport[]))
        );
        const prescriptions$ = this.http.get<ApiResponse<Prescription[]>>(`${this.base}/prescriptions${pid}`).pipe(
          map(r => r.data ?? []),
          catchError(() => of([] as Prescription[]))
        );
        const medicines$ = this.http.get<ApiResponse<UserMedicine[]>>(`${this.base}/user-medicines${pid}`).pipe(
          map(r => r.data ?? []),
          catchError(() => of([] as UserMedicine[]))
        );
        const generalDocuments$ = this.http.get<ApiResponse<GeneralMedicalDocument[]>>(`${this.base}/documents/general`).pipe(
          map(r => r.data ?? []),
          catchError(() => of([] as GeneralMedicalDocument[]))
        );

        return forkJoin({
          labs: labs$,
          imaging: imaging$,
          prescriptions: prescriptions$,
          medicines: medicines$,
          generalDocuments: generalDocuments$,
        });
      })
    );
  }

  // Helper to map raw backend models to unified frontend representation
  toUnifiedRecords(data: {
    labs: LabReport[];
    imaging: ImagingReport[];
    prescriptions: Prescription[];
    medicines: UserMedicine[];
    generalDocuments: GeneralMedicalDocument[];
  }): UnifiedMedicalRecord[] {
    const records: UnifiedMedicalRecord[] = [];

    // Map Labs
    data.labs.forEach(l => {
      records.push({
        id: l.id,
        name: l.labName || 'Lab Report',
        type: 'lab',
        typeLabel: 'Lab Analysis',
        date: l.reportDate || new Date(l.createdAt).toLocaleDateString(),
        uploadedBy: l.doctorName || 'Self',
        hasAiSummary: !!(l.summary || l.ocrText),
        summary: l.summary || 'Lab report processed successfully.',
        rawRecord: l,
      });
    });

    // Map Imaging
    data.imaging.forEach(im => {
      records.push({
        id: im.id,
        name: `${im.imagingType} — ${im.bodyPart}`,
        type: 'imaging',
        typeLabel: 'X-Ray & Imaging',
        date: im.reportDate || new Date(im.createdAt).toLocaleDateString(),
        uploadedBy: im.doctorName || 'Self',
        hasAiSummary: !!(im.summary || im.findings),
        summary: im.summary || im.findings || 'Imaging report analyzed.',
        rawRecord: im,
      });
    });

    // Map Prescriptions
    data.prescriptions.forEach(p => {
      records.push({
        id: p.id,
        name: `Prescription — Dr. ${p.doctorName}`,
        type: 'prescription',
        typeLabel: 'Prescription',
        date: p.prescriptionDate || new Date(p.createdAt).toLocaleDateString(),
        uploadedBy: p.patientName || 'Self',
        hasAiSummary: p.medicines && p.medicines.length > 0,
        summary: p.medicines ? p.medicines.map(m => `${m.medicineName} (${m.dosage})`).join(', ') : 'Prescription verified.',
        rawRecord: p,
      });
    });

    // Map Medicines
    data.medicines.forEach(m => {
      records.push({
        id: m.id,
        name: m.medicineName,
        type: 'medicine',
        typeLabel: 'Medicine Box',
        date: new Date(m.createdAt).toLocaleDateString(),
        uploadedBy: m.source || 'Self',
        hasAiSummary: true,
        summary: m.notes || `Dosage: ${m.dosage}. Frequency: ${m.frequency}.`,
        rawRecord: m,
      });
    });

    // Map General Medical Documents
    data.generalDocuments.forEach(d => {
      records.push({
        id: d.id,
        name: d.title || 'Other Medical Document',
        type: 'general',
        typeLabel: 'Other Medical Document',
        date: d.documentDate || (d.createdAt ? new Date(d.createdAt).toLocaleDateString() : ''),
        uploadedBy: d.doctorName || d.hospitalOrClinic || 'Self',
        hasAiSummary: !!d.aiSummary,
        summary: d.aiSummary || d.description || 'Document saved successfully.',
        rawRecord: d,
      });
    });

    // Sort newest first
    return records.sort((a, b) => new Date(b.date).getTime() - new Date(a.date).getTime());
  }

  deleteRecord(record: UnifiedMedicalRecord): Observable<unknown> {
    const url = this.getRecordUrl(record);
    return this.http.delete(url);
  }

  private getRecordUrl(record: UnifiedMedicalRecord): string {
    const paths: Record<UnifiedMedicalRecord['type'], string> = {
      lab: 'documents/labs',
      imaging: 'documents/imaging',
      prescription: 'prescriptions',
      medicine: 'user-medicines',
      general: 'documents/general',
    };
    return `${this.base}/${paths[record.type]}/${record.id}`;
  }
}
