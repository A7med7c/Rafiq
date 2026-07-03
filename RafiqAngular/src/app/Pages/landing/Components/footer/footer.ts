import { Component, Input } from '@angular/core';
import { LandingLanguage } from '../../landing';

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
      headline: 'Ready to Transform Your Healthcare?',
      subhead: 'Store, analyze and manage your medical records with AI.',
      cta: 'Get Started',
      brand: 'Rafiq',
      brandText: 'AI Healthcare Companion helping you manage your health smarter and faster.',
      quickLinks: 'Quick Links',
      services: 'Services',
      contact: 'Contact',
      links: ['Home', 'About', 'Features', 'Contact'],
      serviceLinks: ['Medical Records', 'Medication Reminder', 'Family Health'],
      copyright: '© 2026 Rafiq. All Rights Reserved.',
    },
    ar: {
      headline: 'جاهز لتطوير رعايتك الصحية؟',
      subhead: 'احفظ وحلل وأدر سجلاتك الطبية بالذكاء الاصطناعي.',
      cta: 'ابدأ الآن',
      brand: 'رفيق',
      brandText: 'رفيقك الصحي الذكي يساعدك على إدارة صحتك بشكل أسرع وأسهل.',
      quickLinks: 'روابط سريعة',
      services: 'الخدمات',
      contact: 'تواصل معنا',
      links: ['الرئيسية', 'من نحن', 'المميزات', 'تواصل معنا'],
      serviceLinks: ['السجلات الطبية', 'تذكير الأدوية', 'صحة العائلة'],
      copyright: '© 2026 رفيق. جميع الحقوق محفوظة.',
    },
  };

  get t() {
    return this.text[this.language];
  }
}
