import { Component, OnInit, inject } from '@angular/core';
import { Router } from '@angular/router';
import { CommonModule } from '@angular/common';
import { TokenStorageService } from '../../../Services/token-storage-service';
import { Gender, BloodType, AllergySeverity, DiseaseStatus } from '../../../Modles/health-profile-enums';

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

  private readonly router       = inject(Router);
  private readonly tokenStorage = inject(TokenStorageService);

  readonly steps = [
    { label: 'Basic Info' },
    { label: 'Allergies' },
    { label: 'Chronic Diseases' },
    { label: 'Review' },
  ];

  step1: Step1Data | null = null;
  step2: Step2Data | null = null;
  step3: Step3Data | null = null;

  ngOnInit(): void {
    try {
      const s1 = sessionStorage.getItem('onboarding_step1');
      const s2 = sessionStorage.getItem('onboarding_step2');
      const s3 = sessionStorage.getItem('onboarding_step3');
      if (s1) this.step1 = JSON.parse(s1);
      if (s2) this.step2 = JSON.parse(s2);
      if (s3) this.step3 = JSON.parse(s3);
    } catch {
      // fallback: go back to step 1
      this.router.navigate(['/onboarding/step1']);
    }
  }

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
      case BloodType.APositive:  return 'A+';
      case BloodType.ANegative:  return 'A-';
      case BloodType.BPositive:  return 'B+';
      case BloodType.BNegative:  return 'B-';
      case BloodType.ABPositive: return 'AB+';
      case BloodType.ABNegative: return 'AB-';
      case BloodType.OPositive:  return 'O+';
      case BloodType.ONegative:  return 'O-';
      default:                   return '—';
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
      case AllergySeverity.Severe:   return 'badge-high';
      case AllergySeverity.Moderate: return 'badge-medium';
      case AllergySeverity.Mild:     return 'badge-low';
      default:                       return 'badge-medium';
    }
  }

  statusClass(status: number): string {
    const num = Number(status);
    switch (num) {
      case DiseaseStatus.Active:       return 'badge-active';
      case DiseaseStatus.Controlled:   return 'badge-controlled';
      case DiseaseStatus.Resolved:     return 'badge-resolved';
      default:                         return 'badge-active';
    }
  }

  goBack(): void {
    this.router.navigate(['/onboarding/step3']);
  }

  goToAiUpload(): void {
    this.router.navigate(['/onboarding/ai-upload']);
  }

  editStep(step: number): void {
    this.router.navigate([`/onboarding/step${step}`]);
  }

  completeProfile(): void {
    // Mark onboarding done so the user goes straight to dashboard on next login
    this.tokenStorage.markOnboardingCompleted();

    // Clear sessionStorage onboarding data
    sessionStorage.removeItem('onboarding_step1');
    sessionStorage.removeItem('onboarding_step2');
    sessionStorage.removeItem('onboarding_step3');

    this.router.navigate(['/dashboard']);
  }
}
