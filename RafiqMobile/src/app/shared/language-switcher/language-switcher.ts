import { Component, HostListener, inject, signal } from '@angular/core';
import { AppLanguage, LocalizationService } from '../../Services/localization.service';

@Component({
  selector: 'app-language-switcher',
  standalone: true,
  imports: [],
  templateUrl: './language-switcher.html',
  styleUrl: './language-switcher.css',
})
export class LanguageSwitcher {
  protected readonly l10n = inject(LocalizationService);
  protected readonly t = this.l10n.t;

  protected readonly open = signal(false);

  protected readonly languages: { code: AppLanguage; nativeLabel: string }[] = [
    { code: 'en', nativeLabel: 'English' },
    { code: 'ar', nativeLabel: 'العربية' },
  ];

  toggleMenu(): void {
    this.open.update((v) => !v);
  }

  closeMenu(): void {
    this.open.set(false);
  }

  select(lang: AppLanguage): void {
    this.l10n.setLanguage(lang);
    this.closeMenu();
  }

  @HostListener('document:keydown.escape')
  onEscape(): void {
    this.closeMenu();
  }
}
