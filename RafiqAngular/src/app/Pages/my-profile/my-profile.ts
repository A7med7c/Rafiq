import {
  Component, OnInit, inject, signal, computed,
  HostListener, ElementRef
} from '@angular/core';
import { CommonModule } from '@angular/common';
import { FormsModule } from '@angular/forms';
import { Router, RouterLink, RouterLinkActive } from '@angular/router';
import { AuthService } from '../../Services/auth-service';
import { HealthProfileService, PatientProfileResponse } from '../../Services/health-profile.service';
import { EmergencyContactService, EmergencyContactResponse } from '../../Services/emergency-contact.service';
import { NotificationService } from '../../Services/notification.service';
import { LocalizationService } from '../../Services/localization.service';
import { HttpClient } from '@angular/common/http';
import { environment } from '../../Environments/Environment';
import { ApiResponse } from '../../Modles/api-response';
import { map, switchMap } from 'rxjs';

interface UpdateProfileBody {
  patientProfileId: string;
  firstName: string;
  lastName: string;
  dateOfBirth: string;
  gender: string;
  bloodType: string | null;
  height: number | null;
  weight: number | null;
  relationship: string | null;
}

@Component({
  selector: 'app-my-profile',
  standalone: true,
  imports: [CommonModule, FormsModule, RouterLink, RouterLinkActive],
  templateUrl: './my-profile.html',
  styleUrl: './my-profile.css',
})
export class MyProfile implements OnInit {
  protected readonly authService      = inject(AuthService);
  private readonly healthSvc          = inject(HealthProfileService);
  private readonly emergencySvc       = inject(EmergencyContactService);
  protected readonly notifService     = inject(NotificationService);
  protected readonly l10n             = inject(LocalizationService);
  protected readonly t                = this.l10n.t;
  private readonly router             = inject(Router);
  private readonly elRef              = inject(ElementRef);
  private readonly http               = inject(HttpClient);

  // ── Sidebar / Header state ────────────────────────────────────────────────
  readonly sidebarCollapsed  = signal(false);
  readonly mobileSidebarOpen = signal(false);
  readonly dropdownOpen      = signal(false);
  readonly unreadNotifCount  = this.notifService.unreadCount;

  // ── Profile data ─────────────────────────────────────────────────────────
  readonly profile        = signal<PatientProfileResponse | null>(null);
  readonly profileLoading = signal(true);
  readonly profileId      = signal<string | null>(null);

  // ── Emergency contacts ────────────────────────────────────────────────────
  readonly contacts        = signal<EmergencyContactResponse[]>([]);
  readonly contactsLoading = signal(true);

  // ── Edit: Personal Info ───────────────────────────────────────────────────
  readonly editingPersonal  = signal(false);
  readonly verifyingEmail   = signal(false);
  readonly pendingEmail     = signal('');
  readonly emailOtpSaving   = signal(false);
  readonly emailOtpResending = signal(false);
  personalForm = { firstName: '', lastName: '', dateOfBirth: '', gender: '', phoneNumber: '', email: '' };
  emailOtpCode = '';
  readonly personalSaving = signal(false);

  // ── Edit: Health Info ─────────────────────────────────────────────────────
  readonly editingHealth = signal(false);
  healthForm = { bloodType: '', height: null as number | null, weight: null as number | null };
  readonly healthSaving = signal(false);

  // ── Allergies ────────────────────────────────────────────────────────────
  readonly addingAllergy    = signal(false);
  newAllergy = { name: '', severity: 'Mild' };
  readonly allergySaving    = signal(false);

  // ── Chronic Diseases ──────────────────────────────────────────────────────
  readonly addingDisease    = signal(false);
  newDisease = { name: '', status: 'Active', diagnosedAt: '' };
  readonly diseaseSaving    = signal(false);

  // ── Emergency Contact form ────────────────────────────────────────────────
  readonly addingContact    = signal(false);
  newContact = { name: '', phoneNumber: '', relation: '' };
  readonly contactSaving    = signal(false);

