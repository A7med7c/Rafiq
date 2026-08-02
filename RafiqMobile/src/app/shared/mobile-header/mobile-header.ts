import { Component, Input, Output, EventEmitter, inject } from '@angular/core';
import { NotificationService } from '../../Services/notification.service';
import { LocalizationService } from '../../Services/localization.service';

@Component({
  selector: 'app-mobile-header',
  standalone: true,
  imports: [],
  templateUrl: './mobile-header.html',
  styleUrl: './mobile-header.css',
})
export class MobileHeader {
  @Input() title = '';
  @Input() showNotifications = true;

  @Input() showBack = false;

  @Output() notificationsClicked = new EventEmitter<void>();

  @Output() backClicked = new EventEmitter<void>();

  protected readonly notifSvc = inject(NotificationService);
  protected readonly l10n     = inject(LocalizationService);
  protected readonly t        = this.l10n.t;
}
