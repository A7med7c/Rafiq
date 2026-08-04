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

  private _cachedSummary: HealthSummaryDto | null = null;
  private _summaryProfileId: string | null = null;
  private _summaryLanguage: string | null = null;

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
    return this.getCurrentProfileId().pipe(
      switchMap(profileId => {
        const pid = `?profileId=${profileId}`;
        const labs$ = this.http.get<ApiResponse<LabReport[]>>(`${this.base}/documents/labs${pid}`).pipe(
          map(r => r.data ?? []), catchError(() => of([] as LabReport[]))
        );
        const imaging$ = this.http.get<ApiResponse<ImagingReport[]>>(`${this.base}/documents/imaging${pid}`).pipe(
          map(r => r.data ?? []), catchError(() => of([] as ImagingReport[]))
        );

        return forkJoin([labs$, imaging$]);
      }),
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

  // ─── Medicines ────────────────────────────────────────────────────────────
  getMedicinesWithReminders(): Observable<ReminderDisplayItem[]> {
    return this.fetchMedicines().pipe(
      map(medicines => this.toDisplayItems(medicines)),
      catchError(() => of([] as ReminderDisplayItem[]))
    );
  }

  getMedicinesForSelf(): Observable<ReminderDisplayItem[]> {
    return this.getSelfProfileId().pipe(
      switchMap(profileId =>
        this.http.get<ApiResponse<UserMedicine[]>>(`${this.base}/user-medicines?profileId=${profileId}`)
      ),
      map(r => this.toDisplayItems(r.data ?? [])),
      catchError(() => of([] as ReminderDisplayItem[]))
    );
  }

  private fetchMedicines(): Observable<UserMedicine[]> {
    return this.getCurrentProfileId().pipe(
      switchMap(profileId =>
        this.http.get<ApiResponse<UserMedicine[]>>(`${this.base}/user-medicines?profileId=${profileId}`)
      ),
      map(r => r.data ?? []),
      catchError(() => of([] as UserMedicine[]))
    );
  }

  private toDisplayItems(medicines: UserMedicine[]): ReminderDisplayItem[] {
    return medicines.slice(0, 5).map(m => ({
      id: m.id,
      medicineName: m.medicineName,
      dosage: m.dosage,
      frequency: m.frequency,
      reminderTime: this.inferNextTime(m.frequency),
      isEnabled: true,
      repeatType: m.frequency,
    }));
  }

  private inferNextTime(f: string): string {
    const fl = (f || '').toLowerCase();
    if (fl.includes('8 hour') || fl.includes('three'))  return 'Next: 2:00 PM';
    if (fl.includes('twice'))                           return 'Next: 8:00 PM';
    if (fl.includes('night') || fl.includes('evening')) return 'Next: 9:00 PM';
    if (fl.includes('noon')  || fl.includes('lunch'))   return 'Next: 12:00 PM';
    return 'Next: 8:00 AM';
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
        if (this._cachedSummary !== null && this._summaryProfileId === profileId && this._summaryLanguage === lang) {
          return of(this._cachedSummary);
        }
        return this.http
          .get<ApiResponse<HealthSummaryDto>>(`${this.base}/chat/health-summary/${profileId}?language=${lang}`)
          .pipe(
            map(r => {
              const data = r.data ?? null;
              this._cachedSummary = data;
              this._summaryProfileId = profileId;
              this._summaryLanguage = lang;
              return data;
            })
          );
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
