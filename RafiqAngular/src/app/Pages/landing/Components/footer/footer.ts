import { Component, Input } from '@angular/core';
import { LandingLanguage, LandingSection } from '../../landing';

@Component({
  selector: 'app-footer',
  imports: [],
  templateUrl: './footer.html',
  styleUrl: './footer.css',
})
export class Footer {
  @Input() language: LandingLanguage = 'en';

  text = {
    en: {
      brand: 'Rafiq | \u0631\u0641\u064a\u0642',
      brandText: 'Your AI healthcare companion, available 24/7 to support you and your family.',
      quickLinks: 'Quick Links',
      services: 'Services',
      support: 'Support',
      download: 'Download the App',
      quickLinkItems: [
        { label: 'Home', section: 'home' as LandingSection },
        { label: 'About', section: 'about' as LandingSection },
        { label: 'Features', section: 'features' as LandingSection },
        { label: 'Contact', section: 'contact' as LandingSection },
      ],
      serviceLinks: ['Appointments', 'Health Records', 'Medications', 'AI Assistant', 'Health Analytics'],
      supportLinks: ['Help Center', 'Privacy Policy', 'Terms of Service', 'FAQ'],
      appStoreSmall: 'Download on the',
      appStore: 'App Store',
      playStoreSmall: 'GET IT ON',
      playStore: 'Google Play',
      copyright: '\u00a9 2024 Rafiq. All rights reserved.',
    },
    ar: {
      brand: 'Rafiq | \u0631\u0641\u064a\u0642',
      brandText: 'رفيقك الصحي الذكي، متاح على مدار الساعة لدعمك أنت وعائلتك.',
      quickLinks: '\u0631\u0648\u0627\u0628\u0637 \u0633\u0631\u064a\u0639\u0629',
      services: '\u0627\u0644\u062e\u062f\u0645\u0627\u062a',
      support: '\u0627\u0644\u062f\u0639\u0645',
      download: '\u062d\u0645\u0651\u0644 \u0627\u0644\u062a\u0637\u0628\u064a\u0642',
      quickLinkItems: [
        { label: '\u0627\u0644\u0631\u0626\u064a\u0633\u064a\u0629', section: 'home' as LandingSection },
        { label: '\u0639\u0646 \u0631\u0641\u064a\u0642', section: 'about' as LandingSection },
        { label: '\u0627\u0644\u0645\u0645\u064a\u0632\u0627\u062a', section: 'features' as LandingSection },
        { label: '\u062a\u0648\u0627\u0635\u0644', section: 'contact' as LandingSection },
      ],
      serviceLinks: ['\u0627\u0644\u0645\u0648\u0627\u0639\u064a\u062f', '\u0627\u0644\u0633\u062c\u0644\u0627\u062a \u0627\u0644\u0635\u062d\u064a\u0629', '\u0627\u0644\u0623\u062f\u0648\u064a\u0629', '\u0627\u0644\u0645\u0633\u0627\u0639\u062f \u0627\u0644\u0630\u0643\u064a', '\u0627\u0644\u062a\u062d\u0644\u064a\u0644\u0627\u062a \u0627\u0644\u0635\u062d\u064a\u0629'],
      supportLinks: ['\u0645\u0631\u0643\u0632 \u0627\u0644\u0645\u0633\u0627\u0639\u062f\u0629', '\u0633\u064a\u0627\u0633\u0629 \u0627\u0644\u062e\u0635\u0648\u0635\u064a\u0629', '\u0634\u0631\u0648\u0637 \u0627\u0644\u062e\u062f\u0645\u0629', '\u0627\u0644\u0623\u0633\u0626\u0644\u0629 \u0627\u0644\u0634\u0627\u0626\u0639\u0629'],
      appStoreSmall: '\u062d\u0645\u0651\u0644 \u0645\u0646',
      appStore: 'App Store',
      playStoreSmall: '\u0627\u062d\u0635\u0644 \u0639\u0644\u0649',
      playStore: 'Google Play',
      copyright: '\u00a9 2024 \u0631\u0641\u064a\u0642. \u062c\u0645\u064a\u0639 \u0627\u0644\u062d\u0642\u0648\u0642 \u0645\u062d\u0641\u0648\u0638\u0629.',
    },
  };

  get t() {
    return this.text[this.language];
  }

  scrollToSection(sectionId: LandingSection): void {
    document.getElementById(sectionId)?.scrollIntoView({
      behavior: 'smooth',
      block: 'start',
    });
  }
}
