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
import { RegisterResponse } from '../../../Modles/register-response';
import { UserRole } from '../../../Modles/register-request';
import { getApiErrorMessages } from '../../../Utils/api-error.util';

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
  imports: [CommonModule, ReactiveFormsModule, RouterLink],
  templateUrl: './register-form.html',
  styleUrl: './register-form.css'
})
export class RegisterFormComponent {

  private readonly formBuilder = inject(FormBuilder);
  private readonly authService = inject(AuthService);
  private readonly router = inject(Router);
  private readonly changeDetector = inject(ChangeDetectorRef);

  isSubmitting = false;
  isVerifying = false;
  isResending = false;
  showPassword = false;
  showConfirmPassword = false;
  apiErrors: string[] = [];
  successMessage = '';
  registeredUser: RegisterResponse | null = null;
  verificationCode = '';

  readonly roles: UserRole[] = ['Patient', 'Caregiver'];

  readonly registerForm = this.formBuilder.nonNullable.group(
    {
      firstName: ['', [Validators.required, Validators.maxLength(100)]],
      lastName: ['', [Validators.required, Validators.maxLength(100)]],
      email: ['', [Validators.required, Validators.email, Validators.maxLength(256)]],
      phoneNumber: ['', [Validators.required, Validators.pattern(EGYPTIAN_PHONE_PATTERN)]],
      password: ['', [Validators.required, Validators.minLength(8), Validators.pattern(PASSWORD_PATTERN)]],
      confirmPassword: ['', [Validators.required]],
      role: ['Patient' as UserRole, [Validators.required]]
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

  onSubmit(): void {
    this.apiErrors = [];
    this.successMessage = '';

    if (this.registerForm.invalid) {
      this.registerForm.markAllAsTouched();
      return;
    }

    this.isSubmitting = true;

    this.authService.register(this.registerForm.getRawValue()).subscribe({
      next: (response) => {
        this.successMessage = response.message;
        this.registeredUser = response.data;
      },
      error: (error: HttpErrorResponse) => {
        this.apiErrors = getApiErrorMessages(error);
        this.isSubmitting = false;
        this.changeDetector.detectChanges();
      },
      complete: () => {
        this.isSubmitting = false;
      }
    });
  }

  verifyPhone(): void {
    if (!this.registeredUser || !this.verificationCode.trim()) {
      this.apiErrors = ['Please enter the verification code.'];
      return;
    }

    this.apiErrors = [];
    this.successMessage = '';
    this.isVerifying = true;

    this.authService.verifyPhone(
      this.registeredUser.phoneNumber,
      this.verificationCode.trim()
    ).subscribe({
      next: (response) => {
        this.successMessage = response.message;
        setTimeout(() => this.router.navigate(['/login']), 1500);
      },
      error: (error: HttpErrorResponse) => {
        this.apiErrors = getApiErrorMessages(error);
        this.isVerifying = false;
        this.changeDetector.detectChanges();
      },
      complete: () => {
        this.isVerifying = false;
      }
    });
  }

  resendCode(): void {
    if (!this.registeredUser) {
      return;
    }

    this.apiErrors = [];
    this.successMessage = '';
    this.isResending = true;

    this.authService.resendPhoneCode(this.registeredUser.phoneNumber).subscribe({
      next: (response) => {
        this.successMessage = response.message;
      },
      error: (error: HttpErrorResponse) => {
        this.apiErrors = getApiErrorMessages(error);
        this.isResending = false;
        this.changeDetector.detectChanges();
      },
      complete: () => {
        this.isResending = false;
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
