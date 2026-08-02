import { Component, OnInit, inject, signal } from '@angular/core';
import { CommonModule } from '@angular/common';
import { ActivatedRoute, Router } from '@angular/router';
import { catchError, of } from 'rxjs';
import { LocalizationService } from '../../Services/localization.service';
import { NotificationService } from '../../Services/notification.service';
import { FamilyProfilesService, AccessibleProfileDto } from '../../Services/family-profiles.service';
import { BottomNav } from '../../shared/bottom-nav/bottom-nav';
import { MobileHeader } from '../../shared/mobile-header/mobile-header';
import { getPermissions, setPermissions, FamilyPermissionsPrefs } from './family-permissions-storage';

type PermKey = keyof FamilyPermissionsPrefs;

@Component({
  selector: 'app-family-permissions',
  standalone: true,
  imports: [CommonModule, BottomNav, MobileHeader],
  templateUrl: './family-permissions.html',
  styleUrl: './family-permissions.css',
})
export class FamilyPermissions implements OnInit {
  protected readonly l10n = inject(LocalizationService);
  protected readonly t = this.l10n.t;
  protected readonly notifSvc = inject(NotificationService);
  private readonly fpSvc = inject(FamilyProfilesService);
  private readonly route = inject(ActivatedRoute);
  readonly router = inject(Router);

  readonly profileId = signal<string>('');
  readonly memberName = signal<string>('');
  readonly loading = signal(true);

  readonly canManageMedications = signal(true);
  readonly receiveReminders = signal(true);
  readonly emergencyContactEnabled = signal(false);

  readonly savedTag = signal<PermKey | null>(null);
  private savedTagTimer: ReturnType<typeof setTimeout> | null = null;

  ngOnInit(): void {
    this.route.paramMap.subscribe(params => {
      const id = params.get('id');
      if (!id) return;
      this.profileId.set(id);

      const prefs = getPermissions(id);
      this.canManageMedications.set(prefs.canManageMedications);
      this.receiveReminders.set(prefs.receiveReminders);
      this.emergencyContactEnabled.set(prefs.emergencyContactEnabled);

      this.fpSvc.getAccessible().pipe(catchError(() => of([] as AccessibleProfileDto[]))).subscribe(list => {
        const p = list.find(x => x.userHealthProfileId === id);
        this.memberName.set(p ? `${p.firstName} ${p.lastName}` : '');
        this.loading.set(false);
      });
    });
  }

  private persist(): void {
    const id = this.profileId();
    if (!id) return;
    setPermissions(id, {
      canManageMedications: this.canManageMedications(),
      receiveReminders: this.receiveReminders(),
      emergencyContactEnabled: this.emergencyContactEnabled(),
    });
  }

  private flashSaved(key: PermKey): void {
    this.savedTag.set(key);
    if (this.savedTagTimer) clearTimeout(this.savedTagTimer);
    this.savedTagTimer = setTimeout(() => this.savedTag.set(null), 1500);
  }

  toggleCanManageMedications(): void {
    this.canManageMedications.update(v => !v);
    this.persist();
    this.flashSaved('canManageMedications');
  }

  toggleReceiveReminders(): void {
    this.receiveReminders.update(v => !v);
    this.persist();
    this.flashSaved('receiveReminders');
  }

  toggleEmergencyContactEnabled(): void {
    this.emergencyContactEnabled.update(v => !v);
    this.persist();
    this.flashSaved('emergencyContactEnabled');
  }
}
