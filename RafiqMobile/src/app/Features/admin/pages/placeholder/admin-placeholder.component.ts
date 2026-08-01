import { ChangeDetectionStrategy, Component, computed, inject } from '@angular/core';
import { RouterLink } from '@angular/router';
import { LocalizationService } from '../../../../Services/localization.service';
import { adminCopy } from '../../admin-copy';

@Component({
  selector: 'app-admin-placeholder',
  standalone: true,
  imports: [RouterLink],
  template: `
    <section class="placeholder">
      <span><i class="fa-solid fa-layer-group"></i></span>
      <h1>{{ copy().title }}</h1>
      <p>{{ copy().body }}</p>
      <a routerLink="/admin/dashboard">{{ copy().back }}</a>
    </section>
  `,
  styles: [`
    .placeholder { min-height: calc(100dvh - 124px); display: grid; place-content: center; justify-items: center; gap: 14px; padding: 24px; border-radius: 14px; background: var(--admin-surface); text-align: center; }
    .placeholder span { width: 52px; height: 52px; display: grid; place-items: center; border-radius: 13px; background: var(--admin-accent-soft); color: var(--admin-accent); font-size: 20px; }
    .placeholder h1 { margin: 0; color: var(--admin-ink); font: 750 22px 'Outfit', sans-serif; }
    .placeholder p { max-width: 560px; margin: 0; color: var(--admin-muted); font-size: 13px; line-height: 1.7; }
    .placeholder a { margin-top: 6px; padding: 10px 16px; border-radius: 9px; background: var(--admin-accent); color: white; text-decoration: none; font-size: 12px; font-weight: 700; }
  `],
  changeDetection: ChangeDetectionStrategy.OnPush
})
export class AdminPlaceholderComponent {
  private readonly l10n = inject(LocalizationService);
  readonly copy = computed(() => adminCopy[this.l10n.lang()].placeholder);
}
