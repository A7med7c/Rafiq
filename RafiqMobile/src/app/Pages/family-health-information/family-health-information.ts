import { Component, OnInit, inject, signal } from '@angular/core';
import { CommonModule } from '@angular/common';
import { FormsModule } from '@angular/forms';
import { ActivatedRoute, Router } from '@angular/router';
import { HttpClient } from '@angular/common/http';
import { catchError, of, forkJoin } from 'rxjs';
import { LocalizationService } from '../../Services/localization.service';
import { NotificationService } from '../../Services/notification.service';
import {
  FamilyProfilesService,
  AccessibleProfileDto,
  PatientProfileDetailDto,
} from '../../Services/family-profiles.service';
import { environment } from '../../Environments/Environment';
import { BottomNav } from '../../shared/bottom-nav/bottom-nav';
import { MobileHeader } from '../../shared/mobile-header/mobile-header';

interface AllergyEntry { id?: string; name: string; severity: string; }
interface DiseaseEntry { id?: string; name: string; status: string; diagnosedAt: string; }

@Component({
  selector: 'app-family-health-information',
  standalone: true,
  imports: [CommonModule, FormsModule, BottomNav, MobileHeader],
  templateUrl: './family-health-information.html',
  styleUrl: './family-health-information.css',
})
export class FamilyHealthInformation implements OnInit {
  protected readonly l10n = inject(LocalizationService);
  protected readonly t = this.l10n.t;
  protected readonly notifSvc = inject(NotificationService);
  private readonly fpSvc = inject(FamilyProfilesService);
  private readonly http = inject(HttpClient);
  private readonly route = inject(ActivatedRoute);
  private readonly base = environment.apiUrl;
  readonly router = inject(Router);

  readonly profileId = signal<string>('');
  readonly profile = signal<AccessibleProfileDto | null>(null);
  readonly detail = signal<PatientProfileDetailDto | null>(null);
  readonly loading = signal(true);

  readonly showEditModal = signal(false);
  readonly editSubmitting = signal(false);
  editError = '';

  readonly severityOptions = ['Mild', 'Moderate', 'Severe'];
  readonly diseaseStatusOptions = ['Active', 'Controlled', 'Resolved'];

  readonly bloodTypes = [
    { value: 'APositive', label: 'A+' }, { value: 'ANegative', label: 'A-' },
    { value: 'BPositive', label: 'B+' }, { value: 'BNegative', label: 'B-' },
    { value: 'ABPositive', label: 'AB+' }, { value: 'ABNegative', label: 'AB-' },
    { value: 'OPositive', label: 'O+' }, { value: 'ONegative', label: 'O-' },
  ];

  editForm = {
    bloodType: '', height: null as number | null, weight: null as number | null,
    allergies: [] as AllergyEntry[],
    chronicDiseases: [] as DiseaseEntry[],
  };

  get todayStr(): string { return new Date().toISOString().split('T')[0]; }

  ngOnInit(): void {
    this.route.paramMap.subscribe(params => {
      const id = params.get('id');
      if (!id) return;
      this.profileId.set(id);
      this.load(id);
    });
  }

  private load(id: string): void {
    this.loading.set(true);
    forkJoin({
      list: this.fpSvc.getAccessible().pipe(catchError(() => of([] as AccessibleProfileDto[]))),
      detail: this.fpSvc.getById(id).pipe(catchError(() => of(null))),
    }).subscribe(({ list, detail }) => {
      this.profile.set(list.find(p => p.userHealthProfileId === id) ?? null);
      this.detail.set(detail);
      this.loading.set(false);
    });
  }

  formatBloodType(bt: string | null | undefined): string {
    if (!bt) return this.t().family.unknown;
    return bt.replace('Positive', '+').replace('Negative', '-');
  }

  // ─── Edit modal ─────────────────────────────────────────────
  openEditModal(): void {
    const d = this.detail();
    if (!d) return;
    this.editError = '';
    this.editForm = {
      bloodType: d.bloodType ?? '',
      height: d.height,
      weight: d.weight,
      allergies: (d.allergies ?? []).map(a => ({ id: a.id, name: a.name, severity: a.severity })),
      chronicDiseases: (d.chronicDiseases ?? []).map(c => ({ id: c.id, name: c.name, status: c.status, diagnosedAt: c.diagnosedAt?.split('T')[0] ?? '' })),
    };
    this.showEditModal.set(true);
  }

  closeEditModal(): void { this.showEditModal.set(false); }

