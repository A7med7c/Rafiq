import { Component } from '@angular/core';
import { Navbar } from "./Components/navbar/navbar";
import { Hero } from "./Components/hero/hero";
import { Features } from "./Components/features/features";
import { Footer } from "./Components/footer/footer";

export type LandingLanguage = 'en' | 'ar';

@Component({
  selector: 'app-landing',
  imports: [Navbar, Hero, Features, Footer],
  templateUrl: './landing.html',
  styleUrl: './landing.css',
})
export class Landing {
  language: LandingLanguage = 'en';

  setLanguage(language: LandingLanguage) {
    this.language = language;
  }
}
