import { Component, inject, OnInit } from '@angular/core';
import { Router } from '@angular/router';
import { LocalizationService } from '../../../Services/localization.service';
import { TourEngineService } from '../../../core/assistant/services/tour-engine.service';
import { AssistantAnchorDirective } from '../../../core/assistant/directives/assistant-anchor.directive';
import { AvatarEngineComponent } from '../../../Components/avatar-engine/avatar-engine';

@Component({
  selector: 'app-onboarding-welcome',
  standalone: true,
  imports: [AssistantAnchorDirective, AvatarEngineComponent],
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
        this.tourEngine.startTour(this.l10n.lang() === 'ar' ? 'onboarding-tour' : 'onboarding-tour-en');
      }
    }, 600);
  }

  getStarted(): void {
    if (this.tourEngine.isPlaying()) this.tourEngine.nextStep();
    this.router.navigate(['/onboarding/step1']);
  }
}
