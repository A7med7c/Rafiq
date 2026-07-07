import { Component, OnInit, inject } from '@angular/core';
import { CommonModule } from '@angular/common';
import {
  AbstractControl,
  FormBuilder,
  ReactiveFormsModule,
  ValidationErrors,
  Validators
} from '@angular/forms';
import { Router, RouterLink } from '@angular/router';
import { HttpErrorResponse } from '@angular/common/http';
import { GoogleService } from '../../../Services/google-service';
import { AuthService } from '../../../Services/auth-service';
import { TokenStorageService } from '../../../Services/token-storage-service';
import { environment } from '../../../Environments/Environment';
import { getApiErrorMessages } from '../../../Utils/api-error.util';

function loginIdentifierValidator(control: AbstractControl): ValidationErrors | null {
  const value = (control.value as string | null)?.trim();

  if (!value) {
    return null;
  }

  const isEmail = /^[^\s@]+@[^\s@]+\.[^\s@]+$/.test(value);
  const isPhone = /^01[0125][0-9]{8}$/.test(value);

  return isEmail || isPhone ? null : { invalidLoginIdentifier: true };
}

@Component({
  selector: 'app-login-form',
  standalone: true,
  imports: [CommonModule, ReactiveFormsModule, RouterLink],
  templateUrl: './login-form.html',
  styleUrl: './login-form.css'
})
export class LoginFormComponent implements OnInit {

  private readonly formBuilder = inject(FormBuilder);
  private readonly googleService = inject(GoogleService);
  private readonly authService = inject(AuthService);
  private readonly tokenStorage = inject(TokenStorageService);
  private readonly router = inject(Router);

  isSubmitting = false;
  showPassword = false;
  apiErrors: string[] = [];
  successMessage = '';

  readonly loginForm = this.formBuilder.nonNullable.group({
    loginIdentifier: ['', [Validators.required, loginIdentifierValidator]],
    password: ['', [Validators.required]]
  });

  ngOnInit(): void {
    this.googleService.initialize(
      environment.googleClientId,
      (idToken: string) => this.handleGoogleLogin(idToken)
    );
  }

  togglePasswordVisibility(): void {
    this.showPassword = !this.showPassword;
  }

  onSubmit(): void {
    this.apiErrors = [];
    this.successMessage = '';

    if (this.loginForm.invalid) {
      this.loginForm.markAllAsTouched();
      return;
    }

    this.isSubmitting = true;

    this.authService.login(this.loginForm.getRawValue()).subscribe({
      next: (response) => {
        this.successMessage = response.message;
        this.navigateAfterLogin();
      },
      error: (error: HttpErrorResponse) => {
        this.apiErrors = getApiErrorMessages(error);
      },
      complete: () => {
        this.isSubmitting = false;
      }
    });
  }

  isInvalid(controlName: 'loginIdentifier' | 'password'): boolean {
    const control = this.loginForm.controls[controlName];
    return control.invalid && (control.dirty || control.touched);
  }

  private handleGoogleLogin(idToken: string): void {
    this.apiErrors = [];
    this.successMessage = '';
    this.isSubmitting = true;

    this.authService.loginWithGoogle(idToken).subscribe({
      next: (response) => {
        this.successMessage = response.message;
        this.navigateAfterLogin();
      },
      error: (error: HttpErrorResponse) => {
        this.apiErrors = getApiErrorMessages(error);
        this.isSubmitting = false;
      },
      complete: () => {
        this.isSubmitting = false;
      }
    });
  }

  private navigateAfterLogin(): void {
    // Wait for getMe() to load the user profile, then check onboarding status
    this.authService.currentUser$.subscribe((user) => {
      if (user) {
        if (this.tokenStorage.isOnboardingCompleted()) {
          this.router.navigate(['/dashboard']);
        } else {
          this.router.navigate(['/onboarding/welcome']);
        }
      }
    });
  }
}
