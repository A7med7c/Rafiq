import { Component, Input, inject } from '@angular/core';
import { AuthService } from '../../../../Services/auth-service';
import { LandingLanguage } from '../../landing';

@Component({
  selector: 'app-contact',
  templateUrl: './contact.html',
  styleUrl: './contact.css',
})
export class Contact {
  private readonly authService = inject(AuthService);

  @Input() language: LandingLanguage = 'en';

  text = {
    en: {
      title: 'Ready to Take Control of Your Health?',
      subtitle: 'Join Rafiq today and experience the future of healthcare.',
      cta: 'Get Started Now →',
      note: "It's free and easy to start.",
    },
    ar: {
      title: 'مستعد تتحكم في صحتك؟',
      subtitle: 'انضم لرفيق النهارده وعيش مستقبل الرعاية الصحية.',
      cta: 'ابدأ دلوقتي ←',
      note: 'مجاني وسهل تبدأ.',
    },
  };

  get t() {
    return this.text[this.language];
  }

  onGetStarted(): void {
    this.authService.navigateToAppEntry();
  }
}