  // ── Delete account modal ──────────────────────────────────────────────────
  readonly deleteModalOpen  = signal(false);
  readonly deleteLoading    = signal(false);

  // ── Computed helpers ──────────────────────────────────────────────────────
  get displayName(): string {
    const u = this.authService.currentUser;
    if (!u) return '';
    return `${u.firstName ?? ''} ${u.lastName ?? ''}`.trim() || u.email;
  }

  get userEmail(): string {
    return this.authService.currentUser?.email ?? '';
  }

  get avatarUrl(): string {
    return this.authService.avatarUrl;
  }

  readonly genderLabel = computed(() => {
    const p = this.profile();
    if (!p) return '-';
    return p.gender === '1' || p.gender?.toLowerCase() === 'male' ? 'Male' : 'Female';
  });

  readonly bloodTypeLabel = computed(() => {
    const map: Record<string, string> = {
      '1': 'A+', '2': 'A-', '3': 'B+', '4': 'B-',
      '5': 'AB+', '6': 'AB-', '7': 'O+', '8': 'O-',
      'APositive': 'A+', 'ANegative': 'A-',
      'BPositive': 'B+', 'BNegative': 'B-',
      'ABPositive': 'AB+', 'ABNegative': 'AB-',
      'OPositive': 'O+', 'ONegative': 'O-',
    };
    const bt = this.profile()?.bloodType ?? '';
    return map[bt] ?? bt ?? '-';
  });

  formatDob(dob: string | null | undefined): string {
    if (!dob) return '-';
    const d = new Date(dob);
    return d.toLocaleDateString('en-US', { year: 'numeric', month: 'long', day: 'numeric' });
  }

  getAge(dob: string | null | undefined): number {
    if (!dob) return 0;
    const today = new Date();
    const birth = new Date(dob);
    let age = today.getFullYear() - birth.getFullYear();
    const m = today.getMonth() - birth.getMonth();
    if (m < 0 || (m === 0 && today.getDate() < birth.getDate())) age--;
    return age;
  }

  // ── Lifecycle ─────────────────────────────────────────────────────────────
  ngOnInit(): void {
    this.applyResponsiveSidebar();
    this.loadProfile();
    this.loadContacts();
  }

  @HostListener('window:resize')
  onWindowResize(): void {
    this.applyResponsiveSidebar();
  }

  @HostListener('document:click', ['$event'])
  onDocumentClick(event: MouseEvent): void {
    if (this.dropdownOpen() && !this.elRef.nativeElement.contains(event.target)) {
      this.dropdownOpen.set(false);
    }
  }

  private applyResponsiveSidebar(): void {
    this.sidebarCollapsed.set(window.innerWidth <= 1024);
    if (window.innerWidth > 768) {
      this.mobileSidebarOpen.set(false);
    }
  }

  private loadProfile(): void {
    this.profileLoading.set(true);
    this.healthSvc.getMyProfile().subscribe({
      next: (res) => {
        this.profile.set(res.data);
        this.profileId.set(res.data?.id ?? null);
        this.profileLoading.set(false);
      },
      error: () => { this.profileLoading.set(false); }
    });
  }

  private loadContacts(): void {
    this.contactsLoading.set(true);
    this.emergencySvc.getEmergencyContacts().subscribe({
      next: (res) => { this.contacts.set(res.data ?? []); this.contactsLoading.set(false); },
      error: () => { this.contactsLoading.set(false); }
    });
  }

  // ── Sidebar / Dropdown ────────────────────────────────────────────────────
  toggleSidebar(): void { this.sidebarCollapsed.update(v => !v); }
  toggleMobileSidebar(): void { this.mobileSidebarOpen.update(v => !v); }
  toggleDropdown(): void { this.dropdownOpen.update(v => !v); }

  goToMyProfile(): void {
    this.dropdownOpen.set(false);
    this.router.navigate(['/my-profile']);
  }

  logout(): void {
    this.dropdownOpen.set(false);
    this.authService.logout().subscribe();
  }

