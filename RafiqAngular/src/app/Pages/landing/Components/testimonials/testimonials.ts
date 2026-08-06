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

const TEXT = {
  en: {
    title: 'What Our Users Say',
    subtitle: 'Trusted by thousands of users and families.',
    noReviewsTitle: 'No Reviews Yet',
    noReviewsSub: 'Be the first to share your experience with Rafiq.',
    loading: 'Loading reviews...',
  },
  ar: {
    title: 'إيه اللي بيقوله مستخدمينا',
    subtitle: 'بيثق فينا آلاف المستخدمين والعائلات',
    noReviewsTitle: 'لا يوجد تقييمات بعد',
    noReviewsSub: 'كن أول من يشارك تجربته مع رفيق.',
    loading: 'جارٍ تحميل التقييمات...',
  },
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
  readonly loading = signal(true);

  get t() { return TEXT[this.language]; }

  ngOnInit(): void {
    this.reviewService.getPublic(12).subscribe({
      next: res => {
        if (res.data && res.data.length > 0) {
          const fromApi: TestimonialItem[] = res.data.map((r, i) => ({
            quote: `"${r.comment || '⭐'.repeat(Math.min(r.stars, 5))}"`,
            initials: r.displayName.slice(0, 2).toUpperCase(),
            name: r.displayName,
            role: this.language === 'ar' ? 'مستخدم' : 'User',
            stars: r.stars,
            gradient: GRADIENTS[i % GRADIENTS.length],
          }));
          this.items.set(fromApi);
        } else {
          this.items.set([]);
        }
        this.loading.set(false);
      },
      error: () => {
        this.items.set([]);
        this.loading.set(false);
      },
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
