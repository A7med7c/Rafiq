import { HttpClient } from '@angular/common/http';
import { Injectable, inject } from '@angular/core';
import { Observable, map, forkJoin, catchError, of, switchMap } from 'rxjs';
import { environment } from '../Environments/Environment';
import { ApiResponse } from '../Modles/api-response';
import {
  LabReport,
  ImagingReport,
  Prescription,
  MedicalRecord,
  UserMedicine,
  ReminderDisplayItem,
} from '../Modles/dashboard.models';
import { HealthProfileService } from './health-profile.service';
import { ProfileSelectionService } from './profile-selection.service';
import { FamilyProfilesService, AccessibleProfileDto } from './family-profiles.service';
import { LocalizationService } from './localization.service';

export interface AllergyBrief { name: string; severity: string; }
export interface HealthSummaryDto {
  overallStatus: string;       // "Good" | "Stable" | "Needs Attention"
  overallStatusNote: string | null;
  conditions: string[];
  allergies: AllergyBrief[];
  medications: { count: number; hasIssues: boolean; issueNote: string | null };
  labResults: { status: string; abnormalCount: number };
  insights: string[];
  recommendations: string[];
  hasData: boolean;
}

@Injectable({ providedIn: 'root' })
export class DashboardService {
  private readonly http = inject(HttpClient);
  private readonly healthProfileSvc = inject(HealthProfileService);
  private readonly profileSelectSvc = inject(ProfileSelectionService);
  private readonly familyProfilesSvc = inject(FamilyProfilesService);
  private readonly l10n = inject(LocalizationService);
  private readonly base = environment.apiUrl;

  private getCurrentProfileId(): Observable<string> {
    const stored = this.profileSelectSvc.selectedProfileId;
    if (stored) return of(stored);
    return this.healthProfileSvc.getMyProfile().pipe(map(r => r.data.id));
  }

  /** Always resolves the authenticated user's own profile ID, ignoring any family-member selection. */
  private getSelfProfileId(): Observable<string> {
    return this.healthProfileSvc.getMyProfile().pipe(map(r => r.data.id));
  }

  getActiveProfileId(): Observable<string> {
    return this.getCurrentProfileId();
  }

  // ─── Medical Records ──────────────────────────────────────────────────────
  getMedicalRecords(): Observable<MedicalRecord[]> {
    const labs$ = this.http.get<ApiResponse<LabReport[]>>(`${this.base}/documents/labs`).pipe(
      map(r => r.data ?? []), catchError(() => of([] as LabReport[]))
    );
    const imaging$ = this.http.get<ApiResponse<ImagingReport[]>>(`${this.base}/documents/imaging`).pipe(
      map(r => r.data ?? []), catchError(() => of([] as ImagingReport[]))
    );

    return forkJoin([labs$, imaging$]).pipe(
      map(([labs, imaging]) => {
        const labRecs: MedicalRecord[] = labs.map(l => ({
          id: l.id, type: 'lab' as const,
          title: l.labName || 'Lab Report',
          subtitle: l.doctorName ? `Dr. ${l.doctorName}` : undefined,
          date: l.reportDate || new Date(l.createdAt).toLocaleDateString(),
          source: l.labName,
          status: 'Processed', statusColor: 'success' as const,
        }));
        const imgRecs: MedicalRecord[] = imaging.map(im => ({
          id: im.id, type: 'imaging' as const,
          title: `${im.imagingType} — ${im.bodyPart}`,
          subtitle: im.doctorName ? `Dr. ${im.doctorName}` : undefined,
          date: im.reportDate || new Date(im.createdAt).toLocaleDateString(),
          source: im.imagingType,
          status: 'Processed', statusColor: 'success' as const,
        }));

        return [...labRecs, ...imgRecs]
          .sort((a, b) => new Date(b.date).getTime() - new Date(a.date).getTime())
          .slice(0, 5);
      })
    );
  }

  // ─── Family Profiles ──────────────────────────────────────────────────────
  getFamilyProfiles(): Observable<AccessibleProfileDto[]> {
    return this.familyProfilesSvc.getAccessible().pipe(
      catchError(() => of([] as AccessibleProfileDto[]))
    );
  }

  // ─── AI Health Summary ────────────────────────────────────────────────────
  getHealthSummaryForSelf(): Observable<HealthSummaryDto | null> {
    return this.getSelfProfileId().pipe(
      switchMap(profileId => {
        const lang = this.l10n.lang();
        return this.http
          .get<ApiResponse<HealthSummaryDto>>(`${this.base}/chat/health-summary/${profileId}?language=${lang}`)
          .pipe(map(r => r.data ?? null));
      }),
      catchError(() => of(null))
    );
  }

  getHealthSummary(): Observable<HealthSummaryDto | null> {
    return this.getCurrentProfileId().pipe(
      switchMap(profileId => {
        const lang = this.l10n.lang();
        return this.http
          .get<ApiResponse<HealthSummaryDto>>(`${this.base}/chat/health-summary/${profileId}?language=${lang}`)
          .pipe(map(r => r.data ?? null));
      }),
      catchError(() => of(null))
    );
  }

  getHealthSummaryForProfile(profileId: string): Observable<HealthSummaryDto | null> {
    const lang = this.l10n.lang();
    return this.http
      .get<ApiResponse<HealthSummaryDto>>(`${this.base}/chat/health-summary/${profileId}?language=${lang}`)
      .pipe(
        map(r => r.data ?? null),
        catchError(() => of(null))
      );
  }
}
