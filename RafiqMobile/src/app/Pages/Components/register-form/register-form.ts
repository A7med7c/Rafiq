import { ChangeDetectorRef, Component, inject } from '@angular/core';
import {
  AbstractControl,
  FormBuilder,
  ReactiveFormsModule,
  ValidationErrors,
  Validators
} from '@angular/forms';
import { CommonModule } from '@angular/common';
import { Router, RouterLink } from '@angular/router';
import { HttpErrorResponse } from '@angular/common/http';
import { AuthService } from '../../../Services/auth-service';
import { LocalizationService } from '../../../Services/localization.service';
import { getApiErrorMessages } from '../../../Utils/api-error.util';
import { AssistantAnchorDirective } from '../../../core/assistant/directives/assistant-anchor.directive';
import { MediaPickerService } from '../../../Services/media-picker.service';

const EGYPTIAN_PHONE_PATTERN = /^01[0125][0-9]{8}$/;
const PASSWORD_PATTERN = /^(?=.*[a-z])(?=.*[A-Z])(?=.*\d)(?=.*[^A-Za-z0-9]).+$/;

function passwordsMatchValidator(group: AbstractControl): ValidationErrors | null {
  const password = group.get('password')?.value;
  const confirmPassword = group.get('confirmPassword')?.value;

  return password === confirmPassword ? null : { passwordMismatch: true };
}

@Component({
  selector: 'app-register-form',
  standalone: true,
  imports: [CommonModule, ReactiveFormsModule, RouterLink, AssistantAnchorDirective],
  templateUrl: './register-form.html',
  styleUrl: './register-form.css'
})
export class RegisterFormComponent {

  private readonly formBuilder = inject(FormBuilder);
  private readonly authService = inject(AuthService);
  private readonly router = inject(Router);
  private readonly changeDetector = inject(ChangeDetectorRef);
  protected readonly l10n = inject(LocalizationService);
  private readonly mediaPicker = inject(MediaPickerService);
  protected readonly t = this.l10n.t;

  isSubmitting = false;
  showPassword = false;
  showConfirmPassword = false;
  apiErrors: string[] = [];
  successMessage = '';

  profileImage: File | null = null;
  profileImagePreview: string | null = null;

  private static readonly MAX_IMAGE_SIZE_BYTES = 5 * 1024 * 1024;
  private static readonly ALLOWED_IMAGE_TYPES = ['image/jpeg', 'image/png', 'image/webp', 'image/gif'];

  readonly registerForm = this.formBuilder.nonNullable.group(
    {
      firstName: ['', [Validators.required, Validators.maxLength(100)]],
      lastName: ['', [Validators.required, Validators.maxLength(100)]],
      email: ['', [Validators.required, Validators.email, Validators.maxLength(256)]],
      phoneNumber: ['', [Validators.required, Validators.pattern(EGYPTIAN_PHONE_PATTERN)]],
      password: ['', [Validators.required, Validators.minLength(8), Validators.pattern(PASSWORD_PATTERN)]],
      confirmPassword: ['', [Validators.required]]
    },
    { validators: passwordsMatchValidator }
  );

  togglePasswordVisibility(field: 'password' | 'confirmPassword'): void {
    if (field === 'password') {
      this.showPassword = !this.showPassword;
      return;
    }

    this.showConfirmPassword = !this.showConfirmPassword;
  }

  async selectProfileImage(): Promise<void> {
    this.apiErrors = [];
    const file = await this.mediaPicker.selectMedia({ accept: 'image/jpeg,image/png,image/webp,image/gif' });
    if (!file) return;

    if (!RegisterFormComponent.ALLOWED_IMAGE_TYPES.includes(file.type)) {
      this.apiErrors = ['Profile image must be a JPEG, PNG, WEBP, or GIF file.'];
      return;
    }

    if (file.size > RegisterFormComponent.MAX_IMAGE_SIZE_BYTES) {
      this.apiErrors = ['Profile image must not exceed 5 MB.'];
      return;
    }

    this.profileImage = file;

    const reader = new FileReader();
    reader.onload = () => {
      this.profileImagePreview = reader.result as string;
      this.changeDetector.detectChanges();
    };
    reader.readAsDataURL(file);
  }

  removeProfileImage(): void {
    this.profileImage = null;
    this.profileImagePreview = null;
    this.changeDetector.detectChanges();
  }

  onSubmit(): void {
    this.apiErrors = [];
    this.successMessage = '';

    if (this.registerForm.invalid) {
      this.registerForm.markAllAsTouched();
      return;
    }

    this.isSubmitting = true;

    this.authService.register(this.registerForm.getRawValue(), this.profileImage).subscribe({
      next: (response) => {
        this.successMessage = 'Your account has been created successfully. We\'ve sent a verification code to your email.';
        const email = response.data.email;

        setTimeout(() => {
          this.router.navigate(['/verify-account'], {
            queryParams: { email, message: this.successMessage }
          });
        }, 1200);
      },
      error: (error: HttpErrorResponse) => {
        this.apiErrors = getApiErrorMessages(error);
        this.isSubmitting = false;
        this.changeDetector.detectChanges();
      }
    });
  }

  isInvalid(controlName: keyof RegisterFormComponent['registerForm']['controls']): boolean {
    const control = this.registerForm.controls[controlName];
    return control.invalid && (control.dirty || control.touched);
  }

  hasPasswordMismatch(): boolean {
    return this.registerForm.hasError('passwordMismatch') &&
      (this.registerForm.controls.confirmPassword.dirty ||
        this.registerForm.controls.confirmPassword.touched);
  }
}
