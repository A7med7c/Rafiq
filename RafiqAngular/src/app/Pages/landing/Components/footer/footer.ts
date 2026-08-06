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
      brand: 'Rafiq | رفيق',
      brandText: 'Your AI healthcare companion, available 24/7 to support you and your family.',
      quickLinks: 'Quick Links',
      services: 'Services',
      support: 'Support',
      download: 'Download the App',
      quickLinkItems: [
        { label: 'Home', section: 'home' as LandingSection },
        { label: 'About', section: 'about' as LandingSection },
        { label: 'Features', section: 'features' as LandingSection },
        // { label: 'Contact', section: 'contact' as LandingSection },
      ],
      serviceLinks: ['Appointments', 'Health Records', 'Medications', 'AI Assistant', 'Health Analytics'],
      supportLinks: ['Help Center', 'Privacy Policy', 'Terms of Service', 'FAQ'],
      appStoreSmall: 'Download on the',
      appStore: 'App Store',
      playStoreSmall: 'GET IT ON',
      playStore: 'Google Play',
      copyright: '© 2026 Rafiq. All rights reserved.',
    },
    ar: {
      brand: 'رفيق | Rafiq',
      brandText: 'مساعدك الصحي الذكي، متاح 24/7 لدعمك ودعم عيلتك.',
      quickLinks: 'روابط سريعة',
      services: 'الخدمات',
      support: 'الدعم',
      download: 'نزل التطبيق',
      quickLinkItems: [
        { label: 'الرئيسية', section: 'home' as LandingSection },
        { label: 'عن رفيق', section: 'about' as LandingSection },
        { label: 'المميزات', section: 'features' as LandingSection },
        { label: 'تواصل معنا', section: 'contact' as LandingSection },
      ],
      serviceLinks: ['المواعيد', 'السجلات الطبية', 'الأدوية', 'المساعد الذكي', 'التحليلات الصحية'],
      supportLinks: ['مركز المساعدة', 'سياسة الخصوصية', 'شروط الخدمة', 'الأسئلة الشائعة'],
      appStoreSmall: 'حمل من',
      appStore: 'App Store',
      playStoreSmall: 'احصل عليه من',
      playStore: 'Google Play',
      copyright: '© 2024 رفيق. جميع الحقوق محفوظة.',
    },
  };

  get t() {
    return this.text[this.language];
  }

  scrollToSection(sectionId: LandingSection): void {
    const el = document.getElementById(sectionId);
    if (!el) return;

    const navbarEl = document.querySelector<HTMLElement>('.navbar');
    const navbarHeight = navbarEl?.getBoundingClientRect().height ?? 0;
    const top = el.getBoundingClientRect().top + window.scrollY - navbarHeight;

    window.scrollTo({ top, behavior: 'smooth' });
  }
}
