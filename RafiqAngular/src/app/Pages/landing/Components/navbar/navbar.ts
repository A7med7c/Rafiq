import {
  AfterViewInit,
  Component,
  EventEmitter,
  HostListener,
  Input,
  OnDestroy,
  Output,
  inject,
  signal,
} from '@angular/core';
import { toSignal } from '@angular/core/rxjs-interop';
import { Router, RouterLink } from '@angular/router';
import { AuthService } from '../../../../Services/auth-service';
import { LandingLanguage, LandingSection } from '../../landing';

@Component({
  selector: 'app-navbar',
  imports: [RouterLink],
  templateUrl: './navbar.html',
  styleUrl: './navbar.css',
})
export class Navbar implements AfterViewInit, OnDestroy {
  private readonly authService = inject(AuthService);
  private readonly router = inject(Router);

  @Input() language: LandingLanguage = 'en';
  @Output() languageChange = new EventEmitter<LandingLanguage>();

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
      brand: 'Rafiq | \u0631\u0641\u064a\u0642',
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
      brand: 'Rafiq | \u0631\u0641\u064a\u0642',
      home: '\u0627\u0644\u0631\u0626\u064a\u0633\u064a\u0629',
      about: '\u0639\u0646 \u0631\u0641\u064a\u0642',
      features: '\u0627\u0644\u0645\u0645\u064a\u0632\u0627\u062a',
      howItWorks: '\u0643\u064a\u0641 \u064a\u0639\u0645\u0644',
      testimonials: '\u0627\u0644\u0622\u0631\u0627\u0621',
      contact: '\u062a\u0648\u0627\u0635\u0644',
      login: '\u062f\u062e\u0648\u0644',
      cta: '\u0627\u0628\u062f\u0623 \u0627\u0644\u0622\u0646',
      dashboard: '\u0644\u0648\u062d\u0629 \u0627\u0644\u062a\u062d\u0643\u0645',
      account: '\u0627\u0644\u062d\u0633\u0627\u0628',
      signOut: '\u062a\u0633\u062c\u064a\u0644 \u0627\u0644\u062e\u0631\u0648\u062c',
      signingOut: '\u062c\u0627\u0631\u064a \u062a\u0633\u062c\u064a\u0644 \u0627\u0644\u062e\u0631\u0648\u062c...',
      openMenu: '\u0641\u062a\u062d \u0627\u0644\u0642\u0627\u0626\u0645\u0629',
      closeMenu: '\u0625\u063a\u0644\u0627\u0642 \u0627\u0644\u0642\u0627\u0626\u0645\u0629',
    },
  };

  get t() {
    return this.text[this.language];
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

  setLanguage(language: LandingLanguage): void {
    this.languageChange.emit(language);
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
