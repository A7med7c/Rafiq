import { Component, Input } from '@angular/core';
import { LandingLanguage } from '../../landing';

@Component({
  selector: 'app-features',
  imports: [],
  templateUrl: './features.html',
  styleUrl: './features.css',
})
export class Features {
  @Input() language: LandingLanguage = 'en';

  text = {
    en: {
      learnMore: 'Learn more →',
      items: [
        {
          index: '01',
          icon: 'assets/icons/LP1.jpg',
          title: 'Smart Vitals Sync',
          description: 'Sync with smart devices and track your vitals in real-time.',
        },
        {
          index: '02',
          icon: 'assets/icons/onboarding-hero.svg',
          title: 'Family Health Records',
          description: "Keep your family's medical history organized and accessible.",
        },
        {
          index: '03',
          icon: 'assets/icons/LP2.jpg',
          title: 'Medication Reminder',
          description: 'Set reminders and never miss your medications again.',
        },
        {
          index: '04',
          icon: 'assets/icons/Lp4.jpg',
          title: 'AI Health Assistant',
          description: 'Chat with Rafiq AI to get medical information and answers.',
        },
      ],
    },
    ar: {
      learnMore: 'اعرف أكثر ←',
      items: [
        {
          index: '01',
          icon: 'assets/icons/LP1.jpg',
          title: 'مزامنة ذكية للعلامات الحيوية',
          description: 'وصل أجهزتك الذكية وتابع علاماتك الحيوية لحظة بلحظة.',
        },
        {
          index: '02',
          icon: 'assets/icons/onboarding-hero.svg',
          title: 'السجلات الصحية للعيلة',
          description: 'حافظ على التاريخ الطبي لعيلتك منظم ومتاح في أي وقت.',
        },
        {
          index: '03',
          icon: 'assets/icons/LP2.jpg',
          title: 'تذكير الأدوية',
          description: 'اضبط تذكيرات وماتنساش دواك تاني.',
        },
        {
          index: '04',
          icon: 'assets/icons/Lp4.jpg',
          title: 'مساعد صحي ذكي',
          description: 'تكلم رفيق AI واحصل على معلومات وإجابات طبية.',
        },
      ],
    },
  };

  get t() {
    return this.text[this.language];
  }
}
