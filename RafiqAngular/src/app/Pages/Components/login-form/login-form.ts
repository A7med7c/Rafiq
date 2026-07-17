import { ChangeDetectorRef, Component, OnInit, inject } from '@angular/core';
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
import { filter, take, switchMap } from 'rxjs';
import { GoogleService } from '../../../Services/google-service';
import { AuthService } from '../../../Services/auth-service';
import { HealthProfileService } from '../../../Services/health-profile.service';
import { LocalizationService } from '../../../Services/localization.service';
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
  private readonly healthProfileSvc = inject(HealthProfileService);
  private readonly router = inject(Router);
  private readonly changeDetector = inject(ChangeDetectorRef);
  protected readonly l10n = inject(LocalizationService);
  protected readonly t = this.l10n.t;

  isSubmitting = false;
  showPassword = false;
  apiErrors: string[] = [];
  successMessage = '';

  readonly loginForm = this.formBuilder.nonNullable.group({
    loginIdentifier: ['', [Validators.required, loginIdentifierValidator]],
    password: ['', [Validators.required, Validators.minLength(8)]]
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
        this.isSubmitting = false;
        this.changeDetector.detectChanges();
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
        this.changeDetector.detectChanges();
      },
      complete: () => {
        this.isSubmitting = false;
      }
    });
  }

  private navigateAfterLogin(): void {
    this.authService.currentUser$.pipe(
      filter(user => !!user),
      take(1),
      switchMap(() => this.healthProfileSvc.getMyProfile()),
    ).subscribe({
      next: () => this.router.navigate(['/dashboard']),
      error: (err: HttpErrorResponse) => {
        if (err.status === 404) {
          this.router.navigate(['/onboarding/welcome']);
        } else {
          this.router.navigate(['/dashboard']);
        }
      },
    });
  }
}
