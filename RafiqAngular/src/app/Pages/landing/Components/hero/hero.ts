import { Component, Input } from '@angular/core';
import { LandingLanguage } from '../../landing';

@Component({
  selector: 'app-hero',
  imports: [],
  templateUrl: './hero.html',
  styleUrl: './hero.css',
})
export class Hero {
  @Input() language: LandingLanguage = 'en';

  text = {
    en: {
      subtitle: 'AI Powered Healthcare',
      title: ['Your AI', 'Healthcare', 'Companion 24/7'],
      description: 'Minimal and premium landing page for your medical assistant.',
      cta: 'Get Started',
    },
    ar: {
      subtitle: 'رعاية صحية مدعومة بالذكاء الاصطناعي',
      title: ['رفيقك الذكي', 'للمتابعة الصحية', 'على مدار الساعة'],
      description: 'منصة ذكية وبسيطة لإدارة سجلك الطبي وتحليل تقاريرك بسهولة.',
      cta: 'ابدأ الآن',
    },
  };

  get t() {
    return this.text[this.language];
  }
}