  // ── Edit Personal Info ────────────────────────────────────────────────────
  openEditPersonal(): void {
    const p = this.profile();
    const u = this.authService.currentUser;
    this.personalForm = {
      firstName:   u?.firstName ?? '',
      lastName:    u?.lastName ?? '',
      dateOfBirth: p?.dateOfBirth ? p.dateOfBirth.slice(0, 10) : '',
      gender:      p?.gender ?? '',
      phoneNumber: u?.phoneNumber ?? '',
      email:       u?.email ?? '',
    };
    this.editingPersonal.set(true);
  }

  cancelEditPersonal(): void { this.editingPersonal.set(false); }

  savePersonal(): void {
    const id = this.profileId();
    if (!id) return;
    this.personalSaving.set(true);

    const u = this.authService.currentUser;
    const currentEmail = u?.email ?? '';
    const currentPhone = u?.phoneNumber ?? '';
    const emailChanged = this.personalForm.email.trim().toLowerCase() !== currentEmail.toLowerCase();
    const phoneOrNameChanged =
      this.personalForm.firstName.trim() !== (u?.firstName ?? '') ||
      this.personalForm.lastName.trim()  !== (u?.lastName ?? '')  ||
      this.personalForm.phoneNumber.trim() !== currentPhone;

    const profileBody: UpdateProfileBody = {
      patientProfileId: id,
      firstName:    this.personalForm.firstName,
      lastName:     this.personalForm.lastName,
      dateOfBirth:  this.personalForm.dateOfBirth,
      gender:       this.personalForm.gender,
      bloodType:    this.profile()?.bloodType ?? null,
      height:       this.profile()?.height ?? null,
      weight:       this.profile()?.weight ?? null,
      relationship: null,
    };

    // Build the base observable: if name/phone changed, call updateAccount first,
    // then update patient profile. Otherwise just update patient profile directly.
    const base$ = phoneOrNameChanged
      ? this.authService.updateAccount(
          this.personalForm.firstName,
          this.personalForm.lastName,
          this.personalForm.phoneNumber
        ).pipe(
          switchMap(() => this.http.put<ApiResponse<PatientProfileResponse>>(
            `${environment.apiUrl}/patient-profiles/${id}`, profileBody
          ))
        )
      : this.http.put<ApiResponse<PatientProfileResponse>>(
          `${environment.apiUrl}/patient-profiles/${id}`, profileBody
        );

    base$.subscribe({
      next: (res) => {
        this.profile.set(res.data);

        if (emailChanged) {
          this.authService.requestEmailChange(this.personalForm.email.trim()).subscribe({
            next: () => {
              this.personalSaving.set(false);
              this.editingPersonal.set(false);
              this.pendingEmail.set(this.personalForm.email.trim());
              this.emailOtpCode = '';
              this.verifyingEmail.set(true);
            },
            error: (err) => {
              this.personalSaving.set(false);
              const msg = err?.error?.message ?? 'Failed to request email change.';
              this.notifService.showToast('Email Update', msg, 'error');
            }
          });
        } else {
          this.personalSaving.set(false);
          this.editingPersonal.set(false);
          this.notifService.showToast('Saved', 'Personal info updated.', 'success');
        }
      },
      error: (err) => {
        this.personalSaving.set(false);
        const msg = err?.error?.message ?? 'Failed to save changes.';
        this.notifService.showToast('Error', msg, 'error');
      }
    });
  }

  // ── Email OTP Verification ────────────────────────────────────────────────
  verifyEmailOtp(): void {
    this.emailOtpSaving.set(true);
    this.authService.verifyAccount(this.pendingEmail(), this.emailOtpCode).subscribe({
      next: () => {
        // Refresh the user object first, then update signals so change detection
        // runs after currentUser already has the new email.
        this.authService.getMe().subscribe({
          next: () => {
            this.emailOtpSaving.set(false);
            this.verifyingEmail.set(false);
            this.notifService.showToast('Email Updated', 'Your email has been verified and updated.', 'success');
          },
          error: () => {
            this.emailOtpSaving.set(false);
            this.verifyingEmail.set(false);
          }
        });
      },
      error: (err) => {
        this.emailOtpSaving.set(false);
        const msg = err?.error?.message ?? 'Invalid or expired code.';
        this.notifService.showToast('Verification Failed', msg, 'error');
      }
    });
  }

