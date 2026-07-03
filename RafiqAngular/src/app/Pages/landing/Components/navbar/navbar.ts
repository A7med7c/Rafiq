import { Component, EventEmitter, Input, Output } from '@angular/core';
import { RouterLink } from "@angular/router";
import { LandingLanguage } from '../../landing';

@Component({
  selector: 'app-navbar',
  imports: [RouterLink],
  templateUrl: './navbar.html',
  styleUrl: './navbar.css',
})
export class Navbar {
  @Input() language: LandingLanguage = 'en';
  @Output() languageChange = new EventEmitter<LandingLanguage>();

  text = {
    en: {
      brand: 'Rafiq | رفيق',
      home: 'Home',
      about: 'About',
      contact: 'Contact',
      login: 'Login',
      cta: 'Get Started',
    },
    ar: {
      brand: 'رفيق | Rafiq',
      home: 'الرئيسية',
      about: 'من نحن',
      contact: 'تواصل معنا',
      login: 'تسجيل الدخول',
      cta: 'ابدأ الآن',
    },
  };

  get t() {
    return this.text[this.language];
  }

  setLanguage(language: LandingLanguage) {
    this.languageChange.emit(language);
  }
}
