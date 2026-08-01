import { Component, input } from '@angular/core';
import { RouterLink } from '@angular/router';
import { inject } from '@angular/core';
import { LocalizationService } from '../../Services/localization.service';
import { AccessibleProfileDto } from '../../Services/family-profiles.service';

@Component({
  selector: 'app-family-profile-banner',
  standalone: true,
  imports: [RouterLink],
  templateUrl: './family-profile-banner.html',
  styleUrl: './family-profile-banner.css',
})
export class FamilyProfileBannerComponent {
  protected readonly l10n = inject(LocalizationService);
  protected readonly t = this.l10n.t;

  readonly profile  = input.required<AccessibleProfileDto | null>();
  readonly readOnly = input<boolean>(false);
}