  addAllergy(): void { this.editForm.allergies.push({ name: '', severity: 'Mild' }); }
  removeAllergy(i: number): void { this.editForm.allergies.splice(i, 1); }
  addDisease(): void { this.editForm.chronicDiseases.push({ name: '', status: 'Active', diagnosedAt: '' }); }
  removeDisease(i: number): void { this.editForm.chronicDiseases.splice(i, 1); }

  submitEdit(): void {
    const profileId = this.profileId();
    const p = this.profile();
    if (!profileId || !p) return;
    this.editSubmitting.set(true);
    this.editError = '';

    const f = this.editForm;
    this.http.put<any>(`${this.base}/patient-profiles/${profileId}`, {
      patientProfileId: profileId,
      firstName: p.firstName,
      lastName: p.lastName,
      dateOfBirth: this.detail()?.dateOfBirth?.split('T')[0] ?? '',
      gender: this.detail()?.gender ?? '',
      bloodType: f.bloodType || null,
      height: f.height,
      weight: f.weight,
      relationship: p.isSelf ? null : (p.relationship || null),
    }).pipe(
      catchError(err => {
        const apiErrors: string[] = err?.error?.errors ?? [];
        this.editError = apiErrors.length ? apiErrors.join(' ') : (err?.error?.message || 'Failed to update profile.');
        this.editSubmitting.set(false);
        return of(null);
      })
    ).subscribe(result => {
      if (result === null) return;

      const detail = this.detail();
      const origAllergies = detail?.allergies ?? [];
      const origDiseases = detail?.chronicDiseases ?? [];
      const origAllergyMap = new Map(origAllergies.map(a => [a.id, a]));
      const origDiseaseMap = new Map(origDiseases.map(d => [d.id, d]));

      const finalAllergies = f.allergies.filter(a => a.name.trim());
      const finalDiseases = f.chronicDiseases.filter(d => d.name.trim());
      const finalAllergyIds = new Set(finalAllergies.filter(a => a.id).map(a => a.id!));
      const finalDiseaseIds = new Set(finalDiseases.filter(d => d.id).map(d => d.id!));

      const ops: any[] = [];

      for (const orig of origAllergies) {
        if (!finalAllergyIds.has(orig.id)) {
          ops.push(this.http.delete(`${this.base}/patient-profiles/${profileId}/allergies/${orig.id}`));
        }
      }
      for (const a of finalAllergies) {
        if (!a.id) {
          ops.push(this.http.post(`${this.base}/patient-profiles/${profileId}/allergies`, {
            patientProfileId: profileId, name: a.name.trim(), severity: a.severity,
          }));
        } else {
          const orig = origAllergyMap.get(a.id);
          if (!orig || orig.name !== a.name.trim() || orig.severity !== a.severity) {
            ops.push(this.http.put(`${this.base}/patient-profiles/${profileId}/allergies/${a.id}`, {
              patientProfileId: profileId, allergyId: a.id, name: a.name.trim(), severity: a.severity,
            }));
          }
        }
      }

      for (const orig of origDiseases) {
        if (!finalDiseaseIds.has(orig.id)) {
          ops.push(this.http.delete(`${this.base}/patient-profiles/${profileId}/chronic-diseases/${orig.id}`));
        }
      }
      for (const d of finalDiseases) {
        if (!d.id) {
          ops.push(this.http.post(`${this.base}/patient-profiles/${profileId}/chronic-diseases`, {
            patientProfileId: profileId, name: d.name.trim(), diagnosedAt: d.diagnosedAt || null, status: d.status,
          }));
        } else {
          const orig = origDiseaseMap.get(d.id);
          if (!orig || orig.name !== d.name.trim() || orig.status !== d.status || (orig.diagnosedAt?.split('T')[0] ?? '') !== d.diagnosedAt) {
            ops.push(this.http.put(`${this.base}/patient-profiles/${profileId}/chronic-diseases/${d.id}`, {
              patientProfileId: profileId, diseaseId: d.id, name: d.name.trim(), diagnosedAt: d.diagnosedAt || null, status: d.status,
            }));
          }
        }
      }

      const finish = () => {
        this.editSubmitting.set(false);
        this.closeEditModal();
        this.fpSvc.getById(profileId).pipe(catchError(() => of(null))).subscribe(d => { if (d) this.detail.set(d); });
      };

      if (ops.length === 0) { finish(); return; }

      forkJoin(ops).pipe(
        catchError(err => {
          const apiErrors: string[] = err?.error?.errors ?? [];
          this.editError = apiErrors.length ? apiErrors.join(' ') : (err?.error?.message || 'Failed to update allergies or diseases.');
          this.editSubmitting.set(false);
          return of(null);
        })
      ).subscribe(res => { if (res !== null) finish(); });
    });
  }
}
