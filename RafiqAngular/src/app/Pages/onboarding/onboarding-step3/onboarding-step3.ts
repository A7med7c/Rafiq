import { Component, OnInit, inject } from '@angular/core';
import { Router } from '@angular/router';
import { FormArray, FormBuilder, FormGroup, ReactiveFormsModule, Validators } from '@angular/forms';
import { CommonModule } from '@angular/common';
import { DiseaseStatus } from '../../../Modles/health-profile-enums';
import { LocalizationService } from '../../../Services/localization.service';
import { TourEngineService } from '../../../core/assistant/services/tour-engine.service';
import { AssistantAnchorDirective } from '../../../core/assistant/directives/assistant-anchor.directive';

import { AvatarEngineComponent } from '../../../Components/avatar-engine/avatar-engine';

@Component({
  selector: 'app-onboarding-step3',
  standalone: true,
  imports: [CommonModule, ReactiveFormsModule, AssistantAnchorDirective, AvatarEngineComponent],
  templateUrl: './onboarding-step3.html',
  styleUrl: './onboarding-step3.css',
})
export class OnboardingStep3 implements OnInit {

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

  readonly statusOptions = [
    { value: DiseaseStatus.Active, label: 'Active' },
    { value: DiseaseStatus.Controlled, label: 'Controlled' },
    { value: DiseaseStatus.Resolved, label: 'Resolved' }
  ];

  /** 'yes' | 'no' */
  hasConditions: 'yes' | 'no' = 'no';

  readonly form: FormGroup = this.fb.group({
    conditions: this.fb.array([]),
  });

  ngOnInit(): void {
    const saved = sessionStorage.getItem('onboarding_step3');
    if (saved) {
      try {
        const data = JSON.parse(saved);
        this.hasConditions = data.hasConditions || 'no';
        if (data.conditions && Array.isArray(data.conditions)) {
          this.conditionsArray.clear();
          data.conditions.forEach((condition: any) => {
            this.conditionsArray.push(
              this.fb.group({
                name:        [condition.name || '', Validators.required],
                diagnosedAt: [condition.diagnosedAt || '', Validators.required],
                status:      [condition.status !== undefined ? Number(condition.status) : DiseaseStatus.Active, Validators.required],
              })
            );
          });
        }
      } catch (e) {
        console.error('Error parsing onboarding_step3 from sessionStorage', e);
      }
    }
  }

  get conditionsArray(): FormArray {
    return this.form.get('conditions') as FormArray;
  }

  selectHasConditions(value: 'yes' | 'no'): void {
    this.hasConditions = value;
    if (value === 'yes' && this.conditionsArray.length === 0) {
      this.addCondition();
    }
    if (value === 'no') {
      this.conditionsArray.clear();
    }
  }

  addCondition(): void {
    this.conditionsArray.push(
      this.fb.group({
        name:         ['', Validators.required],
        diagnosedAt:  ['', Validators.required],
        status:       [DiseaseStatus.Active, Validators.required],
      })
    );
  }

  removeCondition(index: number): void {
    this.conditionsArray.removeAt(index);
    if (this.conditionsArray.length === 0) {
      this.hasConditions = 'no';
    }
  }

  statusClass(status: number | string): string {
    const val = Number(status);
    switch (val) {
      case DiseaseStatus.Active:       return 'status-active';
      case DiseaseStatus.Controlled:   return 'status-controlled';
      case DiseaseStatus.Resolved:     return 'status-remission'; // Matches color-coded styles
      default:                         return '';
    }
  }

  getStatusValue(index: number): number {
    return this.conditionsArray.at(index).get('status')?.value ?? DiseaseStatus.Active;
  }

  isInvalid(index: number, field: 'name' | 'diagnosedAt'): boolean {
    const ctrl = this.conditionsArray.at(index).get(field);
    return !!ctrl && ctrl.invalid && (ctrl.dirty || ctrl.touched);
  }

  goBack(): void {
    this.router.navigate(['/onboarding/step2']);
  }

  continue(): void {
    if (this.hasConditions === 'yes') {
      this.form.markAllAsTouched();
      if (this.form.invalid) return;
    }

    const data = this.hasConditions === 'yes'
      ? this.conditionsArray.getRawValue()
      : [];

    sessionStorage.setItem(
      'onboarding_step3',
      JSON.stringify({ hasConditions: this.hasConditions, conditions: data })
    );
    if (this.tourEngine.isPlaying()) this.tourEngine.nextStep();
    this.router.navigate(['/onboarding/step4']);
  }

  skip(): void {
    this.hasConditions = 'no';
    this.conditionsArray.clear();
    sessionStorage.setItem(
      'onboarding_step3',
      JSON.stringify({ hasConditions: 'no', conditions: [] })
    );
    if (this.tourEngine.isPlaying()) this.tourEngine.nextStep();
    this.router.navigate(['/onboarding/step4']);
  }
}
