import { Component, inject } from '@angular/core';
import { Navbar } from './Components/navbar/navbar';
import { Hero } from './Components/hero/hero';
import { Features } from './Components/features/features';
import { About } from './Components/about/about';
import { HowItWorks } from './Components/how-it-works/how-it-works';
import { Stats } from './Components/stats/stats';
import { Testimonials } from './Components/testimonials/testimonials';
import { Contact } from './Components/contact/contact';
import { Footer } from './Components/footer/footer';
import { LocalizationService } from '../../Services/localization.service';

export type LandingLanguage = 'en' | 'ar';

export type LandingSection =
  | 'home'
  | 'about'
  | 'features'
  | 'how-it-works'
  | 'testimonials'
  | 'contact';

@Component({
  selector: 'app-landing',
  imports: [Navbar, Hero, Features, About, HowItWorks, Stats, Testimonials, Contact, Footer],
  templateUrl: './landing.html',
  styleUrl: './landing.css',
})
export class Landing {
  readonly l10n = inject(LocalizationService);
}
