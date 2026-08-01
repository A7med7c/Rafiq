import { ChangeDetectorRef, Component, OnInit, inject, NgZone, ApplicationRef } from '@angular/core';
import { CommonModule } from '@angular/common';
import {
  AbstractControl,
  FormBuilder,
  ReactiveFormsModule,
  ValidationErrors,
  Validators
} from '@angular/forms';
import { Router, RouterLink, Event, NavigationStart, RoutesRecognized, GuardsCheckStart, GuardsCheckEnd, ResolveStart, ResolveEnd, NavigationEnd, NavigationCancel, NavigationError } from '@angular/router';
import { HttpErrorResponse } from '@angular/common/http';
import { filter, take } from 'rxjs';
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
  private readonly ngZone = inject(NgZone);
  private readonly appRef = inject(ApplicationRef);
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

  private logStep(stepName: string): void {
    console.log(`\n===========================================`);
    console.log(`[TRACE] Step: ${stepName}`);
    console.log(`[TRACE] Timestamp: ${new Date().toISOString()}`);
    console.log(`[TRACE] NgZone.isInAngularZone(): ${NgZone.isInAngularZone()}`);
    console.log(`[TRACE] Current route: ${this.router.url}`);
    console.log(`[TRACE] Current component: LoginFormComponent`);
    console.log(`===========================================\n`);
  }

  ngOnInit(): void {
    this.router.events.subscribe((event: Event) => {
      if (event instanceof NavigationStart) console.log('[ROUTER EVENT] NavigationStart:', event.url);
      if (event instanceof RoutesRecognized) console.log('[ROUTER EVENT] RoutesRecognized:', event.urlAfterRedirects);
      if (event instanceof GuardsCheckStart) console.log('[ROUTER EVENT] GuardsCheckStart:', event.urlAfterRedirects);
      if (event instanceof GuardsCheckEnd) console.log('[ROUTER EVENT] GuardsCheckEnd:', event.urlAfterRedirects, 'Result:', event.state);
      if (event instanceof ResolveStart) console.log('[ROUTER EVENT] ResolveStart:', event.urlAfterRedirects);
      if (event instanceof ResolveEnd) console.log('[ROUTER EVENT] ResolveEnd:', event.urlAfterRedirects);
      if (event instanceof NavigationEnd) {
        console.log('[ROUTER EVENT] NavigationEnd:', event.urlAfterRedirects);
        // Verify ApplicationRef.tick() after NavigationEnd
        setTimeout(() => {
          console.log('[VERIFY] Checking if ApplicationRef is stable (tick executed) after NavigationEnd');
        }, 100);
      }
      if (event instanceof NavigationCancel) console.log('[ROUTER EVENT] NavigationCancel:', event.reason);
      if (event instanceof NavigationError) console.log('[ROUTER EVENT] NavigationError:', event.error);
    });

    this.appRef.isStable.subscribe(isStable => {
      console.log(`[APP STATE] ApplicationRef.isStable changed to: ${isStable}`);
    });

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

    this.logStep('Before authService.login() call');
    this.authService.login(this.loginForm.getRawValue()).subscribe({
      next: (response) => {
        this.logStep('Login response received in LoginFormComponent');
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
    this.logStep('navigateAfterLogin() executed - Waiting for currentUser$');
    this.authService.currentUser$.pipe(
      filter(user => !!user),
      take(1)
    ).subscribe({
      next: user => {
        this.logStep('currentUser$ subscription next() fired in LoginFormComponent');
        if (user.role === 'Admin') {
          void this.router.navigate(['/admin']);
          return;
        }

        this.navigatePatientAfterLogin();
      }
    });
  }

  private navigatePatientAfterLogin(): void {
    this.logStep('navigatePatientAfterLogin() executed - Before getMyProfile()');
    this.healthProfileSvc.getMyProfile().subscribe({
      next: () => {
        this.logStep('getMyProfile() response received - Immediately before Router.navigate()');
        
        console.log(`[VERIFY] Testing ChangeDetectorRef behavior: would detectChanges() render Dashboard immediately?`);
        // We do NOT call this.changeDetector.detectChanges() here to avoid fixing the issue, we just log.
        
        const promise = this.router.navigate(['/dashboard']);
        this.logStep('Immediately after Router.navigate() call');
        
        promise.then(result => {
          this.logStep(`Promise resolution of Router.navigate() - Result: ${result}`);
        }).catch(err => {
          console.error(`[TRACE] Promise rejection of Router.navigate():`, err);
        });
      },
      error: (err: HttpErrorResponse) => {
        if (err.status === 404) {
          void this.router.navigate(['/onboarding/welcome']);
        } else {
          void this.router.navigate(['/dashboard']);
        }
      }
    });
  }
}
