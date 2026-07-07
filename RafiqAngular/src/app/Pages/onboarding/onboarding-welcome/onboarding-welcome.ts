import { Component } from '@angular/core';
import { Router } from '@angular/router';

@Component({
  selector: 'app-onboarding-welcome',
  templateUrl: './onboarding-welcome.html',
  styleUrl: './onboarding-welcome.css',
})
export class OnboardingWelcome {
  constructor(private readonly router: Router) {}

  getStarted(): void {
    // Go to the first onboarding step
    this.router.navigate(['/onboarding/step1']);
  }
}
