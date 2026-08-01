import { Component, inject } from '@angular/core';
import { Router } from '@angular/router';
import { LocalizationService } from '../../../Services/localization.service';

@Component({
  selector: 'app-onboarding-welcome',
  templateUrl: './onboarding-welcome.html',
  styleUrl: './onboarding-welcome.css',
})
export class OnboardingWelcome {
  protected readonly l10n = inject(LocalizationService);
  protected readonly t = this.l10n.t;

  constructor(private readonly router: Router) {}

  getStarted(): void {
    // Go to the first onboarding step
    this.router.navigate(['/onboarding/step1']);
  }
}
