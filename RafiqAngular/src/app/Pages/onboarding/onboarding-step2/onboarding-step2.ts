import { Component, OnInit } from '@angular/core';
import { Router } from '@angular/router';
import { FormArray, FormBuilder, FormGroup, ReactiveFormsModule, Validators } from '@angular/forms';
import { CommonModule } from '@angular/common';
import { inject } from '@angular/core';
import { AllergySeverity } from '../../../Modles/health-profile-enums';
import { LocalizationService } from '../../../Services/localization.service';
import { TourEngineService } from '../../../core/assistant/services/tour-engine.service';
import { AssistantAnchorDirective } from '../../../core/assistant/directives/assistant-anchor.directive';

import { AvatarEngineComponent } from '../../../Components/avatar-engine/avatar-engine';

@Component({
  selector: 'app-onboarding-step2',
  standalone: true,
  imports: [CommonModule, ReactiveFormsModule, AssistantAnchorDirective, AvatarEngineComponent],
  templateUrl: './onboarding-step2.html',
  styleUrl: './onboarding-step2.css',
})
export class OnboardingStep2 implements OnInit {

  private readonly router = inject(Router);
  private readonly fb     = inject(FormBuilder);
  private readonly tourEngine = inject(TourEngineService);
  protected readonly l10n = inject(LocalizationService);
  protected readonly t = this.l10n.t;

  readonly steps = [
    { label: 'Basic Info' },
    { label: 'Emergency Contacts' },
    { label: 'Allergies' },
    { label: 'Chronic Diseases' },
    { label: 'Review' },
  ];

  readonly severityOptions = [
    { value: AllergySeverity.Severe, label: 'Severe' },
    { value: AllergySeverity.Moderate, label: 'Moderate' },
    { value: AllergySeverity.Mild, label: 'Mild' }
  ];

  /** 'yes' | 'no' */
  hasAllergies: 'yes' | 'no' = 'no';

  readonly form: FormGroup = this.fb.group({
    allergies: this.fb.array([]),
  });

  ngOnInit(): void {
    const saved = sessionStorage.getItem('onboarding_step2');
    if (saved) {
      try {
        const data = JSON.parse(saved);
        this.hasAllergies = data.hasAllergies || 'no';
        if (data.allergies && Array.isArray(data.allergies)) {
          this.allergiesArray.clear();
          data.allergies.forEach((allergy: any) => {
            this.allergiesArray.push(
              this.fb.group({
                name:     [allergy.name || '', Validators.required],
                severity: [allergy.severity !== undefined ? Number(allergy.severity) : AllergySeverity.Moderate, Validators.required],
              })
            );
          });
        }
      } catch (e) {
        console.error('Error parsing onboarding_step2 from sessionStorage', e);
      }
    }
  }

  get allergiesArray(): FormArray {
    return this.form.get('allergies') as FormArray;
  }

  selectHasAllergies(value: 'yes' | 'no'): void {
    this.hasAllergies = value;
    if (value === 'yes' && this.allergiesArray.length === 0) {
      this.addAllergy();
    }
    if (value === 'no') {
      this.allergiesArray.clear();
    }
  }

  addAllergy(): void {
    this.allergiesArray.push(
      this.fb.group({
        name:     ['', Validators.required],
        severity: [AllergySeverity.Moderate, Validators.required],
      })
    );
  }

  removeAllergy(index: number): void {
    this.allergiesArray.removeAt(index);
    if (this.allergiesArray.length === 0) {
      this.hasAllergies = 'no';
    }
  }

  severityClass(severity: number | string): string {
    const val = Number(severity);
    switch (val) {
      case AllergySeverity.Severe:   return 'severity-high';
      case AllergySeverity.Moderate: return 'severity-medium';
      case AllergySeverity.Mild:     return 'severity-low';
      default:                       return '';
    }
  }

  getSeverityValue(index: number): number {
    return this.allergiesArray.at(index).get('severity')?.value ?? AllergySeverity.Moderate;
  }

  goBack(): void {
    this.router.navigate(['/onboarding/emergency']);
  }

  continue(): void {
    if (this.hasAllergies === 'yes') {
      this.form.markAllAsTouched();
      if (this.form.invalid) return;
    }

    const data = this.hasAllergies === 'yes'
      ? this.allergiesArray.getRawValue()
      : [];

    sessionStorage.setItem('onboarding_step2', JSON.stringify({ hasAllergies: this.hasAllergies, allergies: data }));
    if (this.tourEngine.isPlaying()) this.tourEngine.nextStep();
    this.router.navigate(['/onboarding/step3']);
  }

  skip(): void {
    this.hasAllergies = 'no';
    this.allergiesArray.clear();
    sessionStorage.setItem('onboarding_step2', JSON.stringify({ hasAllergies: 'no', allergies: [] }));
    if (this.tourEngine.isPlaying()) this.tourEngine.nextStep();
    this.router.navigate(['/onboarding/step3']);
  }

  isNameInvalid(index: number): boolean {
    const ctrl = this.allergiesArray.at(index).get('name');
    return !!ctrl && ctrl.invalid && (ctrl.dirty || ctrl.touched);
  }
}
