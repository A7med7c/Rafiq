import { Component, OnInit, OnDestroy, HostListener, ElementRef, ViewChild, inject, computed } from '@angular/core';
import { Router } from '@angular/router';
import { AbstractControl, FormBuilder, FormGroup, ReactiveFormsModule, ValidationErrors, Validators } from '@angular/forms';
import { CommonModule } from '@angular/common';
import { Subscription } from 'rxjs';
import { Gender } from '../../../Modles/health-profile-enums';
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
export class OnboardingStep1 implements OnInit, OnDestroy {

  private readonly router = inject(Router);
  private readonly fb     = inject(FormBuilder);
  private readonly tourEngine = inject(TourEngineService);
  protected readonly l10n = inject(LocalizationService);
  protected readonly t = this.l10n.t;

  private valueSub?: Subscription;
  dropdownOpen = false;
  dropdownTop = 0;
  dropdownLeft = 0;
  dropdownWidth = 0;

  readonly today = new Date().toISOString().slice(0, 10);

  readonly steps = computed(() => this.t().onboarding.stepperLabels.map(label => ({ label })));

  readonly genderOptions = [
    { value: Gender.Male, labelEn: 'Male', labelAr: 'ذكر' },
    { value: Gender.Female, labelEn: 'Female', labelAr: 'أنثى' }
  ];

  readonly form: FormGroup = this.fb.group({
    dateOfBirth: ['', [Validators.required, this.notFutureDateValidator]],
    gender:      ['', Validators.required],
  });

  ngOnInit(): void {
    const saved = sessionStorage.getItem('onboarding_step1');
    if (saved) {
      try {
        const data = JSON.parse(saved);
        this.form.patchValue({
          dateOfBirth: data.dateOfBirth ?? '',
          gender:      data.gender !== undefined && data.gender !== '' ? Number(data.gender) : '',
        }, { emitEvent: false });
      } catch (e) {
        console.error('Error parsing onboarding_step1 from sessionStorage', e);
      }
    }

    this.valueSub = this.form.valueChanges.subscribe(() => {
      this.saveState();
    });
  }

  ngOnDestroy(): void {
    this.valueSub?.unsubscribe();
  }

  @HostListener('document:click', ['$event'])
  onDocumentClick(event: MouseEvent): void {
    const target = event.target as HTMLElement;
    if (!target.closest('.select-shell') && !target.closest('.dropdown-portal')) {
      this.dropdownOpen = false;
    }
  }

  toggleDropdown(trigger: HTMLElement): void {
    if (this.dropdownOpen) {
      this.dropdownOpen = false;
      return;
    }
    const rect = trigger.getBoundingClientRect();
    this.dropdownTop = rect.bottom + 6;
    this.dropdownLeft = rect.left;
    this.dropdownWidth = rect.width;
    this.dropdownOpen = true;
  }

  selectGender(val: number, event: Event): void {
    event.stopPropagation();
    this.form.get('gender')?.setValue(val);
    this.form.get('gender')?.markAsTouched();
    this.form.get('gender')?.markAsDirty();
    this.saveState();
    this.dropdownOpen = false;
  }

  getSelectedGenderLabel(): string {
    const val = this.form.get('gender')?.value;
    if (val === '' || val === null || val === undefined) return '';
    const found = this.genderOptions.find(g => g.value === Number(val));
    if (!found) return '';
    return this.l10n.isRtl() ? found.labelAr : found.labelEn;
  }

  private saveState(): void {
    const prev = JSON.parse(sessionStorage.getItem('onboarding_step1') ?? '{}');
    sessionStorage.setItem('onboarding_step1', JSON.stringify({ ...prev, ...this.form.getRawValue() }));
  }

  goBack(): void {
    this.saveState();
    this.router.navigate(['/onboarding/welcome']);
  }

  continue(): void {
    if (this.form.invalid) {
      this.form.markAllAsTouched();
      return;
    }
    this.saveState();
    if (this.tourEngine.isPlaying()) this.tourEngine.nextStep();
    this.router.navigate(['/onboarding/step1b']);
  }

  getDateOfBirthError(): string {
    const ctrl = this.form.get('dateOfBirth');
    if (ctrl?.hasError('required')) {
      return 'Date of birth is required';
    }
    if (ctrl?.hasError('futureDate')) {
      return 'Date of birth cannot be in the future';
    }
    return 'Enter a valid date of birth';
  }

  isInvalid(field: 'dateOfBirth' | 'gender'): boolean {
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
