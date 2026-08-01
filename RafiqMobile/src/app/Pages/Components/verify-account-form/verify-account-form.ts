import { ChangeDetectorRef, Component, OnDestroy, OnInit, inject } from '@angular/core';
import { CommonModule } from '@angular/common';
import { ActivatedRoute, Router, RouterLink } from '@angular/router';
import { HttpErrorResponse } from '@angular/common/http';
import { AuthService } from '../../../Services/auth-service';
import { LocalizationService } from '../../../Services/localization.service';
import { getApiErrorMessages } from '../../../Utils/api-error.util';

const RESEND_COOLDOWN_SECONDS = 60;

@Component({
  selector: 'app-verify-account-form',
  standalone: true,
  imports: [CommonModule, RouterLink],
  templateUrl: './verify-account-form.html',
  styleUrl: './verify-account-form.css'
})
export class VerifyAccountFormComponent implements OnInit, OnDestroy {

  private readonly route = inject(ActivatedRoute);
  private readonly router = inject(Router);
  private readonly authService = inject(AuthService);
  private readonly changeDetector = inject(ChangeDetectorRef);
  protected readonly l10n = inject(LocalizationService);
  protected readonly t = this.l10n.t;

  email = '';
  verificationCode = '';
  isVerifying = false;
  isResending = false;
  apiErrors: string[] = [];
  successMessage = '';

  resendCooldown = 0;
  private cooldownIntervalId: ReturnType<typeof setInterval> | null = null;

  ngOnInit(): void {
    this.email = this.route.snapshot.queryParamMap.get('email') ?? '';
    this.successMessage = this.route.snapshot.queryParamMap.get('message') ?? '';
    this.startCooldown();
  }

  ngOnDestroy(): void {
    this.clearCooldown();
  }

  verify(): void {
    if (!this.email.trim()) {
      this.apiErrors = ['Please enter your email address.'];
      return;
    }

    if (!this.verificationCode.trim()) {
      this.apiErrors = ['Please enter the verification code.'];
      return;
    }

    this.apiErrors = [];
    this.successMessage = '';
    this.isVerifying = true;

    this.authService.verifyAccount(this.email.trim(), this.verificationCode.trim()).subscribe({
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

  resend(): void {
    if (!this.email.trim() || this.resendCooldown > 0) {
      return;
    }

    this.apiErrors = [];
    this.successMessage = '';
    this.isResending = true;

    this.authService.resendOtp(this.email.trim()).subscribe({
      next: (response) => {
        this.successMessage = response.message;
        this.startCooldown();
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

  private startCooldown(): void {
    this.clearCooldown();
    this.resendCooldown = RESEND_COOLDOWN_SECONDS;

    this.cooldownIntervalId = setInterval(() => {
      this.resendCooldown -= 1;

      if (this.resendCooldown <= 0) {
        this.clearCooldown();
      }

      this.changeDetector.detectChanges();
    }, 1000);
  }

  private clearCooldown(): void {
    if (this.cooldownIntervalId !== null) {
      clearInterval(this.cooldownIntervalId);
      this.cooldownIntervalId = null;
    }
    this.resendCooldown = 0;
  }
}
