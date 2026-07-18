import { Component, OnInit, ChangeDetectorRef } from '@angular/core';
import { Router } from '@angular/router';
import { FormBuilder, FormGroup, ReactiveFormsModule, Validators } from '@angular/forms';
import { CommonModule } from '@angular/common';
import { inject } from '@angular/core';
import { EmergencyContactService, EmergencyContactResponse } from '../../../Services/emergency-contact.service';
import { TokenStorageService } from '../../../Services/token-storage-service';
import { LocalizationService } from '../../../Services/localization.service';

@Component({
  selector: 'app-onboarding-emergency',
  standalone: true,
  imports: [CommonModule, ReactiveFormsModule],
  templateUrl: './onboarding-emergency.html',
  styleUrl: './onboarding-emergency.css',
})
export class OnboardingEmergency implements OnInit {

  private readonly router = inject(Router);
  private readonly fb     = inject(FormBuilder);
  private readonly emergencyService = inject(EmergencyContactService);
  private readonly tokenStorage = inject(TokenStorageService);
  private readonly cdr = inject(ChangeDetectorRef);
  protected readonly l10n = inject(LocalizationService);
  protected readonly t = this.l10n.t;

  readonly steps = [
    { label: 'Basic Info' },
    { label: 'Emergency Contacts' },
    { label: 'Allergies' },
    { label: 'Chronic Diseases' },
    { label: 'Review' }
  ];

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

    this.isAdding = true;
    this.submitError = null;

    const body = this.form.getRawValue();

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
        const msg = err?.error?.message || 'Failed to add emergency contact.';
        this.submitError = msg;
        this.cdr.detectChanges();
      }
    });
  }

  deleteContact(id: string): void {
    if (confirm('Are you sure you want to delete this emergency contact?')) {
      this.emergencyService.deleteEmergencyContact(id).subscribe({
        next: (res) => {
          if (res?.success) {
            this.loadContacts();
          }
        },
        error: (err) => {
          console.error('Failed to delete contact', err);
          alert('Failed to delete contact.');
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
    if (ctrl?.hasError('required')) return 'Phone number is required';
    if (ctrl?.hasError('pattern')) return 'Must be a valid Egyptian mobile number (e.g. 01012345678)';
    return 'Invalid phone number';
  }

  goBack(): void {
    this.router.navigate(['/onboarding/step1']);
  }

  skip(): void {
    this.router.navigate(['/onboarding/step2']);
  }

  continueToNextStep(): void {
    this.router.navigate(['/onboarding/step2']);
  }
}