  resendEmailOtp(): void {
    this.emailOtpResending.set(true);
    this.authService.resendOtp(this.pendingEmail()).subscribe({
      next: () => { this.emailOtpResending.set(false); },
      error: () => { this.emailOtpResending.set(false); }
    });
  }

  cancelEmailVerification(): void {
    this.verifyingEmail.set(false);
    this.emailOtpCode = '';
    this.authService.getMe().subscribe();
  }

  // ── Edit Health Info ──────────────────────────────────────────────────────
  openEditHealth(): void {
    const p = this.profile();
    this.healthForm = {
      bloodType: p?.bloodType ?? '',
      height:    p?.height ?? null,
      weight:    p?.weight ?? null,
    };
    this.editingHealth.set(true);
  }

  cancelEditHealth(): void { this.editingHealth.set(false); }

  saveHealth(): void {
    const id = this.profileId();
    if (!id) return;
    this.healthSaving.set(true);

    const p = this.profile();
    const u = this.authService.currentUser;
    const body: UpdateProfileBody = {
      patientProfileId: id,
      firstName:  u?.firstName ?? '',
      lastName:   u?.lastName ?? '',
      dateOfBirth: p?.dateOfBirth ? p.dateOfBirth.slice(0, 10) : '',
      gender:     p?.gender ?? '',
      bloodType:  this.healthForm.bloodType || null,
      height:     this.healthForm.height,
      weight:     this.healthForm.weight,
      relationship: null,
    };

    this.http.put<ApiResponse<PatientProfileResponse>>(
      `${environment.apiUrl}/patient-profiles/${id}`, body
    ).subscribe({
      next: (res) => {
        this.profile.set(res.data);
        this.healthSaving.set(false);
        this.editingHealth.set(false);
      },
      error: () => { this.healthSaving.set(false); }
    });
  }

  // ── Allergies ─────────────────────────────────────────────────────────────
  openAddAllergy(): void {
    this.newAllergy = { name: '', severity: 'Mild' };
    this.addingAllergy.set(true);
  }
  cancelAddAllergy(): void { this.addingAllergy.set(false); }

  saveAllergy(): void {
    const id = this.profileId();
    if (!id || !this.newAllergy.name.trim()) return;
    this.allergySaving.set(true);

    const body = { patientProfileId: id, name: this.newAllergy.name, severity: this.newAllergy.severity };
    this.http.post<ApiResponse<{ id: string; name: string; severity: string }>>(
      `${environment.apiUrl}/patient-profiles/${id}/allergies`, body
    ).subscribe({
      next: (res) => {
        this.profile.update(p => p ? { ...p, allergies: [...(p.allergies ?? []), res.data] } : p);
        this.allergySaving.set(false);
        this.addingAllergy.set(false);
      },
      error: () => { this.allergySaving.set(false); }
    });
  }

  deleteAllergy(allergyId: string): void {
    const id = this.profileId();
    if (!id) return;
    this.http.delete<ApiResponse<object>>(
      `${environment.apiUrl}/patient-profiles/${id}/allergies/${allergyId}`
    ).subscribe({
      next: () => {
        this.profile.update(p => p ? { ...p, allergies: (p.allergies ?? []).filter(a => a.id !== allergyId) } : p);
      },
      error: () => {}
    });
  }

  // ── Chronic Diseases ──────────────────────────────────────────────────────
  openAddDisease(): void {
    this.newDisease = { name: '', status: 'Active', diagnosedAt: '' };
    this.addingDisease.set(true);
  }
  cancelAddDisease(): void { this.addingDisease.set(false); }

