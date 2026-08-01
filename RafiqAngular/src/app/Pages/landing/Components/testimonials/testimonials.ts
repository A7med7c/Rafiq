import { Component, Input, OnInit, inject, signal } from '@angular/core';
import { CommonModule } from '@angular/common';
import { LandingLanguage } from '../../landing';
import { ReviewService, PublicReviewDto } from '../../../../Services/review.service';

interface TestimonialItem {
  quote: string;
  initials: string;
  name: string;
  role: string;
  stars: number;
  gradient: string;
}

const GRADIENTS = [
  'linear-gradient(135deg,#ffb199,#ff5c7a)',
  'linear-gradient(135deg,#8ec5ff,#2071ff)',
  'linear-gradient(135deg,#7fe8d0,#18bfff)',
  'linear-gradient(135deg,#ffd580,#ff8c00)',
  'linear-gradient(135deg,#c3a4ff,#7c3aed)',
];

const STATIC: Record<LandingLanguage, TestimonialItem[]> = {
  en: [
    { quote: '"Rafiq has made managing my health so easy. The AI assistant is always there for me!"', initials: 'AM', name: 'Ahmed M.', role: 'Patient', stars: 5, gradient: GRADIENTS[0] },
    { quote: '"I love the medication reminders and how I can keep all my family records in one place."', initials: 'SK', name: 'Sara K.', role: 'User', stars: 5, gradient: GRADIENTS[1] },
    { quote: '"Booking appointments has never been this simple and convenient."', initials: 'OT', name: 'Omar T.', role: 'User', stars: 5, gradient: GRADIENTS[2] },
  ],
  ar: [
    { quote: '"رفيق خلى إدارة صحتي سهلة جداً. المساعد الذكي دايماً موجود معايا!"', initials: 'AM', name: 'أحمد م.', role: 'مريض', stars: 5, gradient: GRADIENTS[0] },
    { quote: '"بحب تذكيرات الأدوية وإزاي أقدر أحتفظ بكل سجلات عيلتي في مكان واحد."', initials: 'SK', name: 'سارة ك.', role: 'مستخدمة', stars: 5, gradient: GRADIENTS[1] },
    { quote: '"حجز المواعيد ماكانش بالسهولة دي قبل كده."', initials: 'OT', name: 'عمر ط.', role: 'مستخدم', stars: 5, gradient: GRADIENTS[2] },
  ],
};

const TEXT = {
  en: { title: 'What Our Users Say', subtitle: 'Trusted by thousands of users and families.' },
  ar: { title: 'إيه اللي بيقوله مستخدمينا', subtitle: 'بيثق فينا آلاف المستخدمين والعائلات' },
};

@Component({
  selector: 'app-testimonials',
  standalone: true,
  imports: [CommonModule],
  templateUrl: './testimonials.html',
  styleUrl: './testimonials.css',
})
export class Testimonials implements OnInit {
  @Input() language: LandingLanguage = 'en';

  private readonly reviewService = inject(ReviewService);

  readonly activeSlide = signal(0);
  readonly items = signal<TestimonialItem[]>([]);

  get t() { return TEXT[this.language]; }

  ngOnInit(): void {
    this.items.set(STATIC[this.language]);
    this.reviewService.getPublic(12).subscribe(res => {
      if (res.data && res.data.length > 0) {
        const fromApi: TestimonialItem[] = res.data.map((r, i) => ({
          quote: `"${r.comment || '⭐'.repeat(r.stars)}"`,
          initials: r.displayName.slice(0, 2).toUpperCase(),
          name: r.displayName,
          role: this.language === 'ar' ? 'مستخدم' : 'User',
          stars: r.stars,
          gradient: GRADIENTS[i % GRADIENTS.length],
        }));
        this.items.set([...fromApi, ...STATIC[this.language]].slice(0, 9));
      }
    });
  }

  selectSlide(index: number): void {
    this.activeSlide.set(index);
    const cards = document.querySelectorAll('.testimonial-card');
    const card = cards[index] as HTMLElement | undefined;
    card?.scrollIntoView({ behavior: 'smooth', block: 'nearest', inline: 'center' });
  }

  starsArray(n: number): number[] {
    return Array.from({ length: 5 }, (_, i) => i + 1);
  }
}
