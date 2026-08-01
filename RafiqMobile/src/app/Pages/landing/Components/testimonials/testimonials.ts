import { Component, Input, signal } from '@angular/core';
import { LandingLanguage } from '../../landing';

@Component({
  selector: 'app-testimonials',
  imports: [],
  templateUrl: './testimonials.html',
  styleUrl: './testimonials.css',
})
export class Testimonials {
  @Input() language: LandingLanguage = 'en';

  readonly activeSlide = signal(0);

  text = {
    en: {
      title: 'What Our Users Say',
      subtitle: 'Trusted by thousands of users and families.',
      items: [
        {
          quote: '"Rafiq has made managing my health so easy. The AI assistant is always there for me!"',
          initials: 'AM',
          name: 'Ahmed M.',
          role: 'Patient',
          gradient: 'linear-gradient(135deg,#ffb199,#ff5c7a)',
        },
        {
          quote: '"I love the medication reminders and how I can keep all my family records in one place."',
          initials: 'SK',
          name: 'Sara K.',
          role: 'User',
          gradient: 'linear-gradient(135deg,#8ec5ff,#2071ff)',
        },
        {
          quote: '"Booking appointments has never been this simple and convenient."',
          initials: 'OT',
          name: 'Omar T.',
          role: 'User',
          gradient: 'linear-gradient(135deg,#7fe8d0,#18bfff)',
        },
      ],
    },
    ar: {
      title: 'إيه اللي بيقوله مستخدمينا',
      subtitle: 'بيثق فينا آلاف المستخدمين والعائلات',
      items: [
        {
          quote: '"رفيق خلى إدارة صحتي سهلة جداً. المساعد الذكي دايماً موجود معايا!"',
          initials: 'AM',
          name: 'أحمد م.',
          role: 'مريض',
          gradient: 'linear-gradient(135deg,#ffb199,#ff5c7a)',
        },
        {
          quote: '"بحب تذكيرات الأدوية وإزاي أقدر أحتفظ بكل سجلات عيلتي في مكان واحد."',
          initials: 'SK',
          name: 'سارة ك.',
          role: 'مستخدمة',
          gradient: 'linear-gradient(135deg,#8ec5ff,#2071ff)',
        },
        {
          quote: '"حجز المواعيد ماكانش بالسهولة دي قبل كده."',
          initials: 'OT',
          name: 'عمر ط.',
          role: 'مستخدم',
          gradient: 'linear-gradient(135deg,#7fe8d0,#18bfff)',
        },
      ],
    },
  };

  get t() {
    return this.text[this.language];
  }

  selectSlide(index: number): void {
    this.activeSlide.set(index);
    const cards = document.querySelectorAll('.testimonial-card');
    const card = cards[index] as HTMLElement | undefined;
    card?.scrollIntoView({ behavior: 'smooth', block: 'nearest', inline: 'center' });
  }
}
