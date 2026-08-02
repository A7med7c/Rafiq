import { Component, inject, Input } from '@angular/core';
import { LocalizationService } from '../../../Services/localization.service';

@Component({
  selector: 'app-auth-hero',
  standalone: true,
  templateUrl: './auth-hero.html',
  styleUrls: ['./auth-hero.css']
})
export class AuthHero {
  @Input() compact = false;

  protected readonly l10n = inject(LocalizationService);
  protected readonly t = this.l10n.t;
}
