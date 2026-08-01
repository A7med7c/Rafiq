import { Component, inject, OnInit } from '@angular/core';
import { Router } from '@angular/router';
import { LocalizationService } from '../../../Services/localization.service';
import { TourEngineService } from '../../../core/assistant/services/tour-engine.service';
import { AssistantAnchorDirective } from '../../../core/assistant/directives/assistant-anchor.directive';

@Component({
  selector: 'app-onboarding-welcome',
  standalone: true,
  imports: [AssistantAnchorDirective],
  templateUrl: './onboarding-welcome.html',
  styleUrl: './onboarding-welcome.css',
})
export class OnboardingWelcome implements OnInit {
  protected readonly l10n = inject(LocalizationService);
  private readonly tourEngine = inject(TourEngineService);
  protected readonly t = this.l10n.t;

  constructor(private readonly router: Router) {}

  ngOnInit(): void {
    setTimeout(() => {
      if (!this.tourEngine.isPlaying()) {
        this.tourEngine.startTour('onboarding-tour');
      }
    }, 600);
  }

  getStarted(): void {
    if (this.tourEngine.isPlaying()) this.tourEngine.nextStep();
    this.router.navigate(['/onboarding/step1']);
  }
}
