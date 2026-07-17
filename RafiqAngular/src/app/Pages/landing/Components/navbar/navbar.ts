import {
  AfterViewInit,
  Component,
  HostListener,
  OnDestroy,
  inject,
  signal,
} from '@angular/core';
import { toSignal } from '@angular/core/rxjs-interop';
import { Router, RouterLink } from '@angular/router';
import { AuthService } from '../../../../Services/auth-service';
import { LocalizationService } from '../../../../Services/localization.service';
import { LandingSection } from '../../landing';

@Component({
  selector: 'app-navbar',
  imports: [RouterLink],
  templateUrl: './navbar.html',
  styleUrl: './navbar.css',
})
export class Navbar implements AfterViewInit, OnDestroy {
  private readonly authService = inject(AuthService);
  private readonly router = inject(Router);
  readonly l10n = inject(LocalizationService);

  readonly currentUser = toSignal(this.authService.currentUser$, {
    initialValue: this.authService.currentUser,
  });
  readonly isSigningOut = signal(false);
  readonly isScrolled = signal(false);
  readonly isMenuOpen = signal(false);
  readonly activeSection = signal<LandingSection>('home');

  private observer: IntersectionObserver | null = null;

  readonly sections: LandingSection[] = [
    'home',
    'about',
    'features',
    'how-it-works',
    'testimonials',
    'contact',
  ];

  text = {
    en: {
      brand: 'Rafiq | رفيق',
      home: 'Home',
      about: 'About',
      features: 'Features',
      howItWorks: 'How it works',
      testimonials: 'Testimonials',
      contact: 'Contact',
      login: 'Login',
      cta: 'Get Started',
      dashboard: 'Dashboard',
      account: 'Account',
      signOut: 'Sign Out',
      signingOut: 'Signing out...',
      openMenu: 'Open menu',
      closeMenu: 'Close menu',
    },
    ar: {
      brand: 'رفيق | Rafiq',
      home: 'الرئيسية',
      about: 'عن رفيق',
      features: 'المميزات',
      howItWorks: 'إزاي بيشتغل',
      testimonials: 'آراء المستخدمين',
      contact: 'تواصل معنا',
      login: 'تسجيل الدخول',
      cta: 'ابدأ دلوقتي',
      account: 'الحساب',
      signOut: 'تسجيل خروج',
      signingOut: 'جاري الخروج...',
      openMenu: 'افتح القائمة',
      closeMenu: 'قفل القائمة',
    },
  };

  get t() {
    return this.text[this.l10n.lang()];
  }

  get isAuthenticated(): boolean {
    return this.authService.isLoggedIn;
  }

  get displayName(): string {
    const user = this.currentUser();

    if (!user) {
      return this.t.account;
    }

    return `${user.firstName} ${user.lastName}`.trim() || user.email;
  }

  @HostListener('window:scroll')
  onWindowScroll(): void {
    this.isScrolled.set(window.scrollY > 30);

    if (this.isMenuOpen()) {
      this.closeMenu();
    }
  }

  @HostListener('window:resize')
  onWindowResize(): void {
    if (window.innerWidth > 1180 && this.isMenuOpen()) {
      this.closeMenu();
    }
  }

  ngAfterViewInit(): void {
    this.onWindowScroll();

    const sectionElements = this.sections
      .map((id) => document.getElementById(id))
      .filter((el): el is HTMLElement => el !== null);

    this.observer = new IntersectionObserver(
      (entries) => {
        entries.forEach((entry) => {
          if (entry.isIntersecting) {
            this.activeSection.set(entry.target.id as LandingSection);
          }
        });
      },
      { rootMargin: '-45% 0px -50% 0px', threshold: 0 },
    );

    sectionElements.forEach((section) => this.observer?.observe(section));
  }

  ngOnDestroy(): void {
    this.observer?.disconnect();
    document.body.style.overflow = '';
  }

  setLanguage(language: 'en' | 'ar'): void {
    this.l10n.setLanguage(language);
  }

  toggleMenu(): void {
    this.isMenuOpen.update((open) => !open);
    document.body.style.overflow = this.isMenuOpen() ? 'hidden' : '';
  }

  closeMenu(): void {
    if (!this.isMenuOpen()) {
      return;
    }

    this.isMenuOpen.set(false);
    document.body.style.overflow = '';
  }

  navigateToSection(sectionId: LandingSection): void {
    this.closeMenu();
    this.activeSection.set(sectionId);

    const currentPath = this.router.url.split('#')[0];

    if (currentPath !== '/') {
      this.router.navigate(['/']).then(() => this.scrollToSection(sectionId));
      return;
    }

    this.scrollToSection(sectionId);
  }

  onGetStarted(): void {
    this.closeMenu();
    this.authService.navigateToAppEntry();
  }

  signOut(): void {
    if (this.isSigningOut()) {
      return;
    }

    this.isSigningOut.set(true);

    this.authService.logout().subscribe({
      complete: () => this.isSigningOut.set(false),
    });
  }

  private scrollToSection(sectionId: string): void {
    setTimeout(() => {
      document.getElementById(sectionId)?.scrollIntoView({
        behavior: 'smooth',
        block: 'start',
      });
    });
  }
}
