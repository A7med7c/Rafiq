import { Component, Input, inject } from '@angular/core';
import { AuthService } from '../../../../Services/auth-service';
import { LandingLanguage } from '../../landing';

@Component({
  selector: 'app-hero',
  templateUrl: './hero.html',
  styleUrl: './hero.css',
})
export class Hero {
  private readonly authService = inject(AuthService);

  @Input() language: LandingLanguage = 'en';

  text = {
    en: {
      subtitle: 'AI Powered Healthcare',
      line1: 'Your AI',
      line2: 'Healthcare',
      line3: 'Companion',
      highlight: '| 24/7.',
      description:
        'AI-powered support for your health. Get answers, track your health, manage medications, and book appointments — all in one place.',
      cta: 'Get Started',
      watchDemo: 'Watch Demo',
    },
    ar: {
      subtitle: 'رعاية صحية مدعومة بالذكاء الاصطناعي',
      line1: 'رفيقك الذكي',
      line2: 'للمتابعة الصحية',
      line3: 'على مدار الساعة',
      highlight: '| 24/7.',
      description:
        'منصة ذكية وبسيطة لإدارة سجلك الطبي وتحليل تقاريرك بسهولة. احصل على إجابات، تابع صحتك، وأدر أدويتك ومواعيدك في مكان واحد.',
      cta: 'ابدأ الآن',
      watchDemo: 'شاهد العرض',
    },
  };

  get t() {
    return this.text[this.language];
  }

  onGetStarted(): void {
    this.authService.navigateToAppEntry();
  }
}
