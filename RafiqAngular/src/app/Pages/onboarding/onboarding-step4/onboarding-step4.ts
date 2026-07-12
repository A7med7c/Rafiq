import { Component, OnInit, inject, ChangeDetectorRef } from '@angular/core';
import { Router } from '@angular/router';
import { CommonModule } from '@angular/common';
import { HealthProfileService } from '../../../Services/health-profile.service';
import { Gender, BloodType, AllergySeverity, DiseaseStatus } from '../../../Modles/health-profile-enums';
import { CreatePatientProfileRequest } from '../../../Modles/health-profile-request';
import { EmergencyContactService, EmergencyContactResponse } from '../../../Services/emergency-contact.service';

interface Step1Data {
  dateOfBirth: string;
  gender: number;
  height: string;
  weight: string;
  bloodType: number;
}

interface AllergyEntry {
  name: string;
  severity: number;
}

interface Step2Data {
  hasAllergies: 'yes' | 'no';
  allergies: AllergyEntry[];
}

interface ConditionEntry {
  name: string;
  diagnosedAt: string;
  status: number;
}

interface Step3Data {
  hasConditions: 'yes' | 'no';
  conditions: ConditionEntry[];
}

@Component({
  selector: 'app-onboarding-step4',
  standalone: true,
  imports: [CommonModule],
  templateUrl: './onboarding-step4.html',
  styleUrl: './onboarding-step4.css',
})
export class OnboardingStep4 implements OnInit {

  private readonly router = inject(Router);
  private readonly healthProfile = inject(HealthProfileService);
  private readonly emergencyService = inject(EmergencyContactService);
  private readonly cdr = inject(ChangeDetectorRef);

  readonly steps = [
    { label: 'Basic Info' },
    { label: 'Emergency Contacts' },
    { label: 'Allergies' },
    { label: 'Chronic Diseases' },
    { label: 'Review' },
  ];

  step1: Step1Data | null = null;
  step2: Step2Data | null = null;
  step3: Step3Data | null = null;
  emergencyContacts: EmergencyContactResponse[] = [];

  /** UI state */
  isSubmitting = false;
  submitError: string | null = null;
  submitSuccess: string | null = null;

  // ── Enum-to-string maps (backend uses JsonStringEnumConverter) ──────────

  private readonly genderMap: Record<number, string> = {
    [Gender.Male]: 'Male',
    [Gender.Female]: 'Female',
  };

  private readonly bloodTypeMap: Record<number, string> = {
    [BloodType.APositive]: 'APositive',
    [BloodType.ANegative]: 'ANegative',
    [BloodType.BPositive]: 'BPositive',
    [BloodType.BNegative]: 'BNegative',
    [BloodType.ABPositive]: 'ABPositive',
    [BloodType.ABNegative]: 'ABNegative',
    [BloodType.OPositive]: 'OPositive',
    [BloodType.ONegative]: 'ONegative',
  };

  private readonly severityMap: Record<number, string> = {
    [AllergySeverity.Mild]: 'Mild',
    [AllergySeverity.Moderate]: 'Moderate',
    [AllergySeverity.Severe]: 'Severe',
  };

  private readonly statusMap: Record<number, string> = {
    [DiseaseStatus.Active]: 'Active',
    [DiseaseStatus.Controlled]: 'Controlled',
    [DiseaseStatus.Resolved]: 'Resolved',
  };

  // ── Lifecycle ───────────────────────────────────────────────────────────

  ngOnInit(): void {
    try {
      const s1 = sessionStorage.getItem('onboarding_step1');
      const s2 = sessionStorage.getItem('onboarding_step2');
      const s3 = sessionStorage.getItem('onboarding_step3');
      if (s1) this.step1 = JSON.parse(s1);
      if (s2) this.step2 = JSON.parse(s2);
      if (s3) this.step3 = JSON.parse(s3);
      this.loadEmergencyContacts();
    } catch {
      this.router.navigate(['/onboarding/step1']);
    }
  }

  loadEmergencyContacts(): void {
    this.emergencyService.getEmergencyContacts().subscribe({
      next: (res) => {
        console.log('Review step loadEmergencyContacts res:', res);
        if (res?.success && res.data) {
          this.emergencyContacts = res.data;
          console.log('Review step emergencyContacts set:', this.emergencyContacts);
          this.cdr.detectChanges();
        }
      },
      error: (err) => {
        console.error('Failed to load emergency contacts in review step', err);
      }
    });
  }

  // ── Display helpers ─────────────────────────────────────────────────────

  /** Format ISO date string "YYYY-MM-DD" → "DD / MM / YYYY" */
  formatDate(iso: string): string {
    if (!iso) return '—';
    const [y, m, d] = iso.split('-');
    return `${d} / ${m} / ${y}`;
  }

  getGenderLabel(val: number): string {
    const num = Number(val);
    if (num === Gender.Male) return 'Male';
    if (num === Gender.Female) return 'Female';
    return '—';
  }

