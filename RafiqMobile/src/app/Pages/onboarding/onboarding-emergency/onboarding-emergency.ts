import { Component, OnInit, HostListener, ChangeDetectorRef, inject, computed } from '@angular/core';
import { Router } from '@angular/router';
import { FormBuilder, FormGroup, ReactiveFormsModule, Validators } from '@angular/forms';
import { CommonModule } from '@angular/common';
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

  dropdownOpen = false;
  dropdownTop = 0;
  dropdownLeft = 0;
  dropdownWidth = 0;

  readonly steps = computed(() => this.t().onboarding.stepperLabels.map(label => ({ label })));

  readonly relationOptions = [
    { value: 'Father', labelEn: 'Father', labelAr: 'أب' },
    { value: 'Mother', labelEn: 'Mother', labelAr: 'أم' },
    { value: 'Spouse', labelEn: 'Spouse', labelAr: 'زوج / زوجة' },
    { value: 'Son', labelEn: 'Son', labelAr: 'ابن' },
    { value: 'Daughter', labelEn: 'Daughter', labelAr: 'ابنة' },
    { value: 'Brother', labelEn: 'Brother', labelAr: 'أخ' },
    { value: 'Sister', labelEn: 'Sister', labelAr: 'أخت' },
    { value: 'Relative', labelEn: 'Relative', labelAr: 'قريب' },
    { value: 'Friend', labelEn: 'Friend', labelAr: 'صديق' },
    { value: 'Other', labelEn: 'Other', labelAr: 'آخر' }
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

  selectRelation(val: string, event: Event): void {
    event.stopPropagation();
    this.form.get('relation')?.setValue(val);
    this.form.get('relation')?.markAsTouched();
    this.form.get('relation')?.markAsDirty();
    this.dropdownOpen = false;
  }

  getSelectedRelationLabel(): string {
    const val = this.form.get('relation')?.value;
    if (!val) return '';
    const found = this.relationOptions.find(r => r.value === val);
    if (!found) return val;
    return this.l10n.isRtl() ? found.labelAr : found.labelEn;
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
      this.submitError = this.l10n.isRtl()
        ? "لا يمكنك إضافة رقم هاتفك الخاص كجهة اتصال للطوارئ."
        : "You cannot add your own phone number as an emergency contact.";
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
        const msg = err?.error?.message || (this.l10n.isRtl() ? 'فشلت إضافة جهة اتصل الطوارئ.' : 'Failed to add emergency contact.');
        this.submitError = msg;
        this.cdr.detectChanges();
      }
    });
  }

  deleteContact(id: string): void {
    const confirmMsg = this.l10n.isRtl() ? 'هل أنت تأكد من حذف جهة الاتصال هذه؟' : 'Are you sure you want to delete this emergency contact?';
    if (confirm(confirmMsg)) {
      this.emergencyService.deleteEmergencyContact(id).subscribe({
        next: (res) => {
          if (res?.success) {
            this.loadContacts();
          }
        },
        error: (err) => {
          console.error('Failed to delete contact', err);
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
    if (ctrl?.hasError('required')) return this.l10n.isRtl() ? 'رقم الهاتف مطلوب' : 'Phone number is required';
    if (ctrl?.hasError('pattern')) return this.l10n.isRtl() ? 'يجب أن يكون رقم مصري صحيح (مثال: 01012345678)' : 'Must be a valid Egyptian mobile number (e.g. 01012345678)';
    return this.l10n.isRtl() ? 'رقم الهاتف غير صحيح' : 'Invalid phone number';
  }

  goBack(): void {
    this.router.navigate(['/onboarding/step1b']);
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
