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

const PASSWORD_PATTERN = /^(?=.*[a-z])(?=.*[A-Z])(?=.*\d)(?=.*[^A-Za-z0-9]).+$/;

function passwordsMatchValidator(group: AbstractControl): ValidationErrors | null {
  const newPassword = group.get('newPassword')?.value;
  const confirmPassword = group.get('confirmPassword')?.value;

  return newPassword === confirmPassword ? null : { passwordMismatch: true };
}

type Step = 'email' | 'otp' | 'password';

@Component({
  selector: 'app-forgot-password-form',
  standalone: true,
  imports: [CommonModule, ReactiveFormsModule, RouterLink],
  templateUrl: './forgot-password-form.html',
  styleUrl: './forgot-password-form.css'
})
export class ForgotPasswordFormComponent {

  private readonly formBuilder = inject(FormBuilder);
  private readonly authService = inject(AuthService);
  private readonly router = inject(Router);
  private readonly changeDetector = inject(ChangeDetectorRef);
  protected readonly l10n = inject(LocalizationService);
  protected readonly t = this.l10n.t;

  step: Step = 'email';
  isSubmitting = false;
  showPassword = false;
  showConfirmPassword = false;
  apiErrors: string[] = [];
  successMessage = '';

  submittedEmail = '';
  otpCode = '';
  private resetToken = '';

  readonly emailForm = this.formBuilder.nonNullable.group({
    email: ['', [Validators.required, Validators.email, Validators.maxLength(256)]]
  });

  readonly passwordForm = this.formBuilder.nonNullable.group(
    {
      newPassword: ['', [Validators.required, Validators.minLength(8), Validators.pattern(PASSWORD_PATTERN)]],
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

  isEmailInvalid(): boolean {
    const control = this.emailForm.controls.email;
    return control.invalid && (control.dirty || control.touched);
  }

  isPasswordInvalid(controlName: 'newPassword' | 'confirmPassword'): boolean {
    const control = this.passwordForm.controls[controlName];
    return control.invalid && (control.dirty || control.touched);
  }

  hasPasswordMismatch(): boolean {
    return this.passwordForm.hasError('passwordMismatch') &&
      (this.passwordForm.controls.confirmPassword.dirty ||
        this.passwordForm.controls.confirmPassword.touched);
  }

  submitEmail(): void {
    this.apiErrors = [];
    this.successMessage = '';

    if (this.emailForm.invalid) {
      this.emailForm.markAllAsTouched();
      return;
    }

    this.isSubmitting = true;
    const email = this.emailForm.getRawValue().email;

    this.authService.forgotPassword(email).subscribe({
      next: (response) => {
        this.submittedEmail = email;
        this.successMessage = response.message;
        this.step = 'otp';
      },
      error: (error: HttpErrorResponse) => {
        this.apiErrors = getApiErrorMessages(error, this.t());
        this.isSubmitting = false;
        this.changeDetector.detectChanges();
      },
      complete: () => {
        this.isSubmitting = false;
      }
    });
  }

  submitOtp(): void {
    this.apiErrors = [];
    this.successMessage = '';

    if (!this.otpCode.trim()) {
      this.apiErrors = ['Please enter the verification code.'];
      return;
    }

    this.isSubmitting = true;

    this.authService.verifyResetOtp(this.submittedEmail, this.otpCode.trim()).subscribe({
      next: (response) => {
        this.resetToken = response.data.resetToken;
        this.successMessage = response.message;
        this.step = 'password';
      },
      error: (error: HttpErrorResponse) => {
        this.apiErrors = getApiErrorMessages(error, this.t());
        this.isSubmitting = false;
        this.changeDetector.detectChanges();
      },
      complete: () => {
        this.isSubmitting = false;
      }
    });
  }

  resendOtp(): void {
    this.apiErrors = [];
    this.successMessage = '';
    this.isSubmitting = true;

    this.authService.forgotPassword(this.submittedEmail).subscribe({
      next: (response) => {
        this.successMessage = response.message;
      },
      error: (error: HttpErrorResponse) => {
        this.apiErrors = getApiErrorMessages(error, this.t());
        this.isSubmitting = false;
        this.changeDetector.detectChanges();
      },
      complete: () => {
        this.isSubmitting = false;
      }
    });
  }

  submitNewPassword(): void {
    this.apiErrors = [];
    this.successMessage = '';

    if (this.passwordForm.invalid) {
      this.passwordForm.markAllAsTouched();
      return;
    }

    this.isSubmitting = true;
    const { newPassword } = this.passwordForm.getRawValue();

    this.authService.resetPassword(this.resetToken, newPassword).subscribe({
      next: (response) => {
        this.successMessage = response.message;
        setTimeout(() => this.router.navigate(['/login']), 1500);
      },
      error: (error: HttpErrorResponse) => {
        this.apiErrors = getApiErrorMessages(error, this.t());
        this.isSubmitting = false;
        this.changeDetector.detectChanges();
      },
      complete: () => {
        this.isSubmitting = false;
      }
    });
  }
}
