import { Component, OnInit, ChangeDetectorRef } from '@angular/core';
import { Router } from '@angular/router';
import { FormBuilder, FormGroup, ReactiveFormsModule, Validators } from '@angular/forms';
import { CommonModule } from '@angular/common';
import { inject } from '@angular/core';
import { EmergencyContactService, EmergencyContactResponse } from '../../../Services/emergency-contact.service';
import { TokenStorageService } from '../../../Services/token-storage-service';
import { LocalizationService } from '../../../Services/localization.service';
import { TourEngineService } from '../../../core/assistant/services/tour-engine.service';
import { AssistantAnchorDirective } from '../../../core/assistant/directives/assistant-anchor.directive';

import { AvatarEngineComponent } from '../../../Components/avatar-engine/avatar-engine';

@Component({
  selector: 'app-onboarding-emergency',
  standalone: true,
  imports: [CommonModule, ReactiveFormsModule, AssistantAnchorDirective, AvatarEngineComponent],
  templateUrl: './onboarding-emergency.html',
  styleUrl: './onboarding-emergency.css',
})
export class OnboardingEmergency implements OnInit {

  private readonly router = inject(Router);
  private readonly fb     = inject(FormBuilder);
  private readonly emergencyService = inject(EmergencyContactService);
  private readonly tokenStorage = inject(TokenStorageService);
  private readonly cdr = inject(ChangeDetectorRef);
  private readonly tourEngine = inject(TourEngineService);
  protected readonly l10n = inject(LocalizationService);
  protected readonly t = this.l10n.t;

  get steps() {
    return [
      { label: this.t().onboarding.step4.basicInfo },
      { label: this.t().onboarding.step4.emergencyContacts },
      { label: this.t().onboarding.step4.allergies },
      { label: this.t().onboarding.step4.chronicDiseases },
      { label: this.t().onboarding.step4.title },
    ];
  }

  contacts: EmergencyContactResponse[] = [];
  isLoading = false;
  isAdding = false;
  submitError: string | null = null;

  readonly form: FormGroup = this.fb.group({
    name:        ['', [Validators.required, Validators.maxLength(100)]],
    phoneNumber: ['', [Validators.required, Validators.pattern(/^01[0125][0-9]{8}$/)]],
    relation:    ['', [Validators.required, Validators.maxLength(100)]],
  });

  ngOnInit(): void {
    this.loadContacts();
  }

  loadContacts(): void {
    this.isLoading = true;
    this.emergencyService.getEmergencyContacts().subscribe({
      next: (res) => {
        this.isLoading = false;
        if (res?.success && res.data) {
          this.contacts = res.data;
          this.cdr.detectChanges();
        }
      },
      error: (err) => {
        this.isLoading = false;
        console.error('Failed to load emergency contacts', err);
      }
    });
  }

  addContact(): void {
    if (this.form.invalid) {
      this.form.markAllAsTouched();
      return;
    }

    this.submitError = null;
    const body = this.form.getRawValue();
    const userPhone = this.tokenStorage.getUser()?.phoneNumber;
    const cleanNum = (num: string) => num.replace(/\D/g, '').slice(-10);

    if (userPhone && cleanNum(body.phoneNumber) === cleanNum(userPhone)) {
      this.submitError = this.t().myProfile.toastOwnPhoneError;
      return;
    }

    this.isAdding = true;

    this.emergencyService.createEmergencyContact(body).subscribe({
      next: (res) => {
        this.isAdding = false;
        if (res?.success) {
          this.form.reset({
            name: '',
            phoneNumber: '',
            relation: ''
          });
          this.loadContacts();
        }
      },
      error: (err) => {
        this.isAdding = false;
        const msg = err?.error?.message || this.t().onboarding.emergency.failedAdd;
        this.submitError = msg;
        this.cdr.detectChanges();
      }
    });
  }

  deleteContact(id: string): void {
    if (confirm(this.t().onboarding.emergency.confirmDelete)) {
      this.emergencyService.deleteEmergencyContact(id).subscribe({
        next: (res) => {
          if (res?.success) {
            this.loadContacts();
          }
        },
        error: (err) => {
          console.error('Failed to delete contact', err);
          alert(this.t().onboarding.emergency.failedDelete);
        }
      });
    }
  }

  isInvalid(field: 'name' | 'phoneNumber' | 'relation'): boolean {
    const ctrl = this.form.get(field);
    return !!ctrl && ctrl.invalid && (ctrl.dirty || ctrl.touched);
  }

  getPhoneError(): string {
    const ctrl = this.form.get('phoneNumber');
    if (ctrl?.hasError('required')) return this.t().onboarding.emergency.phoneRequired;
    if (ctrl?.hasError('pattern')) return this.t().onboarding.emergency.phoneInvalid;
    return this.t().onboarding.emergency.phoneError;
  }

  goBack(): void {
    this.router.navigate(['/onboarding/step1']);
  }

  skip(): void {
    if (this.tourEngine.isPlaying()) this.tourEngine.nextStep();
    this.router.navigate(['/onboarding/step2']);
  }

  continueToNextStep(): void {
    if (this.tourEngine.isPlaying()) this.tourEngine.nextStep();
    this.router.navigate(['/onboarding/step2']);
  }
}
