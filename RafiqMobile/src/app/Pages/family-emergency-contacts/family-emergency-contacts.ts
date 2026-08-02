import { Component, OnInit, inject, signal } from '@angular/core';
import { CommonModule } from '@angular/common';
import { ActivatedRoute, Router } from '@angular/router';
import { catchError, of } from 'rxjs';
import { LocalizationService } from '../../Services/localization.service';
import { NotificationService } from '../../Services/notification.service';
import { FamilyProfilesService, AccessibleProfileDto } from '../../Services/family-profiles.service';
import { BottomNav } from '../../shared/bottom-nav/bottom-nav';
import { MobileHeader } from '../../shared/mobile-header/mobile-header';

@Component({
  selector: 'app-family-emergency-contacts',
  standalone: true,
  imports: [CommonModule, BottomNav, MobileHeader],
  templateUrl: './family-emergency-contacts.html',
  styleUrl: './family-emergency-contacts.css',
})
export class FamilyEmergencyContacts implements OnInit {
  protected readonly l10n = inject(LocalizationService);
  protected readonly t = this.l10n.t;
  protected readonly notifSvc = inject(NotificationService);
  private readonly fpSvc = inject(FamilyProfilesService);
  private readonly route = inject(ActivatedRoute);
  readonly router = inject(Router);

  readonly profileId = signal<string>('');
  readonly memberName = signal<string>('');

  ngOnInit(): void {
    this.route.paramMap.subscribe(params => {
      const id = params.get('id');
      if (!id) return;
      this.profileId.set(id);
      this.fpSvc.getAccessible().pipe(catchError(() => of([] as AccessibleProfileDto[]))).subscribe(list => {
        const p = list.find(x => x.userHealthProfileId === id);
        this.memberName.set(p ? `${p.firstName} ${p.lastName}` : '');
      });
    });
  }
}