  saveDisease(): void {
    const id = this.profileId();
    if (!id || !this.newDisease.name.trim()) return;
    this.diseaseSaving.set(true);

    const body = {
      patientProfileId: id,
      name: this.newDisease.name,
      status: this.newDisease.status,
      diagnosedAt: this.newDisease.diagnosedAt || null,
    };
    this.http.post<ApiResponse<{ id: string; name: string; diagnosedAt: string | null; status: string }>>(
      `${environment.apiUrl}/patient-profiles/${id}/chronic-diseases`, body
    ).subscribe({
      next: (res) => {
        this.profile.update(p => p ? { ...p, chronicDiseases: [...(p.chronicDiseases ?? []), res.data] } : p);
        this.diseaseSaving.set(false);
        this.addingDisease.set(false);
      },
      error: () => { this.diseaseSaving.set(false); }
    });
  }

  deleteDisease(diseaseId: string): void {
    const id = this.profileId();
    if (!id) return;
    this.http.delete<ApiResponse<object>>(
      `${environment.apiUrl}/patient-profiles/${id}/chronic-diseases/${diseaseId}`
    ).subscribe({
      next: () => {
        this.profile.update(p => p ? { ...p, chronicDiseases: (p.chronicDiseases ?? []).filter(d => d.id !== diseaseId) } : p);
      },
      error: () => {}
    });
  }

  // ── Emergency Contacts ────────────────────────────────────────────────────
  openAddContact(): void {
    this.newContact = { name: '', phoneNumber: '', relation: '' };
    this.addingContact.set(true);
  }
  cancelAddContact(): void { this.addingContact.set(false); }

  saveContact(): void {
    if (!this.newContact.name.trim() || !this.newContact.phoneNumber.trim()) return;
    this.contactSaving.set(true);

    this.emergencySvc.createEmergencyContact(this.newContact).subscribe({
      next: (res) => {
        this.contacts.update(list => [...list, res.data]);
        this.contactSaving.set(false);
        this.addingContact.set(false);
      },
      error: () => { this.contactSaving.set(false); }
    });
  }

  deleteContact(contactId: string): void {
    this.emergencySvc.deleteEmergencyContact(contactId).subscribe({
      next: () => {
        this.contacts.update(list => list.filter(c => c.id !== contactId));
      },
      error: () => {}
    });
  }

  // ── Delete Account ────────────────────────────────────────────────────────
  openDeleteModal(): void { this.deleteModalOpen.set(true); }
  closeDeleteModal(): void { if (!this.deleteLoading()) this.deleteModalOpen.set(false); }

  confirmDeleteAccount(): void {
    this.deleteLoading.set(true);
    this.authService.deleteAccount().subscribe({
      next: () => {
        this.deleteLoading.set(false);
        this.closeDeleteModal();
        // authService.deleteAccount() tap() handles clearLocalSession + navigate('/login')
      },
      error: () => {
        this.deleteLoading.set(false);
        this.closeDeleteModal();
        this.notifService.showToast('Error', 'Failed to delete account. Please try again.', 'error');
      }
    });
  }

  // ── Severity / Status labels ──────────────────────────────────────────────
  severityLabel(s: string): string {
    const map: Record<string, string> = { '1': 'Mild', '2': 'Moderate', '3': 'Severe' };
    return map[s] ?? s;
  }

  severityClass(s: string): string {
    const v = this.severityLabel(s).toLowerCase();
    if (v === 'mild') return 'tag-mild';
    if (v === 'moderate') return 'tag-moderate';
    return 'tag-severe';
  }

  statusLabel(s: string): string {
    const map: Record<string, string> = { '1': 'Active', '2': 'Controlled', '3': 'Resolved' };
    return map[s] ?? s;
  }

  statusClass(s: string): string {
    const v = this.statusLabel(s).toLowerCase();
    if (v === 'active') return 'tag-active';
    if (v === 'controlled') return 'tag-controlled';
    return 'tag-resolved';
  }
}
