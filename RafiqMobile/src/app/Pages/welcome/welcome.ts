import { Component, OnInit, OnDestroy, Renderer2, inject } from '@angular/core';
import { CommonModule } from '@angular/common';
import { Router } from '@angular/router';
import { LocalizationService } from '../../Services/localization.service';

export interface FloatingIcon {
  class: string;
  posClass: string;
  shapeClass: string;
  colorClass: string;
  subClass?: string;
}

export interface WelcomeStep {
  title: string;
  text: string;
  img?: string;
  icon?: string;
  isImage: boolean;
  buttonText: string;
  floatingIcons: FloatingIcon[];
}

@Component({
  selector: 'app-welcome',
  standalone: true,
  imports: [CommonModule],
  templateUrl: './welcome.html',
  styleUrl: './welcome.css',
})
export class Welcome implements OnInit, OnDestroy {
  currentStep = 0;

  localization = inject(LocalizationService);
  private router = inject(Router);
  private renderer = inject(Renderer2);

  get steps(): WelcomeStep[] {
    const t = this.localization.t().welcome;
    return [
      {
        title: t.step1Title,
        text: t.step1Text,
        img: '/images/RafiqLogo.png',
        isImage: true,
        buttonText: t.next,
        floatingIcons: [
          { class: 'fa-solid fa-heart', posClass: 'pos-top-left', shapeClass: 'shape-circle', colorClass: 'color-purple' },
          { class: 'fa-solid fa-file-waveform', posClass: 'pos-top-right', shapeClass: 'shape-rect', colorClass: 'color-blue' },
          { class: 'fa-solid fa-microchip', posClass: 'pos-mid-left', shapeClass: 'shape-rect', colorClass: 'color-blue' },
          { class: 'fa-regular fa-calendar-days', posClass: 'pos-mid-right', shapeClass: 'shape-rect', colorClass: 'color-blue' },
          { class: 'fa-solid fa-capsules', posClass: 'pos-bottom-right', shapeClass: 'shape-circle', colorClass: 'color-blue' }
        ]
      },
      {
        title: t.step2Title,
        text: t.step2Text,
        img: '/images/RafiqLogo.png',
        isImage: true,
        buttonText: t.next,
        floatingIcons: [
          { class: 'fa-solid fa-lock', posClass: 'pos-top-left', shapeClass: 'shape-circle', colorClass: 'color-blue' },
          { class: 'fa-solid fa-shield', subClass: 'fa-solid fa-users', posClass: 'pos-big-shield', shapeClass: 'shape-shield', colorClass: 'color-blue' },
          { class: 'fa-solid fa-check', posClass: 'pos-shield-check', shapeClass: 'shape-circle-small', colorClass: 'color-blue' }
        ]
      },
      {
        title: t.step3Title,
        text: t.step3Text,
        img: '/images/RafiqLogo.png',
        isImage: true,
        buttonText: t.next,
        floatingIcons: [
          { class: 'fa-solid fa-folder', posClass: 'pos-top-left', shapeClass: 'shape-rect', colorClass: 'color-blue' },
          { class: 'fa-solid fa-bell', posClass: 'pos-top-right', shapeClass: 'shape-circle', colorClass: 'color-blue' },
          { class: 'fa-solid fa-magnifying-glass-chart', posClass: 'pos-mid-left', shapeClass: 'shape-circle', colorClass: 'color-blue' },
          { class: 'fa-solid fa-cloud', posClass: 'pos-bottom-right', shapeClass: 'shape-rect', colorClass: 'color-blue' }
        ]
      },
      {
        title: t.step4Title,
        text: t.step4Text,
        img: '/images/RafiqLogo.png',
        isImage: true,
        buttonText: t.getStarted,
        floatingIcons: [
          { class: 'fa-solid fa-user-tie', posClass: 'pos-top', shapeClass: 'shape-circle', colorClass: 'color-blue' },
          { class: 'fa-solid fa-user-nurse', posClass: 'pos-mid-left', shapeClass: 'shape-circle', colorClass: 'color-blue' },
          { class: 'fa-solid fa-user-doctor', posClass: 'pos-mid-right', shapeClass: 'shape-circle', colorClass: 'color-blue' },
          { class: 'fa-solid fa-user', posClass: 'pos-bottom-right', shapeClass: 'shape-circle', colorClass: 'color-blue' },
          { class: 'fa-solid fa-heart', posClass: 'pos-bottom', shapeClass: 'shape-circle-small', colorClass: 'color-purple' }
        ]
      }
    ];
  }

  ngOnInit() {
    this.renderer.addClass(document.body, 'hide-floating-elements');
  }

  ngOnDestroy() {
    this.renderer.removeClass(document.body, 'hide-floating-elements');
  }

  nextStep() {
    if (this.currentStep < this.steps.length - 1) {
      this.currentStep++;
    } else {
      this.finish();
    }
  }

  prevStep() {
    if (this.currentStep > 0) {
      this.currentStep--;
    }
  }

  finish() {
    this.router.navigate(['/register']);
  }
}