  getBloodTypeLabel(val: number): string {
    const num = Number(val);
    switch (num) {
      case BloodType.APositive: return 'A+';
      case BloodType.ANegative: return 'A-';
      case BloodType.BPositive: return 'B+';
      case BloodType.BNegative: return 'B-';
      case BloodType.ABPositive: return 'AB+';
      case BloodType.ABNegative: return 'AB-';
      case BloodType.OPositive: return 'O+';
      case BloodType.ONegative: return 'O-';
      default: return '—';
    }
  }

  getSeverityLabel(val: number): string {
    const num = Number(val);
    if (num === AllergySeverity.Severe) return 'Severe';
    if (num === AllergySeverity.Moderate) return 'Moderate';
    if (num === AllergySeverity.Mild) return 'Mild';
    return '—';
  }

  getStatusLabel(val: number): string {
    const num = Number(val);
    if (num === DiseaseStatus.Active) return 'Active';
    if (num === DiseaseStatus.Controlled) return 'Controlled';
    if (num === DiseaseStatus.Resolved) return 'Resolved';
    return '—';
  }

  severityClass(severity: number): string {
    const num = Number(severity);
    switch (num) {
      case AllergySeverity.Severe: return 'badge-high';
      case AllergySeverity.Moderate: return 'badge-medium';
      case AllergySeverity.Mild: return 'badge-low';
      default: return 'badge-medium';
    }
  }

  statusClass(status: number): string {
    const num = Number(status);
    switch (num) {
      case DiseaseStatus.Active: return 'badge-active';
      case DiseaseStatus.Controlled: return 'badge-controlled';
      case DiseaseStatus.Resolved: return 'badge-resolved';
      default: return 'badge-active';
    }
  }

  // ── Navigation ──────────────────────────────────────────────────────────

  goBack(): void {
    this.router.navigate(['/onboarding/step3']);
  }

  goToAiUpload(): void {
    this.router.navigate(['/onboarding/ai-upload']);
  }

  editStep(step: number | string): void {
    if (step === 'emergency') {
      this.router.navigate(['/onboarding/emergency']);
    } else {
      this.router.navigate([`/onboarding/step${step}`]);
    }
  }

  // ── Submit ──────────────────────────────────────────────────────────────

  completeProfile(): void {
    if (!this.step1) {
      this.submitError = 'Basic information is missing. Please go back to step 1.';
      return;
    }

    this.isSubmitting = true;
    this.submitError  = null;
    this.submitSuccess = null;

    const request = this.buildRequest();

    this.healthProfile.createProfile(request).subscribe({
      next: (res) => {
        this.isSubmitting = false;
        this.submitSuccess = res?.message || 'Patient profile created successfully!';
        this.clearSessionStorage();
        setTimeout(() => {
          this.router.navigate(['/dashboard']);
        }, 2000);
      },
      error: (err) => {
        this.isSubmitting = false;
        const body = err?.error;
        if (err.status === 409) {
          // Profile already exists — treat as success so the user can move forward
          this.submitSuccess = 'Your health profile is already complete.';
          this.clearSessionStorage();
          setTimeout(() => {
            this.router.navigate(['/dashboard']);
          }, 2000);
        } else if (body) {
          if (Array.isArray(body.errors)) {
            this.submitError = body.errors.join(' ');
          } else if (body.errors && typeof body.errors === 'object') {
            const messages: string[] = [];
            for (const key of Object.keys(body.errors)) {
              const errVal = body.errors[key];
              if (Array.isArray(errVal)) {
                messages.push(...errVal);
              } else if (typeof errVal === 'string') {
                messages.push(errVal);
              }
            }
            this.submitError = messages.length ? messages.join(' ') : 'Validation failed.';
          } else if (body.message) {
            this.submitError = body.message;
          } else {
            this.submitError = 'An error occurred during profile submission.';
          }
        } else if (err.status === 0) {
          this.submitError = 'Cannot connect to the server. Please check your internet connection.';
        } else {
          this.submitError = 'Something went wrong. Please try again.';
        }
      },
    });
  }

  // ── Private helpers ─────────────────────────────────────────────────────

  private buildRequest(): CreatePatientProfileRequest {
    const s1 = this.step1!;
    const s2 = this.step2;
    const s3 = this.step3;

    return {
      dateOfBirth: s1.dateOfBirth,               // already "YYYY-MM-DD"
      gender: this.genderMap[Number(s1.gender)] ?? 'Male',
      bloodType: this.bloodTypeMap[Number(s1.bloodType)] ?? 'OPositive',
      height: Number(s1.height),
      weight: Number(s1.weight),

      allergies: (s2?.hasAllergies === 'yes' ? s2.allergies : []).map(a => ({
        name: a.name,
        severity: this.severityMap[Number(a.severity)] ?? 'Mild',
      })),

      chronicDiseases: (s3?.hasConditions === 'yes' ? s3.conditions : []).map(c => ({
        name: c.name,
        diagnosedAt: c.diagnosedAt || null,   // "YYYY-MM-DD" or null
        status: this.statusMap[Number(c.status)] ?? 'Active',
      })),
    };
  }

  private clearSessionStorage(): void {
    sessionStorage.removeItem('onboarding_step1');
    sessionStorage.removeItem('onboarding_step2');
    sessionStorage.removeItem('onboarding_step3');
  }
}
