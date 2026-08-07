import { Injectable, signal, computed, effect } from '@angular/core';
import { en } from '../i18n/en';
import { ar } from '../i18n/ar';

export type AppLanguage = 'en' | 'ar';

const STORAGE_KEY = 'rafiq_lang';

@Injectable({ providedIn: 'root' })
export class LocalizationService {
  private readonly _lang = signal<AppLanguage>(
    (localStorage.getItem(STORAGE_KEY) as AppLanguage) ?? 'ar'
  );

  readonly lang = this._lang.asReadonly();
  readonly t = computed(() => (this._lang() === 'ar' ? ar : en));
  readonly dir = computed(() => (this._lang() === 'ar' ? 'rtl' : 'ltr'));
  readonly isRtl = computed(() => this._lang() === 'ar');

  constructor() {
    effect(() => {
      const lang = this._lang();
      const dir = lang === 'ar' ? 'rtl' : 'ltr';
      document.documentElement.dir = dir;
      document.documentElement.lang = lang;
    });
  }

  setLanguage(lang: AppLanguage): void {
    this._lang.set(lang);
    localStorage.setItem(STORAGE_KEY, lang);
  }

  toggle(): void {
    this.setLanguage(this._lang() === 'en' ? 'ar' : 'en');
  }
}
