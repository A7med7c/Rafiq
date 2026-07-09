import { HttpClient } from '@angular/common/http';
import { Injectable, inject } from '@angular/core';
import { Observable, map, forkJoin, catchError, of } from 'rxjs';
import { environment } from '../Environments/Environment';
import { ApiResponse } from '../Modles/api-response';
import {
  LabReport,
  ImagingReport,
  Prescription,
  UserMedicine,
  MedicalRecord,
} from '../Modles/dashboard.models';

export interface UnifiedMedicalRecord {
  id: string;
  name: string;
  type: 'lab' | 'imaging' | 'prescription' | 'medicine';
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
  private readonly base = environment.apiUrl;

  // Fetch all categories
  getAllData(): Observable<{
    labs: LabReport[];
    imaging: ImagingReport[];
    prescriptions: Prescription[];
    medicines: UserMedicine[];
  }> {
    const labs$ = this.http.get<ApiResponse<LabReport[]>>(`${this.base}/documents/labs`).pipe(
      map(r => r.data ?? []),
      catchError(() => of([] as LabReport[]))
    );
    const imaging$ = this.http.get<ApiResponse<ImagingReport[]>>(`${this.base}/documents/imaging`).pipe(
      map(r => r.data ?? []),
      catchError(() => of([] as ImagingReport[]))
    );
    const prescriptions$ = this.http.get<ApiResponse<Prescription[]>>(`${this.base}/prescriptions`).pipe(
      map(r => r.data ?? []),
      catchError(() => of([] as Prescription[]))
    );
    const medicines$ = this.http.get<ApiResponse<UserMedicine[]>>(`${this.base}/user-medicines`).pipe(
      map(r => r.data ?? []),
      catchError(() => of([] as UserMedicine[]))
    );

    return forkJoin({
      labs: labs$,
      imaging: imaging$,
      prescriptions: prescriptions$,
      medicines: medicines$,
    });
  }

  // Helper to map raw backend models to unified frontend representation
  toUnifiedRecords(data: {
    labs: LabReport[];
    imaging: ImagingReport[];
    prescriptions: Prescription[];
    medicines: UserMedicine[];
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
        hasAiSummary: !!m.notes,
        summary: m.notes || `Dosage: ${m.dosage}. Frequency: ${m.frequency}.`,
        rawRecord: m,
      });
    });

    // Sort newest first
    return records.sort((a, b) => new Date(b.date).getTime() - new Date(a.date).getTime());
  }
}
