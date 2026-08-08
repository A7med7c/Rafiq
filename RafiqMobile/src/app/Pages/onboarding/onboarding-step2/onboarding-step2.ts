import { Component, OnInit, OnDestroy, HostListener, inject, computed } from '@angular/core';
import { Router } from '@angular/router';
import { FormArray, FormBuilder, FormGroup, ReactiveFormsModule, Validators } from '@angular/forms';
import { CommonModule } from '@angular/common';
import { Subscription } from 'rxjs';
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
export class OnboardingStep2 implements OnInit, OnDestroy {

  private readonly router = inject(Router);
  private readonly fb     = inject(FormBuilder);
  private readonly tourEngine = inject(TourEngineService);
  protected readonly l10n = inject(LocalizationService);
  protected readonly t = this.l10n.t;

  private valueSub?: Subscription;

  // Per-row dropdown state
  openDropdownIndex: number | null = null;
  dropdownTop = 0;
  dropdownLeft = 0;
  dropdownWidth = 0;

  readonly steps = computed(() => this.t().onboarding.stepperLabels.map((label: string) => ({ label })));

  readonly severityOptions = [
    { value: AllergySeverity.Severe,   label: 'Severe',   labelAr: 'شديدة' },
    { value: AllergySeverity.Moderate, label: 'Moderate', labelAr: 'متوسطة' },
    { value: AllergySeverity.Mild,     label: 'Mild',     labelAr: 'خفيفة' }
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
    if (!target.closest('.severity-shell') && !target.closest('.dropdown-portal')) {
      this.openDropdownIndex = null;
    }
  }

  toggleSeverityDropdown(index: number, trigger: HTMLElement): void {
    if (this.openDropdownIndex === index) {
      this.openDropdownIndex = null;
      return;
    }
    const rect = trigger.getBoundingClientRect();
    this.dropdownTop = rect.bottom + 6;
    this.dropdownLeft = rect.left;
    this.dropdownWidth = Math.max(rect.width, 160);
    this.openDropdownIndex = index;
  }

  selectSeverity(index: number, val: number, event: Event): void {
    event.stopPropagation();
    this.allergiesArray.at(index).get('severity')?.setValue(val);
    this.allergiesArray.at(index).get('severity')?.markAsDirty();
    this.saveState();
    this.openDropdownIndex = null;
  }

  getSeverityLabel(val: number): string {
    const found = this.severityOptions.find(o => o.value === Number(val));
    if (!found) return '';
    return this.l10n.isRtl() ? found.labelAr : found.label;
  }

  getSeverityClass(val: number): string {
    switch (Number(val)) {
      case AllergySeverity.Severe:   return 'severity-high';
      case AllergySeverity.Moderate: return 'severity-medium';
      case AllergySeverity.Mild:     return 'severity-low';
      default: return '';
    }
  }

  private saveState(): void {
    const data = this.hasAllergies === 'yes' ? this.allergiesArray.getRawValue() : [];
    sessionStorage.setItem('onboarding_step2', JSON.stringify({ hasAllergies: this.hasAllergies, allergies: data }));
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
    this.saveState();
  }

  addAllergy(): void {
    this.allergiesArray.push(
      this.fb.group({
        name:     ['', Validators.required],
        severity: [AllergySeverity.Moderate, Validators.required],
      })
    );
    this.saveState();
  }

  removeAllergy(index: number): void {
    this.allergiesArray.removeAt(index);
    if (this.allergiesArray.length === 0) {
      this.hasAllergies = 'no';
    }
    this.saveState();
  }

  getSeverityValue(index: number): number {
    return this.allergiesArray.at(index).get('severity')?.value ?? AllergySeverity.Moderate;
  }

  goBack(): void {
    this.saveState();
    this.router.navigate(['/onboarding/emergency']);
  }

  continue(): void {
    if (this.hasAllergies === 'yes') {
      this.form.markAllAsTouched();
      if (this.form.invalid) return;
    }
    this.saveState();
    if (this.tourEngine.isPlaying()) this.tourEngine.nextStep();
    this.router.navigate(['/onboarding/step3']);
  }

  skip(): void {
    this.hasAllergies = 'no';
    this.allergiesArray.clear();
    this.saveState();
    if (this.tourEngine.isPlaying()) this.tourEngine.nextStep();
    this.router.navigate(['/onboarding/step3']);
  }

  isNameInvalid(index: number): boolean {
    const ctrl = this.allergiesArray.at(index).get('name');
    return !!ctrl && ctrl.invalid && (ctrl.dirty || ctrl.touched);
  }
}
