import { Component, OnInit, computed } from '@angular/core';
import { Router } from '@angular/router';
import { AbstractControl, FormBuilder, FormGroup, ReactiveFormsModule, ValidationErrors, Validators } from '@angular/forms';
import { CommonModule } from '@angular/common';
import { inject } from '@angular/core';
import { Gender, BloodType } from '../../../Modles/health-profile-enums';
import { LocalizationService } from '../../../Services/localization.service';
import { TourEngineService } from '../../../core/assistant/services/tour-engine.service';
import { AssistantAnchorDirective } from '../../../core/assistant/directives/assistant-anchor.directive';

import { AvatarEngineComponent } from '../../../Components/avatar-engine/avatar-engine';

@Component({
  selector: 'app-onboarding-step1',
  standalone: true,
  imports: [CommonModule, ReactiveFormsModule, AssistantAnchorDirective, AvatarEngineComponent],
  templateUrl: './onboarding-step1.html',
  styleUrl: './onboarding-step1.css',
})
export class OnboardingStep1 implements OnInit {

  private readonly router = inject(Router);
  private readonly fb     = inject(FormBuilder);
  private readonly tourEngine = inject(TourEngineService);
  protected readonly l10n = inject(LocalizationService);
  protected readonly t = this.l10n.t;

  readonly today = new Date().toISOString().slice(0, 10);

  readonly steps = computed(() =>
    this.t().onboarding.stepperLabels.map(label => ({ label }))
  );

  get genderOptions() {
    return [
      { value: Gender.Male, label: this.t().common.male },
      { value: Gender.Female, label: this.t().common.female }
    ];
  }

  get bloodTypes() {
    return [
      { value: BloodType.APositive, label: 'A+' },
      { value: BloodType.ANegative, label: 'A-' },
      { value: BloodType.BPositive, label: 'B+' },
      { value: BloodType.BNegative, label: 'B-' },
      { value: BloodType.ABPositive, label: 'AB+' },
      { value: BloodType.ABNegative, label: 'AB-' },
      { value: BloodType.OPositive, label: 'O+' },
      { value: BloodType.ONegative, label: 'O-' }
    ];
  }

  readonly form: FormGroup = this.fb.group({
    dateOfBirth: ['', [Validators.required, this.notFutureDateValidator]],
    gender:      ['', Validators.required],
    height:      ['', [Validators.required, Validators.min(50), Validators.max(300)]],
    weight:      ['', [Validators.required, Validators.min(1), Validators.max(500)]],
    bloodType:   ['', Validators.required],
  });

  ngOnInit(): void {
    const saved = sessionStorage.getItem('onboarding_step1');
    if (saved) {
      try {
        const data = JSON.parse(saved);
        this.form.patchValue(data);
      } catch (e) {
        console.error('Error parsing onboarding_step1 from sessionStorage', e);
      }
    }
  }

  goBack(): void {
    this.router.navigate(['/onboarding/welcome']);
  }

  continue(): void {
    if (this.form.invalid) {
      this.form.markAllAsTouched();
      return;
    }
    // Store step 1 data so later steps can access it
    sessionStorage.setItem('onboarding_step1', JSON.stringify(this.form.getRawValue()));
    // Advance the onboarding tour to the emergency contacts step
    if (this.tourEngine.isPlaying()) this.tourEngine.nextStep();
    this.router.navigate(['/onboarding/emergency']);
  }

  getDateOfBirthError(): string {
    const ctrl = this.form.get('dateOfBirth');
    if (ctrl?.hasError('required')) {
      return this.t().onboarding.step1.dobRequired;
    }
    if (ctrl?.hasError('futureDate')) {
      return this.t().onboarding.step1.dobFuture;
    }
    return this.t().onboarding.step1.dobInvalid;
  }

  isInvalid(field: 'dateOfBirth' | 'gender' | 'height' | 'weight' | 'bloodType'): boolean {
    const ctrl = this.form.get(field);
    return !!ctrl && ctrl.invalid && (ctrl.dirty || ctrl.touched);
  }

  private notFutureDateValidator(control: AbstractControl): ValidationErrors | null {
    if (!control.value) {
      return null;
    }

    const selectedDate = new Date(`${control.value}T00:00:00`);
    const today = new Date();
    today.setHours(0, 0, 0, 0);

    return selectedDate > today ? { futureDate: true } : null;
  }
}
