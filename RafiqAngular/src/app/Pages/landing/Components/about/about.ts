import { Component, Input } from '@angular/core';
import { LandingLanguage } from '../../landing';

@Component({
  selector: 'app-about',
  imports: [],
  templateUrl: './about.html',
  styleUrl: './about.css',
})
export class About {
  @Input() language: LandingLanguage = 'en';

  text = {
    en: {
      title: 'All Your Health Needs, In One Place',
      subtitle: 'Rafiq brings everything together to help you live healthier and easier.',
      items: [
        { title: 'Book Appointments', description: 'Find and book doctor appointments effortlessly.' },
        { title: 'Health Records', description: 'Access your medical history anytime, anywhere.' },
        { title: 'Medications', description: 'Manage your medications and set reminders.' },
        { title: 'Health Analytics', description: 'Track your progress with beautiful insights.' },
        { title: 'AI Assistant', description: 'Get 24/7 answers to your health questions.' },
        { title: 'Secure & Private', description: 'Your data is encrypted and 100% confidential.' },
      ],
    },
    ar: {
      title: 'كل احتياجاتك الصحية في مكان واحد',
      subtitle: 'يجمع رفيق كل شيء لمساعدتك على العيش بصحة أفضل وبسهولة.',
      items: [
        { title: 'حجز المواعيد', description: 'ابحث واحجز مواعيد الأطباء بسهولة.' },
        { title: 'السجلات الصحية', description: 'الوصول إلى تاريخك الطبي في أي وقت وأي مكان.' },
        { title: 'الأدوية', description: 'أدر أدويتك واضبط التذكيرات.' },
        { title: 'تحليلات صحية', description: 'تابع تقدمك برؤى واضحة وجميلة.' },
        { title: 'مساعد ذكي', description: 'احصل على إجابات لأسئلتك الصحية على مدار الساعة.' },
        { title: 'آمن وخاص', description: 'بياناتك مشفرة وسرية بنسبة 100%.' },
      ],
    },
  };

  get t() {
    return this.text[this.language];
  }
}
