import { Component, OnInit } from '@angular/core';
import { Router } from '@angular/router';
import { FormBuilder, FormGroup, ReactiveFormsModule, Validators } from '@angular/forms';
import { CommonModule } from '@angular/common';
import { inject } from '@angular/core';
import { Gender, BloodType } from '../../../Modles/health-profile-enums';

@Component({
  selector: 'app-onboarding-step1',
  standalone: true,
  imports: [CommonModule, ReactiveFormsModule],
  templateUrl: './onboarding-step1.html',
  styleUrl: './onboarding-step1.css',
})
export class OnboardingStep1 implements OnInit {

  private readonly router = inject(Router);
  private readonly fb     = inject(FormBuilder);

  readonly steps = [
    { label: 'Basic Info' },
    { label: 'Allergies' },
    { label: 'Chronic Diseases' },
    { label: 'Review' },
  ];

  readonly genderOptions = [
    { value: Gender.Male, label: 'Male' },
    { value: Gender.Female, label: 'Female' }
  ];

  readonly bloodTypes = [
    { value: BloodType.APositive, label: 'A+ (APositive)' },
    { value: BloodType.ANegative, label: 'A- (ANegative)' },
    { value: BloodType.BPositive, label: 'B+ (BPositive)' },
    { value: BloodType.BNegative, label: 'B- (BNegative)' },
    { value: BloodType.ABPositive, label: 'AB+ (ABPositive)' },
    { value: BloodType.ABNegative, label: 'AB- (ABNegative)' },
    { value: BloodType.OPositive, label: 'O+ (OPositive)' },
    { value: BloodType.ONegative, label: 'O- (ONegative)' }
  ];

  readonly form: FormGroup = this.fb.group({
    dateOfBirth: ['', Validators.required],
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
    this.router.navigate(['/onboarding/step2']);
  }

  isInvalid(field: 'dateOfBirth' | 'gender' | 'height' | 'weight' | 'bloodType'): boolean {
    const ctrl = this.form.get(field);
    return !!ctrl && ctrl.invalid && (ctrl.dirty || ctrl.touched);
  }
}
