import { Component } from '@angular/core';
import { Navbar } from './Components/navbar/navbar';
import { Hero } from './Components/hero/hero';
import { Features } from './Components/features/features';
import { About } from './Components/about/about';
import { HowItWorks } from './Components/how-it-works/how-it-works';
import { Stats } from './Components/stats/stats';
import { Testimonials } from './Components/testimonials/testimonials';
import { Contact } from './Components/contact/contact';
import { Footer } from './Components/footer/footer';

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
  language: LandingLanguage = 'en';

  setLanguage(language: LandingLanguage) {
    this.language = language;
  }
}
