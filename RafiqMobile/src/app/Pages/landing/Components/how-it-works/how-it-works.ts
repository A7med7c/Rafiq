import { Component, Input } from '@angular/core';
import { LandingLanguage } from '../../landing';

@Component({
  selector: 'app-how-it-works',
  imports: [],
  templateUrl: './how-it-works.html',
  styleUrl: './how-it-works.css',
})
export class HowItWorks {
  @Input() language: LandingLanguage = 'en';

  text = {
    en: {
      title: 'How Rafiq Works',
      subtitle: 'Simple steps to a healthier you.',
      steps: [
        { title: '1. Create Account', description: 'Sign up and set up your profile.' },
        { title: '2. Sync & Connect', description: 'Connect your devices and add your health data.' },
        { title: '3. Get Support', description: 'Chat with Rafiq AI or explore your health insights.' },
        { title: '4. Take Action', description: 'Book appointments, follow reminders and stay healthy.' },
      ],
    },
    ar: {
      title: 'إزاي رفيق بيشتغل',
      subtitle: 'خطوات بسيطة لحياة أصح.',
      steps: [
        { title: '١. أنشئ حساب', description: 'سجل وأعد ملفك الشخصي بسهولة.' },
        { title: '٢. وصل وزامن', description: 'وصل أجهزتك وأضف بياناتك الصحية.' },
        { title: '٣. احصل على دعم', description: 'تكلم مع رفيق AI أو اكتشف معلوماتك الصحية.' },
        { title: '٤. اتحرك', description: 'احجز مواعيد، اتبع التذكيرات، وابقى بصحة.' },
      ],
    },
  };

  get t() {
    return this.text[this.language];
  }
}
