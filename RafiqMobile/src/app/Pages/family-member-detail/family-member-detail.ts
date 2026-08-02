import { Component, OnInit, computed, inject, signal } from '@angular/core';
import { CommonModule } from '@angular/common';
import { ActivatedRoute, Router, RouterLink } from '@angular/router';
import { catchError, of } from 'rxjs';
import { LocalizationService } from '../../Services/localization.service';
import { NotificationService } from '../../Services/notification.service';
import {
  FamilyProfilesService,
  AccessibleProfileDto,
  PatientProfileDetailDto,
} from '../../Services/family-profiles.service';
import { environment } from '../../Environments/Environment';
import { BottomNav } from '../../shared/bottom-nav/bottom-nav';
import { MobileHeader } from '../../shared/mobile-header/mobile-header';
import { getPermissions, FamilyPermissionsPrefs } from '../family-permissions/family-permissions-storage';

@Component({
  selector: 'app-family-member-detail',
  standalone: true,
  imports: [CommonModule, RouterLink, BottomNav, MobileHeader],
  templateUrl: './family-member-detail.html',
  styleUrl: './family-member-detail.css',
})
export class FamilyMemberDetail implements OnInit {
  protected readonly l10n = inject(LocalizationService);
  protected readonly t = this.l10n.t;
  protected readonly notifSvc = inject(NotificationService);
  private readonly fpSvc = inject(FamilyProfilesService);
  private readonly route = inject(ActivatedRoute);
  readonly router = inject(Router);

  readonly profileId = signal<string>('');
  readonly profile = signal<AccessibleProfileDto | null>(null);
  readonly detail = signal<PatientProfileDetailDto | null>(null);
  readonly loading = signal(true);
  readonly notFound = signal(false);

  readonly permissions = computed<FamilyPermissionsPrefs>(() => {
    const id = this.profileId();
    return id ? getPermissions(id) : { canManageMedications: true, receiveReminders: true, emergencyContactEnabled: false };
  });

  readonly showRemoveConfirm = signal(false);
  readonly removing = signal(false);
  readonly removeError = signal('');

  readonly canRemove = computed(() => {
    const p = this.profile();
    if (!p) return false;
    return !p.isSelf;
  });

  readonly removeType = computed<'managed' | 'self' | null>(() => {
    const p = this.profile();
    if (!p || p.isSelf) return null;
    if (p.profileType === 'Managed' && p.userId === null && p.accessRole === 'Owner') return 'managed';
    if (p.profileType !== 'Managed') return 'self';
    return null;
  });

  ngOnInit(): void {
    this.route.paramMap.subscribe(params => {
      const id = params.get('id');
      if (!id) { this.notFound.set(true); this.loading.set(false); return; }
      this.profileId.set(id);
      this.load(id);
    });
  }

  private load(id: string): void {
    this.loading.set(true);
    this.notFound.set(false);
    this.fpSvc.getAccessible().pipe(catchError(() => of([] as AccessibleProfileDto[]))).subscribe(list => {
      const found = list.find(p => p.userHealthProfileId === id) ?? null;
      this.profile.set(found);
      if (!found) {
        this.notFound.set(true);
        this.loading.set(false);
        return;
      }
      this.fpSvc.getById(id).pipe(catchError(() => of(null))).subscribe(d => {
        this.detail.set(d);
        this.loading.set(false);
      });
    });
  }

  private resolveProfileImageUrl(path: string | null | undefined): string | null {
    return path ? `${environment.fileBaseUrl}${path}` : null;
  }

  avatarUrl(): string | null {
    return this.resolveProfileImageUrl(this.profile()?.profileImageUrl ?? null);
  }

  getAge(dob: string | null | undefined): number | null {
    if (!dob) return null;
    const b = new Date(dob);
    const n = new Date();
    let age = n.getFullYear() - b.getFullYear();
    if (n.getMonth() < b.getMonth() || (n.getMonth() === b.getMonth() && n.getDate() < b.getDate())) age--;
    return age;
  }

  getRelLabel(p: AccessibleProfileDto): string {
    if (p.isSelf) return this.t().family.self;
    const key = (p.relationship ?? 'member').toLowerCase();
    return (this.t().family as any)[key] ?? this.t().family.member;
  }

  fullName(): string {
    const p = this.profile();
    return p ? `${p.firstName} ${p.lastName}` : '';
  }

  editProfile(): void {
    this.router.navigate(['/family-profiles'], { queryParams: { editId: this.profileId() } });
  }

  openRemoveConfirm(): void {
    this.removeError.set('');
    this.showRemoveConfirm.set(true);
  }

  closeRemoveConfirm(): void {
    this.showRemoveConfirm.set(false);
  }

  confirmRemove(): void {
    const type = this.removeType();
    const id = this.profileId();
    if (!type || !id) return;
    this.removing.set(true);
    this.removeError.set('');
    const action$ = type === 'managed' ? this.fpSvc.deleteProfile(id) : this.fpSvc.leaveProfile(id);
    action$.pipe(catchError(() => of(null))).subscribe(result => {
      this.removing.set(false);
      if (result === null) {
        this.removeError.set(this.t().family.removeMemberError);
        return;
      }
      this.router.navigate(['/family-profiles']);
    });
  }
}
