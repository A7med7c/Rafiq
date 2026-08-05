import { Component, OnInit, inject } from '@angular/core';
import { Router } from '@angular/router';
import { CommonModule } from '@angular/common';
import { HealthProfileService } from '../../../Services/health-profile.service';
import { localizeKnownApiMessage } from '../../../Utils/api-error.util';
import { LocalizationService } from '../../../Services/localization.service';
import { Gender, BloodType, AllergySeverity, DiseaseStatus } from '../../../Modles/health-profile-enums';
import { CreatePatientProfileRequest } from '../../../Modles/health-profile-request';

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
  selector: 'app-onboarding-ai-upload',
  standalone: true,
  imports: [CommonModule],
  templateUrl: './onboarding-ai-upload.html',
  styleUrl: './onboarding-ai-upload.css',
})
export class OnboardingAiUpload implements OnInit {
  private readonly router        = inject(Router);
  private readonly healthProfile = inject(HealthProfileService);
  protected readonly l10n = inject(LocalizationService);
  protected readonly t = this.l10n.t;

  readonly docTypes = [
    { id: 'lab',          label: 'Lab Report'    },
    { id: 'prescription', label: 'Prescription'  },
    { id: 'radiology',    label: 'Radiology'     },
    { id: 'medical',      label: 'Medical Report'},
    { id: 'other',        label: 'Other'         },
  ];

  readonly extractItems = [
    'Blood Type',
    'Allergies',
    'Chronic Diseases',
    'Medications',
    'And more...',
  ];

  readonly resultItems = [
    { value: 'A+',        category: 'Blood Type' },
    { value: 'Penicillin',category: 'Allergy'    },
    { value: 'Diabetes',  category: 'Disease'    },
    { value: 'Metformin', category: 'Medication' },
  ];

  uploadedFiles: Record<string, File | null> = {};

  step1: Step1Data | null = null;
  step2: Step2Data | null = null;
  step3: Step3Data | null = null;

  /** UI state */
  isSubmitting = false;
  submitError: string | null = null;
  submitSuccess: string | null = null;

  // ── Enum-to-string maps (backend uses JsonStringEnumConverter) ──────────

  private readonly genderMap: Record<number, string> = {
    [Gender.Male]:   'Male',
    [Gender.Female]: 'Female',
  };

  private readonly bloodTypeMap: Record<number, string> = {
    [BloodType.APositive]:  'APositive',
    [BloodType.ANegative]:  'ANegative',
    [BloodType.BPositive]:  'BPositive',
    [BloodType.BNegative]:  'BNegative',
    [BloodType.ABPositive]: 'ABPositive',
    [BloodType.ABNegative]: 'ABNegative',
    [BloodType.OPositive]:  'OPositive',
    [BloodType.ONegative]:  'ONegative',
  };

  private readonly statusMap: Record<number, string> = {
    [DiseaseStatus.Active]:     'Active',
    [DiseaseStatus.Controlled]: 'Controlled',
    [DiseaseStatus.Resolved]:   'Resolved',
  };

  // Wait, let's write exact maps from step 4:

  private readonly severityMap: Record<number, string> = {
    [AllergySeverity.Mild]:     'Mild',
    [AllergySeverity.Moderate]: 'Moderate',
    [AllergySeverity.Severe]:   'Severe',
  };

  ngOnInit(): void {
    try {
      const s1 = sessionStorage.getItem('onboarding_step1');
      const s2 = sessionStorage.getItem('onboarding_step2');
      const s3 = sessionStorage.getItem('onboarding_step3');
      if (s1) this.step1 = JSON.parse(s1);
      if (s2) this.step2 = JSON.parse(s2);
      if (s3) this.step3 = JSON.parse(s3);
    } catch {
      // ignore
    }
  }

  goBack(): void {
    this.router.navigate(['/onboarding/step4']);
  }

  triggerUpload(docId: string): void {
    const el = document.getElementById('file-' + docId) as HTMLInputElement;
    el?.click();
  }

  onFileSelected(event: Event, docId: string): void {
    const input = event.target as HTMLInputElement;
    if (input.files?.[0]) {
      this.uploadedFiles[docId] = input.files[0];
    }
  }

  hasUpload(docId: string): boolean {
    return !!this.uploadedFiles[docId];
  }

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
            this.submitError = messages.length ? messages.join(' ') : this.t().onboarding.step4.errorValidation;
          } else if (body.message) {
            this.submitError = localizeKnownApiMessage(body.message, this.t());
          } else {
            this.submitError = this.t().onboarding.step4.errorDuringSubmission;
          }
        } else if (err.status === 0) {
          this.submitError = this.t().onboarding.step4.errorNetwork;
        } else {
          this.submitError = this.t().onboarding.step4.errorGeneral;
        }
      },
    });
  }

  private buildRequest(): CreatePatientProfileRequest {
    const s1 = this.step1!;
    const s2 = this.step2;
    const s3 = this.step3;

    return {
      dateOfBirth: s1.dateOfBirth,
      gender:      this.genderMap[Number(s1.gender)]    ?? 'Male',
      bloodType:   this.bloodTypeMap[Number(s1.bloodType)] ?? 'OPositive',
      height:      Number(s1.height),
      weight:      Number(s1.weight),

      allergies: (s2?.hasAllergies === 'yes' ? s2.allergies : []).map(a => ({
        name:     a.name,
        severity: this.severityMap[Number(a.severity)] ?? 'Mild',
      })),

      chronicDiseases: (s3?.hasConditions === 'yes' ? s3.conditions : []).map(c => ({
        name:        c.name,
        diagnosedAt: c.diagnosedAt || null,
        status:      this.statusMap[Number(c.status)] ?? 'Active',
      })),
    };
  }

  private clearSessionStorage(): void {
    sessionStorage.removeItem('onboarding_step1');
    sessionStorage.removeItem('onboarding_step2');
    sessionStorage.removeItem('onboarding_step3');
  }
}
