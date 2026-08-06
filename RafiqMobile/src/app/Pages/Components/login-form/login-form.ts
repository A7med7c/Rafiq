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
import { filter, take } from 'rxjs';
import { GoogleService } from '../../../Services/google-service';
import { AuthService } from '../../../Services/auth-service';
import { HealthProfileService } from '../../../Services/health-profile.service';
import { LocalizationService } from '../../../Services/localization.service';
import { environment } from '../../../Environments/Environment';
import { getApiErrorMessages } from '../../../Utils/api-error.util';
import { AssistantAnchorDirective } from '../../../core/assistant/directives/assistant-anchor.directive';

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
  imports: [CommonModule, ReactiveFormsModule, RouterLink, AssistantAnchorDirective],
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
        this.successMessage = this.localizeLoginMessage(response.message);
        this.navigateAfterLogin();
      },
      error: (error: HttpErrorResponse) => {
        this.apiErrors = getApiErrorMessages(error, this.t())
          .map((message) => this.localizeLoginMessage(message));
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
        this.successMessage = this.localizeLoginMessage(response.message);
        this.navigateAfterLogin();
      },
      error: (error: HttpErrorResponse) => {
        this.apiErrors = getApiErrorMessages(error, this.t())
          .map((message) => this.localizeLoginMessage(message));
        this.isSubmitting = false;
        this.changeDetector.detectChanges();
      },
      complete: () => {
        this.isSubmitting = false;
      }
    });
  }

  private handleUnverifiedAccount(error: HttpErrorResponse, errorMessages: string[]): boolean {
    if (!this.isUnverifiedAccountError(error, errorMessages)) {
      return false;
    }

    const rawId = this.loginForm.getRawValue().loginIdentifier?.trim() || '';
    const errorBody = error.error as any;
    const email = errorBody?.email || errorBody?.data?.email || (rawId.includes('@') ? rawId : '');

    const redirectMsg = this.l10n.isRtl()
      ? 'حسابك غير مفعل بعد. لقد أرسلنا رمز التحقق إلى بريدك الإلكتروني.'
      : 'Your account is not verified. A verification code has been sent to your email.';

    if (email) {
      this.authService.resendOtp(email).subscribe({
        next: () => {
          void this.router.navigate(['/verify-account'], {
            queryParams: { email, message: redirectMsg }
          });
        },
        error: () => {
          void this.router.navigate(['/verify-account'], {
            queryParams: { email, message: redirectMsg }
          });
        }
      });
    } else {
      void this.router.navigate(['/verify-account'], {
        queryParams: { message: redirectMsg }
      });
    }

    return true;
  }

  private isUnverifiedAccountError(error: HttpErrorResponse, messages: string[]): boolean {
    const body = error.error as any;
    if (body?.requiresVerification || body?.emailNotVerified || body?.code === 'EmailNotVerified' || body?.code === 'AccountNotVerified') {
      return true;
    }

    const combinedText = [
      body?.message || '',
      body?.code || '',
      ...(messages || []),
      JSON.stringify(body || {})
    ].join(' ').toLowerCase();

    return /unverified|not verified|notverified|verify your|verify-email|verifyemail|email_not_verified/i.test(combinedText);
  }

  private navigateAfterLogin(): void {
    this.authService.currentUser$.pipe(
      filter(user => !!user),
      take(1)
    ).subscribe({
      next: user => {
        if (user.role === 'Admin') {
          void this.router.navigate(['/admin']);
          return;
        }

        this.navigatePatientAfterLogin();
      }
    });
  }

  private navigatePatientAfterLogin(): void {
    this.healthProfileSvc.getMyProfile().subscribe({
      next: () => void this.router.navigate(['/dashboard']),
      error: (err: HttpErrorResponse) => {
        if (err.status === 404) {
          void this.router.navigate(['/onboarding/welcome']);
        } else {
          void this.router.navigate(['/dashboard']);
        }
      }
    });
  }

  private localizeLoginMessage(message: string): string {
    const loginTranslations = this.t().login;
    const normalizedMessage = message.trim().replace(/\.$/, '');

    const messages: Record<string, string> = {
      'Invalid email / phone number or password': loginTranslations.invalidCredentials,
      'Please verify your email before logging in': loginTranslations.verifyEmailBeforeLogin,
      'Login successful': loginTranslations.loginSuccess,
      'Google login successful': loginTranslations.googleLoginSuccess,
    };

    return messages[normalizedMessage] ?? message;
  }
}
