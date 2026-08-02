import { Component, inject } from '@angular/core';
import { RouterLink, RouterLinkActive } from '@angular/router';
import { LocalizationService } from '../../Services/localization.service';
import { AiChatService } from '../../Services/ai-chat.service';

@Component({
  selector: 'app-bottom-nav',
  standalone: true,
  imports: [RouterLink, RouterLinkActive],
  templateUrl: './bottom-nav.html',
  styleUrl: './bottom-nav.css',
})
export class BottomNav {
  protected readonly l10n = inject(LocalizationService);
  protected readonly t    = this.l10n.t;
  protected readonly aiChatService = inject(AiChatService);
}
