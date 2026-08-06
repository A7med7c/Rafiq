import { Component, OnInit, OnDestroy, HostListener, inject, computed } from '@angular/core';
import { Router } from '@angular/router';
import { FormArray, FormBuilder, FormGroup, ReactiveFormsModule, Validators } from '@angular/forms';
import { CommonModule } from '@angular/common';
import { Subscription } from 'rxjs';
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
export class OnboardingStep3 implements OnInit, OnDestroy {

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

  readonly steps = computed(() => this.t().onboarding.stepperLabels.map(label => ({ label })));

  readonly statusOptions = [
    { value: DiseaseStatus.Active,     label: 'Active',     labelAr: 'نشط' },
    { value: DiseaseStatus.Controlled, label: 'Controlled', labelAr: 'تحت السيطرة' },
    { value: DiseaseStatus.Resolved,   label: 'Resolved',   labelAr: 'متعافٍ' }
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
    if (!target.closest('.status-shell') && !target.closest('.dropdown-portal')) {
      this.openDropdownIndex = null;
    }
  }

  toggleStatusDropdown(index: number, trigger: HTMLElement): void {
    if (this.openDropdownIndex === index) {
      this.openDropdownIndex = null;
      return;
    }
    const rect = trigger.getBoundingClientRect();
    this.dropdownTop = rect.bottom + 6;
    this.dropdownLeft = rect.left;
    this.dropdownWidth = Math.max(rect.width, 180);
    this.openDropdownIndex = index;
  }

  selectStatus(index: number, val: number, event: Event): void {
    event.stopPropagation();
    this.conditionsArray.at(index).get('status')?.setValue(val);
    this.conditionsArray.at(index).get('status')?.markAsDirty();
    this.saveState();
    this.openDropdownIndex = null;
  }

  getStatusLabel(val: number): string {
    const found = this.statusOptions.find(o => o.value === Number(val));
    if (!found) return '';
    return this.l10n.isRtl() ? found.labelAr : found.label;
  }

  getStatusClass(val: number): string {
    switch (Number(val)) {
      case DiseaseStatus.Active:       return 'status-active';
      case DiseaseStatus.Controlled:   return 'status-controlled';
      case DiseaseStatus.Resolved:     return 'status-remission';
      default: return '';
    }
  }

  private saveState(): void {
    const data = this.hasConditions === 'yes' ? this.conditionsArray.getRawValue() : [];
    sessionStorage.setItem('onboarding_step3', JSON.stringify({ hasConditions: this.hasConditions, conditions: data }));
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
    this.saveState();
  }

  addCondition(): void {
    this.conditionsArray.push(
      this.fb.group({
        name:         ['', Validators.required],
        diagnosedAt:  ['', Validators.required],
        status:       [DiseaseStatus.Active, Validators.required],
      })
    );
    this.saveState();
  }

  removeCondition(index: number): void {
    this.conditionsArray.removeAt(index);
    if (this.conditionsArray.length === 0) {
      this.hasConditions = 'no';
    }
    this.saveState();
  }

  statusClass(status: number | string): string {
    return this.getStatusClass(Number(status));
  }

  getStatusValue(index: number): number {
    return this.conditionsArray.at(index).get('status')?.value ?? DiseaseStatus.Active;
  }

  isInvalid(index: number, field: 'name' | 'diagnosedAt'): boolean {
    const ctrl = this.conditionsArray.at(index).get(field);
    return !!ctrl && ctrl.invalid && (ctrl.dirty || ctrl.touched);
  }

  goBack(): void {
    this.saveState();
    this.router.navigate(['/onboarding/step2']);
  }

  continue(): void {
    if (this.hasConditions === 'yes') {
      this.form.markAllAsTouched();
      if (this.form.invalid) return;
    }
    this.saveState();
    if (this.tourEngine.isPlaying()) this.tourEngine.nextStep();
    this.router.navigate(['/onboarding/step4']);
  }

  skip(): void {
    this.hasConditions = 'no';
    this.conditionsArray.clear();
    this.saveState();
    if (this.tourEngine.isPlaying()) this.tourEngine.nextStep();
    this.router.navigate(['/onboarding/step4']);
  }
}
