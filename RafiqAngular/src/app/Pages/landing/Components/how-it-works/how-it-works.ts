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
      title: 'كيف يعمل رفيق',
      subtitle: 'خطوات بسيطة لحياة أكثر صحة.',
      steps: [
        { title: '1. إنشاء حساب', description: 'سجّل وأنشئ ملفك الشخصي.' },
        { title: '2. المزامنة والربط', description: 'اربط أجهزتك وأضف بياناتك الصحية.' },
        { title: '3. احصل على الدعم', description: 'تحدث مع رفيق أو استكشف رؤاك الصحية.' },
        { title: '4. اتخذ إجراء', description: 'احجز المواعيد واتبع التذكيرات وابقَ بصحة جيدة.' },
      ],
    },
  };

  get t() {
    return this.text[this.language];
  }
}
