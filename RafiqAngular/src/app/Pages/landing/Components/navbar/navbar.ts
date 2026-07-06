import { Component, EventEmitter, Input, Output, inject, signal } from '@angular/core';
import { toSignal } from '@angular/core/rxjs-interop';
import { Router, RouterLink } from '@angular/router';
import { AuthService } from '../../../../Services/auth-service';
import { LandingLanguage } from '../../landing';

@Component({
  selector: 'app-navbar',
  imports: [RouterLink],
  templateUrl: './navbar.html',
  styleUrl: './navbar.css',
})
export class Navbar {
  private readonly authService = inject(AuthService);
  private readonly router = inject(Router);

  @Input() language: LandingLanguage = 'en';
  @Output() languageChange = new EventEmitter<LandingLanguage>();

  readonly currentUser = toSignal(this.authService.currentUser$, {
    initialValue: this.authService.currentUser
  });
  readonly isSigningOut = signal(false);

  text = {
    en: {
      brand: 'Rafiq | \u0631\u0641\u064a\u0642',
      home: 'Home',
      about: 'About',
      contact: 'Contact',
      login: 'Login',
      cta: 'Get Started',
      account: 'Account',
      signOut: 'Sign Out',
      signingOut: 'Signing out...',
    },
    ar: {
      brand: '\u0631\u0641\u064a\u0642 | Rafiq',
      home: '\u0627\u0644\u0631\u0626\u064a\u0633\u064a\u0629',
      about: '\u0645\u0646 \u0646\u062d\u0646',
      contact: '\u062a\u0648\u0627\u0635\u0644 \u0645\u0639\u0646\u0627',
      login: '\u062a\u0633\u062c\u064a\u0644 \u0627\u0644\u062f\u062e\u0648\u0644',
      cta: '\u0627\u0628\u062f\u0623 \u0627\u0644\u0622\u0646',
      account: '\u0627\u0644\u062d\u0633\u0627\u0628',
      signOut: '\u062a\u0633\u062c\u064a\u0644 \u0627\u0644\u062e\u0631\u0648\u062c',
      signingOut: '\u062c\u0627\u0631\u064a \u062a\u0633\u062c\u064a\u0644 \u0627\u0644\u062e\u0631\u0648\u062c...',
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

  setLanguage(language: LandingLanguage) {
    this.languageChange.emit(language);
  }

  navigateToSection(sectionId: 'home' | 'about' | 'contact'): void {
    const currentPath = this.router.url.split('#')[0];

    if (currentPath !== '/') {
      this.router.navigate(['/']).then(() => this.scrollToSection(sectionId));
      return;
    }

    this.scrollToSection(sectionId);
  }

  signOut(): void {
    if (this.isSigningOut()) {
      return;
    }

    this.isSigningOut.set(true);

    this.authService.logout().subscribe({
      complete: () => this.isSigningOut.set(false)
    });
  }

  private scrollToSection(sectionId: string): void {
    setTimeout(() => {
      document.getElementById(sectionId)?.scrollIntoView({
        behavior: 'smooth',
        block: 'start'
      });
    });
  }
}
