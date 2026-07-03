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
      items: [
        {
          title: 'Smart Vitals Sync',
          description: 'Smart vitals monitor and healthcare AI to track your health in real-time.',
          alt: 'Vitals sync',
        },
        {
          title: 'Medication Reminder',
          description: 'Never miss a medicine with smart reminders and schedules.',
          alt: 'Reminder',
        },
        {
          title: 'Family Health Records',
          description: "Store and share your family's medical records securely.",
          alt: 'Health records',
        },
      ],
    },
    ar: {
      items: [
        {
          title: 'مزامنة المؤشرات الحيوية',
          description: 'تابع صحتك لحظة بلحظة مع مراقبة ذكية وتحليل طبي مدعوم بالذكاء الاصطناعي.',
          alt: 'مزامنة المؤشرات',
        },
        {
          title: 'تذكير بالأدوية',
          description: 'لا تفوّت جرعة دواء مع تنبيهات ذكية وجداول سهلة التنظيم.',
          alt: 'تذكير بالأدوية',
        },
        {
          title: 'سجلات صحة العائلة',
          description: 'احفظ وشارك السجلات الطبية لعائلتك بأمان وخصوصية.',
          alt: 'السجلات الصحية',
        },
      ],
    },
  };

  get t() {
    return this.text[this.language];
  }
}
