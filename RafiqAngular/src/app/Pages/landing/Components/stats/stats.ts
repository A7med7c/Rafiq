import { Component, Input } from '@angular/core';
import { LandingLanguage } from '../../landing';

@Component({
  selector: 'app-stats',
  imports: [],
  templateUrl: './stats.html',
  styleUrl: './stats.css',
})
export class Stats {
  @Input() language: LandingLanguage = 'en';

  text = {
    en: {
      items: [
        { value: '50K+', label: 'Happy Users' },
        { value: '120K+', label: 'Appointments Booked' },
        { value: '98%', label: 'Satisfaction Rate' },
        { value: '24/7', label: 'AI Support' },
      ],
    },
    ar: {
      items: [
        { value: '50K+', label: 'مستخدم سعيد' },
        { value: '120K+', label: 'موعد محجوز' },
        { value: '98%', label: 'نسبة الرضا' },
        { value: '24/7', label: 'دعم ذكي' },
      ],
    },
  };

  get t() {
    return this.text[this.language];
  }
}
