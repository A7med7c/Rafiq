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
      title: 'كل احتياجاتك الصحية، في مكان واحد',
      subtitle: 'رفيق بيجمع كل حاجة عشان تعيش أصح وأسهل.',
      items: [
        { title: 'احجز مواعيد', description: 'دور واحجز مواعيد الدكاترة بسهولة.' },
        { title: 'السجلات الطبية', description: 'وصل لتاريخك الطبي في أي وقت ومن أي مكان.' },
        { title: 'الأدوية', description: 'اتحكم في أدويتك واضبط تذكيرات عشان ماتنساش.' },
        { title: 'تحليلات صحية', description: 'تابع تقدمك بتقارير وإحصائيات واضحة.' },
        { title: 'مساعد ذكي', description: 'احصل على إجابات لأسئلتك الصحية على مدار اليوم.' },
        { title: 'آمن وخاص', description: 'بياناتك مشفرة وسرية 100%.' },
      ],
    },
  };

  get t() {
    return this.text[this.language];
  }
}
