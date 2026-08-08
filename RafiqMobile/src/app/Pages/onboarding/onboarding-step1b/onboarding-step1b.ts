import { Component, OnInit, OnDestroy, HostListener, inject, computed } from '@angular/core';
import { Router } from '@angular/router';
import { FormBuilder, FormGroup, ReactiveFormsModule, Validators } from '@angular/forms';
import { CommonModule } from '@angular/common';
import { Subscription } from 'rxjs';
import { BloodType } from '../../../Modles/health-profile-enums';
import { LocalizationService } from '../../../Services/localization.service';
import { TourEngineService } from '../../../core/assistant/services/tour-engine.service';
import { AssistantAnchorDirective } from '../../../core/assistant/directives/assistant-anchor.directive';
import { AvatarEngineComponent } from '../../../Components/avatar-engine/avatar-engine';

@Component({
  selector: 'app-onboarding-step1b',
  standalone: true,
  imports: [CommonModule, ReactiveFormsModule, AssistantAnchorDirective, AvatarEngineComponent],
  templateUrl: './onboarding-step1b.html',
  styleUrl: './onboarding-step1b.css',
})
export class OnboardingStep1b implements OnInit, OnDestroy {

  private readonly router      = inject(Router);
  private readonly fb          = inject(FormBuilder);
  private readonly tourEngine  = inject(TourEngineService);
  protected readonly l10n      = inject(LocalizationService);
  protected readonly t         = this.l10n.t;

  private valueSub?: Subscription;
  dropdownOpen = false;
  dropdownTop = 0;
  dropdownLeft = 0;
  dropdownWidth = 0;

  readonly steps = computed(() => this.t().onboarding.stepperLabels.map((label: string) => ({ label })));

  readonly bloodTypes = [
    { value: BloodType.APositive,  label: 'A+'  },
    { value: BloodType.ANegative,  label: 'A-'  },
    { value: BloodType.BPositive,  label: 'B+'  },
    { value: BloodType.BNegative,  label: 'B-'  },
    { value: BloodType.ABPositive, label: 'AB+' },
    { value: BloodType.ABNegative, label: 'AB-' },
    { value: BloodType.OPositive,  label: 'O+'  },
    { value: BloodType.ONegative,  label: 'O-'  },
  ];

  readonly form: FormGroup = this.fb.group({
    height:    ['', [Validators.required, Validators.min(50),  Validators.max(300)]],
    weight:    ['', [Validators.required, Validators.min(1),   Validators.max(500)]],
    bloodType: ['',  Validators.required],
  });

  ngOnInit(): void {
    const saved = sessionStorage.getItem('onboarding_step1');
    if (saved) {
      try {
        const data = JSON.parse(saved);
        this.form.patchValue({
          height:    data.height    ?? '',
          weight:    data.weight    ?? '',
          bloodType: data.bloodType !== undefined && data.bloodType !== '' ? Number(data.bloodType) : '',
        }, { emitEvent: false });
      } catch { /* ignore */ }
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

  selectBloodType(val: number, event: Event): void {
    event.stopPropagation();
    this.form.get('bloodType')?.setValue(val);
    this.form.get('bloodType')?.markAsTouched();
    this.form.get('bloodType')?.markAsDirty();
    this.saveState();
    this.dropdownOpen = false;
  }

  getSelectedBloodTypeLabel(): string {
    const val = this.form.get('bloodType')?.value;
    if (val === '' || val === null || val === undefined) return '';
    const found = this.bloodTypes.find(b => b.value === Number(val));
    return found ? found.label : '';
  }

  private saveState(): void {
    const prev = JSON.parse(sessionStorage.getItem('onboarding_step1') ?? '{}');
    sessionStorage.setItem('onboarding_step1', JSON.stringify({ ...prev, ...this.form.getRawValue() }));
  }

  goBack(): void {
    this.saveState();
    this.router.navigate(['/onboarding/step1']);
  }

  continue(): void {
    if (this.form.invalid) {
      this.form.markAllAsTouched();
      return;
    }
    this.saveState();
    if (this.tourEngine.isPlaying()) this.tourEngine.nextStep();
    this.router.navigate(['/onboarding/emergency']);
  }

  isInvalid(field: 'height' | 'weight' | 'bloodType'): boolean {
    const ctrl = this.form.get(field);
    return !!ctrl && ctrl.invalid && (ctrl.dirty || ctrl.touched);
  }
}
