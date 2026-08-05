import {
  ChangeDetectorRef, Component, HostListener, OnInit,
  computed, inject, signal
} from '@angular/core';
import { CommonModule } from '@angular/common';
import { FormsModule } from '@angular/forms';
import { ActivatedRoute, Router, RouterLink } from '@angular/router';
import { HttpClient } from '@angular/common/http';
import { catchError, of, map, forkJoin } from 'rxjs';
import { LocalizationService } from '../../Services/localization.service';
import { AuthService } from '../../Services/auth-service';
import { ProfileCacheService } from '../../Services/profile-cache.service';
import { NotificationService } from '../../Services/notification.service';
import { MediaPickerService } from '../../Services/media-picker.service';
import { ProfileSelectionService } from '../../Services/profile-selection.service';
import { environment } from '../../Environments/Environment';
import {
  FamilyProfilesService,
  AccessibleProfileDto,
  PatientProfileDetailDto,
  ReceivedInvitationDto,
  ProfileMemberDto,
  SentInvitationDto,
} from '../../Services/family-profiles.service';
import { AssistantAnchorDirective } from '../../core/assistant/directives/assistant-anchor.directive';
import { BottomNav } from '../../shared/bottom-nav/bottom-nav';
import { MobileHeader } from '../../shared/mobile-header/mobile-header';

type AddStep = 'choose' | 'create' | 'invite' | 'invited';

interface AllergyEntry { id?: string; name: string; severity: string; }
interface DiseaseEntry { id?: string; name: string; status: string; diagnosedAt: string; }

interface SupervisionMemberEntry {
  accessId: string;
  profileId: string;
  profileName: string;
  firstName: string;
  lastName: string;
  email: string;
  profileImageUrl: string | null;
  role: string;
}

@Component({
  selector: 'app-family-profiles',
  standalone: true,
  imports: [CommonModule, FormsModule, RouterLink, AssistantAnchorDirective, BottomNav, MobileHeader],
  templateUrl: './family-profiles.html',
  styleUrl: './family-profiles.css',
})
export class FamilyProfiles implements OnInit {
  protected readonly l10n = inject(LocalizationService);
  protected readonly t = this.l10n.t;
  private readonly authSvc = inject(AuthService);
  protected readonly profileCache = inject(ProfileCacheService);
  private readonly fpSvc = inject(FamilyProfilesService);
  private readonly profileSelectSvc = inject(ProfileSelectionService);
  private readonly mediaPicker = inject(MediaPickerService);
  protected readonly notifSvc = inject(NotificationService); // template access
  private readonly http = inject(HttpClient);
  private readonly changeDetector = inject(ChangeDetectorRef);
  private readonly route = inject(ActivatedRoute);
  private readonly base = environment.apiUrl;
  readonly router = inject(Router);

  // ─── Data ───────────────────────────────────────────────────
  readonly profiles = signal<AccessibleProfileDto[]>([]);
  readonly selectedProfile = signal<AccessibleProfileDto | null>(null);
  readonly selectedDetail = signal<PatientProfileDetailDto | null>(null);
  readonly receivedInvitations = signal<ReceivedInvitationDto[]>([]);

  // ─── Loading ────────────────────────────────────────────────
  readonly profilesLoading = signal(true);
  readonly detailLoading = signal(false);
  readonly submitting = signal(false);

  // ─── UI state ───────────────────────────────────────────────
  readonly showAddModal = signal(false);
  readonly addStep = signal<AddStep>('choose');
  readonly showInvitationsPanel = signal(false);
  readonly invitationsTab = signal<'received' | 'sent'>('received');
  readonly showEditModal = signal(false);
  readonly editSubmitting = signal(false);
  errorMessage = '';
  editError = '';

  // ─── Supervision Settings ────────────────────────────────────
  readonly showSupervisionModal = signal(false);
  readonly supervisionTab = signal<'supervising' | 'supervised'>('supervising');
  readonly supervisingSearch = signal('');
  readonly supervisedSearch = signal('');
  readonly supervisionLoading = signal(false);
  readonly supervisingList = signal<SupervisionMemberEntry[]>([]);
  readonly showModifyModal = signal(false);
  readonly showRemoveConfirm = signal(false);
  readonly removing = signal(false);
  readonly modifySubmitting = signal(false);
  modifyTarget: { accessId: string; profileId: string; name: string; currentRole: string } | null = null;
  modifyNewRole = 'Viewer';
  removeTarget: { accessId: string; profileId: string; name: string; type: 'member' | 'self' | 'managed' } | null = null;

  // ─── Sent Invitations ────────────────────────────────────────
  readonly sentInvitations = signal<SentInvitationDto[]>([]);
  readonly sentInvitationsLoading = signal(false);

  // ─── Supervision pagination ───────────────────────────────────
  readonly PAGE_SIZE = 2;
  readonly supervisedPage = signal(0);
  readonly supervisingPage = signal(0);
  readonly supervisedVisible = signal(true);
  readonly supervisingVisible = signal(true);

  // ─── Computed ───────────────────────────────────────────────
  readonly pendingCount = computed(() =>
    this.receivedInvitations().filter(i => i.status === 'Pending').length
  );

  readonly ownerProfiles = computed(() =>
    this.profiles().filter(p => p.accessRole === 'Owner')
  );

  readonly supervisedProfiles = computed(() =>
    this.profiles().filter(p => !p.isSelf)
  );

  readonly filteredSupervised = computed(() => {
    const q = this.supervisedSearch().toLowerCase();
    if (!q) return this.supervisedProfiles();
    return this.supervisedProfiles().filter(p =>
      `${p.firstName} ${p.lastName}`.toLowerCase().includes(q)
    );
  });

  readonly filteredSupervising = computed(() => {
    const q = this.supervisingSearch().toLowerCase();
    if (!q) return this.supervisingList();
    return this.supervisingList().filter(m =>
      `${m.firstName} ${m.lastName}`.toLowerCase().includes(q) ||
      m.email.toLowerCase().includes(q)
    );
  });

  readonly pagedSupervised = computed(() => {
    const all = this.filteredSupervised();
    return all.slice(this.supervisedPage() * this.PAGE_SIZE, (this.supervisedPage() + 1) * this.PAGE_SIZE);
  });

  readonly supervisedTotalPages = computed(() =>
    Math.ceil(this.filteredSupervised().length / this.PAGE_SIZE) || 1
  );

  readonly pagedSupervising = computed(() => {
    const all = this.filteredSupervising();
    return all.slice(this.supervisingPage() * this.PAGE_SIZE, (this.supervisingPage() + 1) * this.PAGE_SIZE);
  });

  readonly supervisingTotalPages = computed(() =>
    Math.ceil(this.filteredSupervising().length / this.PAGE_SIZE) || 1
  );

  readonly unreadNotifCount = this.notifSvc.unreadCount;

  // ─── New: Family Hub overview ────────────────────────────────
  readonly familyOverview = computed(() => ({
    totalMembers: this.profiles().length,
    activeProfiles: this.profiles().filter(p => p.userId !== null || p.isSelf).length,
    pendingInvitations: this.pendingCount(),
  }));

  healthStatusFor(detail: PatientProfileDetailDto | null): { allClear: boolean; conditionCount: number } {
    const allergyCount = detail?.allergies?.length ?? 0;
    const diseaseCount = detail?.chronicDiseases?.length ?? 0;
    return {
      allClear: allergyCount === 0 && diseaseCount === 0,
      conditionCount: allergyCount + diseaseCount,
    };
  }

  // ─── Create form ────────────────────────────────────────────
  createForm = {
    firstName: '', lastName: '', dateOfBirth: '',
    gender: '', bloodType: '', height: null as number | null,
    weight: null as number | null, relationship: '',
    showAllergies: false,
    allergies: [] as AllergyEntry[],
    showDiseases: false,
    chronicDiseases: [] as DiseaseEntry[],
    profileImage: null as File | null,
    profileImagePreview: null as string | null,
  };

  inviteForm = { profileId: '', email: '', role: 'Viewer' };

  // ─── Edit form ──────────────────────────────────────────────
  editForm = {
    firstName: '', lastName: '', dateOfBirth: '',
    gender: '', bloodType: '', height: null as number | null,
    weight: null as number | null, relationship: '',
    allergies: [] as AllergyEntry[],
    chronicDiseases: [] as DiseaseEntry[],
    profileImage: null as File | null,
    profileImagePreview: null as string | null,
    existingImageUrl: null as string | null,
    removeImage: false,
  };

  private static readonly MAX_IMAGE_SIZE_BYTES = 5 * 1024 * 1024;
  private static readonly ALLOWED_IMAGE_TYPES = ['image/jpeg', 'image/png', 'image/webp', 'image/gif'];

  // ─── Static options ─────────────────────────────────────────
  readonly relationships = [
    'Son', 'Daughter', 'Father', 'Mother', 'Husband', 'Wife',
    'Brother', 'Sister', 'Grandfather', 'Grandmother', 'Other',
  ];

  readonly bloodTypes = [
    { value: 'APositive', label: 'A+' }, { value: 'ANegative', label: 'A-' },
    { value: 'BPositive', label: 'B+' }, { value: 'BNegative', label: 'B-' },
    { value: 'ABPositive', label: 'AB+' }, { value: 'ABNegative', label: 'AB-' },
    { value: 'OPositive', label: 'O+' }, { value: 'ONegative', label: 'O-' },
  ];

  readonly severityOptions = ['Mild', 'Moderate', 'Severe'];
  readonly diseaseStatusOptions = ['Active', 'Controlled', 'Resolved'];

  private readonly avatarColors = [
    '#0EAFD7', '#7C3AED', '#16A34A', '#EA580C', '#0D9488', '#D97706', '#DC2626',
  ];

  /** Resolves a stored relative image path to an absolute URL, or null when there's no image. */
  private resolveProfileImageUrl(path: string | null | undefined): string | null {
    return path ? `${environment.fileBaseUrl}${path}` : null;
  }

  /** The photo to show for a family/self profile, or null to fall back to the initials avatar. */
  profileAvatarUrl(profile: AccessibleProfileDto): string | null {
    const path = profile.isSelf ? this.authSvc.currentUser?.profileImageUrl : profile.profileImageUrl;
    return this.resolveProfileImageUrl(path ?? null);
  }

  /** The photo to show for a profile member (registered account), or null for the initials avatar. */
  memberAvatarUrl(profileImageUrl: string | null | undefined): string | null {
    return this.resolveProfileImageUrl(profileImageUrl);
  }

  // ─── Lifecycle ──────────────────────────────────────────────
  
  ngOnInit(): void {
    this.profileCache.ensure();
    this.loadProfiles();
    this.checkAddRoute();
    this.router.events.subscribe(() => this.checkAddRoute());

    this.loadReceivedInvitations();

    this.route.queryParamMap.subscribe(params => {
      const editId = params.get('editId');
      if (!editId) return;
      this.tryConsumeEditId(editId);
    });
  }

  private pendingEditId: string | null = null;

  private tryConsumeEditId(editId: string): void {
    if (this.profilesLoading()) {
      // Profiles not loaded yet — loadProfiles() will call this again once they are.
      this.pendingEditId = editId;
      return;
    }
    const target = this.profiles().find(p => p.userHealthProfileId === editId);
    if (target) {
      this.selectProfile(target);
      this.openEditModal();
    }
    this.router.navigate([], { queryParams: {}, replaceUrl: true });
  }

  private loadProfiles(): void {
    this.profilesLoading.set(true);
    this.fpSvc.getAccessible().pipe(catchError(() => of([] as AccessibleProfileDto[]))).subscribe(list => {
      this.profiles.set(list);
      this.profilesLoading.set(false);
      if (list.length > 0 && !this.selectedProfile()) {
        const storedId = this.profileSelectSvc.selectedProfileId;
        const restored = storedId ? list.find(p => p.userHealthProfileId === storedId) : null;
        this.selectProfile(restored ?? list[0]);
      }
      if (this.pendingEditId) {
        const editId = this.pendingEditId;
        this.pendingEditId = null;
        this.tryConsumeEditId(editId);
      }
    });
  }

  private loadReceivedInvitations(): void {
    this.fpSvc.getReceivedInvitations().pipe(catchError(() => of([] as ReceivedInvitationDto[]))).subscribe(list => {
      this.receivedInvitations.set(list);
    });
  }

  // ─── Profile selection ──────────────────────────────────────
  selectProfile(p: AccessibleProfileDto): void {
    this.selectedProfile.set(p);
    this.profileSelectSvc.select(p.userHealthProfileId);
    this.detailLoading.set(true);
    this.selectedDetail.set(null);
    this.fpSvc.getById(p.userHealthProfileId).pipe(catchError(() => of(null))).subscribe(d => {
      this.selectedDetail.set(d);
      this.detailLoading.set(false);
    });
  }

  // ─── Add modal ──────────────────────────────────────────────
  
  checkAddRoute() {
    if (this.router.url.endsWith('/family-profiles/add')) {
      if (!this.showAddModal()) {
        this.openAddModal();
      }
    } else {
      if (this.showAddModal()) {
        this.showAddModal.set(false);
      }
    }
  }

  openAddModal(): void {
    this.createForm = {
      firstName: '', lastName: '', dateOfBirth: '', gender: '', bloodType: '',
      height: null, weight: null, relationship: '',
      showAllergies: false, allergies: [],
      showDiseases: false, chronicDiseases: [],
      profileImage: null, profileImagePreview: null,
    };
    const firstOwner = this.ownerProfiles()[0];
    this.inviteForm = { profileId: firstOwner?.userHealthProfileId ?? '', email: '', role: 'Viewer' };
    this.errorMessage = '';
    this.addStep.set('choose');
    this.showAddModal.set(true);
  }

  closeAddModal(): void { this.router.navigate(['/family-profiles']); }
  goToCreate(): void { this.addStep.set('create'); this.errorMessage = ''; }
  goToInvite(): void { this.addStep.set('invite'); this.errorMessage = ''; }
  backToChoose(): void { this.addStep.set('choose'); this.errorMessage = ''; }

  // Allergy helpers
  addAllergyToCreate(): void { this.createForm.allergies.push({ name: '', severity: 'Mild' }); }
  removeAllergyFromCreate(i: number): void { this.createForm.allergies.splice(i, 1); }
  addDiseaseToCreate(): void { this.createForm.chronicDiseases.push({ name: '', status: 'Active', diagnosedAt: '' }); }
  removeDiseaseFromCreate(i: number): void { this.createForm.chronicDiseases.splice(i, 1); }

  addAllergyToEdit(): void { this.editForm.allergies.push({ name: '', severity: 'Mild' }); }
  removeAllergyFromEdit(i: number): void { this.editForm.allergies.splice(i, 1); }
  addDiseaseToEdit(): void { this.editForm.chronicDiseases.push({ name: '', status: 'Active', diagnosedAt: '' }); }
  removeDiseaseFromEdit(i: number): void { this.editForm.chronicDiseases.splice(i, 1); }

  // ─── Profile image pickers ────────────────────────────────────
  private validateAndPreviewImage(
    file: File,
    onValid: (file: File, preview: string) => void,
    onError: (message: string) => void
  ): void {
    if (!FamilyProfiles.ALLOWED_IMAGE_TYPES.includes(file.type)) {
      onError('Profile image must be a JPEG, PNG, WEBP, or GIF file.');
      return;
    }
    if (file.size > FamilyProfiles.MAX_IMAGE_SIZE_BYTES) {
      onError('Profile image must not exceed 5 MB.');
      return;
    }

    const reader = new FileReader();
    reader.onload = () => {
      onValid(file, reader.result as string);
      this.changeDetector.detectChanges();
    };
    reader.readAsDataURL(file);
  }

  async selectCreateImage(): Promise<void> {
    this.errorMessage = '';
    const file = await this.mediaPicker.selectMedia({ accept: 'image/*' });
    if (!file) return;

    this.validateAndPreviewImage(
      file,
      (f, preview) => { this.createForm.profileImage = f; this.createForm.profileImagePreview = preview; },
      (message) => { this.errorMessage = message; }
    );
  }

  removeCreateImage(): void {
    this.createForm.profileImage = null;
    this.createForm.profileImagePreview = null;
  }

  async selectEditImage(): Promise<void> {
    this.editError = '';
    const file = await this.mediaPicker.selectMedia({ accept: 'image/*' });
    if (!file) return;

    this.validateAndPreviewImage(
      file,
      (f, preview) => {
        this.editForm.profileImage = f;
        this.editForm.profileImagePreview = preview;
        this.editForm.removeImage = false;
      },
      (message) => { this.editError = message; }
    );
  }

  removeEditImage(): void {
    this.editForm.profileImage = null;
    this.editForm.profileImagePreview = null;
    this.editForm.removeImage = true;
  }

  submitCreateManaged(): void {
    const f = this.createForm;
    if (!f.firstName.trim() || !f.lastName.trim() || !f.dateOfBirth || !f.gender || !f.relationship) {
      this.errorMessage = this.t().family.fillRequiredFields;
      return;
    }
    this.submitting.set(true);
    this.errorMessage = '';
    this.fpSvc.createManaged({
      firstName: f.firstName.trim(),
      lastName: f.lastName.trim(),
      dateOfBirth: f.dateOfBirth,
      gender: f.gender,
      bloodType: f.bloodType || null,
      height: f.height,
      weight: f.weight,
      relationship: f.relationship,
      allergies: f.showAllergies ? f.allergies.filter(a => a.name.trim()) : [],
      chronicDiseases: f.showDiseases ? f.chronicDiseases.filter(d => d.name.trim()) : [],
    }).pipe(catchError(err => {
      const apiErrors: string[] = err?.error?.errors ?? [];
      this.errorMessage = apiErrors.length ? apiErrors.join(' ') : (err?.error?.message || 'Failed to create profile.');
      this.submitting.set(false);
      return of(null);
    })).subscribe(result => {
      if (!result) { return; }

      const profileImage = f.profileImage;
      if (!profileImage) {
        this.submitting.set(false);
        this.closeAddModal();
        this.loadProfiles();
        return;
      }

      this.fpSvc.updateProfileImage(result.id, profileImage).pipe(
        catchError(() => of(null))
      ).subscribe(() => {
        this.submitting.set(false);
        this.closeAddModal();
        this.loadProfiles();
      });
    });
  }

  submitInvite(): void {
    const f = this.inviteForm;
    if (!f.profileId || !f.email.trim() || !f.role) {
      this.errorMessage = this.t().family.fillRequiredFields;
      return;
    }
    this.submitting.set(true);
    this.errorMessage = '';
    this.fpSvc.sendInvitation(f.profileId, f.email.trim(), f.role).pipe(
      catchError(err => {
        const apiErrors: string[] = err?.error?.errors ?? [];
        this.errorMessage = apiErrors.length ? apiErrors.join(' ') : (err?.error?.message || 'Failed to send invitation.');
        this.submitting.set(false);
        return of(null);
      })
    ).subscribe(result => {
      this.submitting.set(false);
      if (result !== null) { this.addStep.set('invited'); }
    });
  }

  // ─── Edit Profile modal ─────────────────────────────────────
  openEditModal(): void {
    const d = this.selectedDetail();
    const p = this.selectedProfile();
    if (!d || !p) return;
    this.editError = '';
    this.editForm = {
      firstName: p.firstName,
      lastName: p.lastName,
      dateOfBirth: d.dateOfBirth?.split('T')[0] ?? '',
      gender: d.gender ?? '',
      bloodType: d.bloodType ?? '',
      height: d.height,
      weight: d.weight,
      relationship: p.relationship ?? '',
      allergies: (d.allergies ?? []).map((a: any) => ({ id: a.id, name: a.name, severity: a.severity })),
      chronicDiseases: (d.chronicDiseases ?? []).map((c: any) => ({ id: c.id, name: c.name, status: c.status, diagnosedAt: c.diagnosedAt?.split('T')[0] ?? '' })),
      profileImage: null,
      profileImagePreview: null,
      existingImageUrl: p.profileImageUrl ?? null,
      removeImage: false,
    };
    this.showEditModal.set(true);
  }

  closeEditModal(): void { this.showEditModal.set(false); }

  submitEditProfile(): void {
    const f = this.editForm;
    if (!f.firstName?.trim() || !f.lastName?.trim() || !f.dateOfBirth || !f.gender) {
      this.editError = this.t().family.fillRequiredFields;
      return;
    }
    const profileId = this.selectedProfile()?.userHealthProfileId;
    if (!profileId) return;
    this.editSubmitting.set(true);
    this.editError = '';
    const isSelf = this.selectedProfile()?.isSelf ?? false;
    this.http.put<any>(`${this.base}/patient-profiles/${profileId}`, {
      patientProfileId: profileId,
      firstName: f.firstName.trim(),
      lastName: f.lastName.trim(),
      dateOfBirth: f.dateOfBirth,
      gender: f.gender,
      bloodType: f.bloodType || null,
      height: f.height,
      weight: f.weight,
      relationship: isSelf ? null : (f.relationship || null),
    }).pipe(
      catchError(err => {
        const apiErrors: string[] = err?.error?.errors ?? [];
        this.editError = apiErrors.length ? apiErrors.join(' ') : (err?.error?.message || 'Failed to update profile.');
        this.editSubmitting.set(false);
        return of(null);
      })
    ).subscribe(result => {
      if (result === null) return;

      const detail = this.selectedDetail();
      const origAllergies = detail?.allergies ?? [];
      const origDiseases = detail?.chronicDiseases ?? [];
      const origAllergyMap = new Map(origAllergies.map(a => [a.id, a]));
      const origDiseaseMap = new Map(origDiseases.map(d => [d.id, d]));

      const finalAllergies = f.allergies.filter(a => a.name.trim());
      const finalDiseases = f.chronicDiseases.filter(d => d.name.trim());
      const finalAllergyIds = new Set(finalAllergies.filter(a => a.id).map(a => a.id!));
      const finalDiseaseIds = new Set(finalDiseases.filter(d => d.id).map(d => d.id!));

      const ops: any[] = [];

      // Allergies: delete removed
      for (const orig of origAllergies) {
        if (!finalAllergyIds.has(orig.id)) {
          ops.push(this.http.delete(`${this.base}/patient-profiles/${profileId}/allergies/${orig.id}`));
        }
      }
      // Allergies: create new / update changed
      for (const a of finalAllergies) {
        if (!a.id) {
          ops.push(this.http.post(`${this.base}/patient-profiles/${profileId}/allergies`, {
            patientProfileId: profileId, name: a.name.trim(), severity: a.severity,
          }));
        } else {
          const orig = origAllergyMap.get(a.id);
          if (!orig || orig.name !== a.name.trim() || orig.severity !== a.severity) {
            ops.push(this.http.put(`${this.base}/patient-profiles/${profileId}/allergies/${a.id}`, {
              patientProfileId: profileId, allergyId: a.id, name: a.name.trim(), severity: a.severity,
            }));
          }
        }
      }

      // Diseases: delete removed
      for (const orig of origDiseases) {
        if (!finalDiseaseIds.has(orig.id)) {
          ops.push(this.http.delete(`${this.base}/patient-profiles/${profileId}/chronic-diseases/${orig.id}`));
        }
      }
      // Diseases: create new / update changed
      for (const d of finalDiseases) {
        if (!d.id) {
          ops.push(this.http.post(`${this.base}/patient-profiles/${profileId}/chronic-diseases`, {
            patientProfileId: profileId, name: d.name.trim(), diagnosedAt: d.diagnosedAt || null, status: d.status,
          }));
        } else {
          const orig = origDiseaseMap.get(d.id);
          if (!orig || orig.name !== d.name.trim() || orig.status !== d.status || (orig.diagnosedAt?.split('T')[0] ?? '') !== d.diagnosedAt) {
            ops.push(this.http.put(`${this.base}/patient-profiles/${profileId}/chronic-diseases/${d.id}`, {
              patientProfileId: profileId, diseaseId: d.id, name: d.name.trim(), diagnosedAt: d.diagnosedAt || null, status: d.status,
            }));
          }
        }
      }

      const finish = () => {
        const profileImage = this.editForm.profileImage;
        const removeImage = this.editForm.removeImage;

        // Helper: patch the image URL across every in-memory signal so no reload is needed
        const patchImageUrl = (newUrl: string | null) => {
          this.selectedDetail.update(d => d ? { ...d, profileImageUrl: newUrl } : d);
          this.selectedProfile.update(p => p ? { ...p, profileImageUrl: newUrl } : p);
          this.profiles.update(list =>
            list.map(p => p.userHealthProfileId === profileId ? { ...p, profileImageUrl: newUrl } : p)
          );
        };

        // Helper: patch text fields (name etc.) that were also edited
        const patchTextFields = () => {
          const firstName = this.editForm.firstName.trim();
          const lastName = this.editForm.lastName.trim();
          this.selectedProfile.update(p => p ? { ...p, firstName, lastName } : p);
          this.profiles.update(list =>
            list.map(p => p.userHealthProfileId === profileId ? { ...p, firstName, lastName } : p)
          );
        };

        if (profileImage || removeImage) {
          this.fpSvc.updateProfileImage(profileId, profileImage, removeImage).pipe(
            catchError(() => of(null))
          ).subscribe(updated => {
            if (updated) { patchImageUrl(updated.profileImageUrl); }
            patchTextFields();
            this.editSubmitting.set(false);
            this.closeEditModal();
          });
        } else {
          patchTextFields();
          this.editSubmitting.set(false);
          this.closeEditModal();
          // Refresh detail text fields from server without a full list reload
          this.fpSvc.getById(profileId).pipe(catchError(() => of(null))).subscribe(d => {
            if (d) { this.selectedDetail.set(d); }
          });
        }
      };

      if (ops.length === 0) { finish(); return; }

      forkJoin(ops).pipe(
        catchError(err => {
          const apiErrors: string[] = err?.error?.errors ?? [];
          this.editError = apiErrors.length ? apiErrors.join(' ') : (err?.error?.message || 'Failed to update allergies or diseases.');
          this.editSubmitting.set(false);
          return of(null);
        })
      ).subscribe(res => { if (res !== null) finish(); });
    });
  }

  // ─── Invitations ────────────────────────────────────────────
  acceptInvitation(id: string): void {
    this.fpSvc.acceptInvitation(id).subscribe({
      next: () => { this.loadReceivedInvitations(); this.loadProfiles(); },
      error: () => { },
    });
  }

  rejectInvitation(id: string): void {
    this.fpSvc.rejectInvitation(id).subscribe({
      next: () => { this.loadReceivedInvitations(); },
      error: () => { },
    });
  }

  loadSentInvitations(): void {
    this.sentInvitationsLoading.set(true);
    this.fpSvc.getSentInvitations().pipe(catchError(() => of([] as SentInvitationDto[]))).subscribe(list => {
      this.sentInvitations.set(list);
      this.sentInvitationsLoading.set(false);
    });
  }

  cancelSentInvitation(id: string): void {
    this.fpSvc.cancelInvitation(id).pipe(catchError(() => of(null))).subscribe(() => {
      this.loadSentInvitations();
    });
  }

  formatInvitationDateTime(dateStr: string): string {
    const d = new Date(dateStr);
    return d.toLocaleDateString('en-US', { month: 'short', day: 'numeric', year: 'numeric' })
      + ' · '
      + d.toLocaleTimeString('en-US', { hour: 'numeric', minute: '2-digit', hour12: true });
  }

  // ─── Supervision Settings ────────────────────────────────────
  openSupervisionModal(): void {
    this.showSupervisionModal.set(true);
    this.supervisionTab.set('supervising');
    this.supervisingSearch.set('');
    this.supervisedSearch.set('');
    this.loadSupervisingMembers();
  }

  closeSupervisionModal(): void {
    this.showSupervisionModal.set(false);
    this.showModifyModal.set(false);
    this.showRemoveConfirm.set(false);
    this.modifyTarget = null;
    this.removeTarget = null;
  }

  switchSupervisionTab(tab: 'supervising' | 'supervised'): void {
    this.supervisionTab.set(tab);
    this.supervisedPage.set(0);
    this.supervisingPage.set(0);
  }

  // ─── Supervision search (resets page) ───────────────────────
  setSupervisedSearch(val: string): void { this.supervisedSearch.set(val); this.supervisedPage.set(0); }
  setSupervisingSearch(val: string): void { this.supervisingSearch.set(val); this.supervisingPage.set(0); }

  // ─── Supervision pagination ───────────────────────────────────
  prevSupervisedPage(): void { this.animatePage(() => this.supervisedPage.update(v => v - 1), v => this.supervisedVisible.set(v)); }
  nextSupervisedPage(): void { this.animatePage(() => this.supervisedPage.update(v => v + 1), v => this.supervisedVisible.set(v)); }
  prevSupervisingPage(): void { this.animatePage(() => this.supervisingPage.update(v => v - 1), v => this.supervisingVisible.set(v)); }
  nextSupervisingPage(): void { this.animatePage(() => this.supervisingPage.update(v => v + 1), v => this.supervisingVisible.set(v)); }

  private animatePage(changeFn: () => void, setVisible: (v: boolean) => void): void {
    setVisible(false);
    setTimeout(() => { changeFn(); setVisible(true); }, 160);
  }

  private loadSupervisingMembers(): void {
    const owned = this.profiles().filter(p => p.accessRole === 'Owner');
    if (owned.length === 0) {
      this.supervisingList.set([]);
      return;
    }
    this.supervisionLoading.set(true);
    const calls$ = owned.map(p =>
      this.fpSvc.getProfileMembers(p.userHealthProfileId).pipe(
        map(members => ({
          profileId: p.userHealthProfileId,
          profileName: `${p.firstName} ${p.lastName}`,
          members,
        })),
        catchError(() => of({ profileId: p.userHealthProfileId, profileName: `${p.firstName} ${p.lastName}`, members: [] as ProfileMemberDto[] }))
      )
    );
    forkJoin(calls$).subscribe(results => {
      const entries: SupervisionMemberEntry[] = [];
      for (const { profileId, profileName, members } of results) {
        for (const m of members) {
          if (!m.isCurrentUser) {
            entries.push({
              accessId: m.accessId, profileId, profileName, firstName: m.firstName, lastName: m.lastName,
              email: m.email, profileImageUrl: m.profileImageUrl, role: m.role
            });
          }
        }
      }
      this.supervisingList.set(entries);
      this.supervisionLoading.set(false);
    });
  }

  openModifyAccess(entry: SupervisionMemberEntry): void {
    this.modifyTarget = { accessId: entry.accessId, profileId: entry.profileId, name: `${entry.firstName} ${entry.lastName}`, currentRole: entry.role };
    this.modifyNewRole = entry.role;
    this.showModifyModal.set(true);
  }

  closeModifyModal(): void {
    this.showModifyModal.set(false);
    this.modifyTarget = null;
  }

  saveModifyAccess(): void {
    if (!this.modifyTarget) return;
    this.modifySubmitting.set(true);
    this.fpSvc.changeMemberRole(this.modifyTarget.profileId, this.modifyTarget.accessId, this.modifyNewRole)
      .pipe(catchError(() => of(null)))
      .subscribe(() => {
        this.modifySubmitting.set(false);
        this.closeModifyModal();
        this.loadSupervisingMembers();
      });
  }

  openRemoveConfirm(entry: SupervisionMemberEntry): void {
    this.removeTarget = { accessId: entry.accessId, profileId: entry.profileId, name: `${entry.firstName} ${entry.lastName}`, type: 'member' };
    this.showRemoveConfirm.set(true);
  }

  openLeaveConfirm(profile: AccessibleProfileDto): void {
    const type = profile.profileType === 'Managed' ? 'managed' : 'self';
    this.removeTarget = { accessId: '', profileId: profile.userHealthProfileId, name: `${profile.firstName} ${profile.lastName}`, type };
    this.showRemoveConfirm.set(true);
  }

  closeRemoveConfirm(): void {
    this.showRemoveConfirm.set(false);
    this.removeTarget = null;
  }

  confirmRemove(): void {
    if (!this.removeTarget) return;
    this.removing.set(true);
    const { type, profileId, accessId } = this.removeTarget;
    let action$;
    if (type === 'managed') {
      action$ = this.fpSvc.deleteProfile(profileId);
    } else if (type === 'self') {
      action$ = this.fpSvc.leaveProfile(profileId);
    } else {
      action$ = this.fpSvc.revokeMemberAccess(profileId, accessId);
    }
    action$.pipe(catchError(() => of(null))).subscribe(() => {
      this.removing.set(false);
      this.closeRemoveConfirm();
      if (type === 'managed' || type === 'self') {
        this.loadProfiles();
      } else {
        this.loadSupervisingMembers();
      }
    });
  }

  // ─── Display helpers ────────────────────────────────────────
  get todayStr(): string {
    return new Date().toISOString().split('T')[0];
  }

  getAvatarColor(i: number): string { return this.avatarColors[i % this.avatarColors.length]; }

  getRelationTranslation(r: string): string {
    return (this.t().family as any)[r.toLowerCase()] ?? r;
  }

  getRelLabel(p: AccessibleProfileDto): string {
    if (p.isSelf) {
      return this.t().family.self;
    }

    const key = (p.relationship ?? 'member').toLowerCase();

    return (this.t().family as any)[key] ?? this.t().family.member;
  }

  getRelBadgeClass(p: AccessibleProfileDto): string {
    if (p.isSelf) return 'fp-badge fp-badge--blue';
    const r = p.relationship ?? '';
    return ['Husband', 'Wife'].includes(r) ? 'fp-badge fp-badge--blue' : 'fp-badge fp-badge--orange';
  }

  getRelIcon(p: AccessibleProfileDto): string {
    if (p.isSelf) return 'fa-solid fa-user';
    const r = p.relationship ?? '';
    return ['Husband', 'Wife'].includes(r) ? 'fa-solid fa-heart' : 'fa-solid fa-person';
  }

  getStatusLabel(p: AccessibleProfileDto): string {
    const key = (p.relationship ?? 'member').toLowerCase();
    return p.isSelf ? (this.t().family as any)[key] ?? this.t().family.member : 'Active';
  }

  getDotClass(p: AccessibleProfileDto): string {
    return p.isSelf ? 'fp-dot fp-dot--blue' : 'fp-dot fp-dot--green';
  }

  formatBloodType(bt: string | null | undefined): string {
    if (!bt) return 'N/A';
    return bt.replace('Positive', '+').replace('Negative', '-');
  }

  formatDob(dob: string): string {
    return new Date(dob).toLocaleDateString('en-US', { month: 'short', day: 'numeric', year: 'numeric' });
  }

  getAge(dob: string | null | undefined): number | null {
    if (!dob) return null;
    const b = new Date(dob);
    const n = new Date();
    let age = n.getFullYear() - b.getFullYear();
    if (n.getMonth() < b.getMonth() || (n.getMonth() === b.getMonth() && n.getDate() < b.getDate())) age--;
    return age;
  }

  getInviterLabel(inv: ReceivedInvitationDto): string {
    if (inv.inviterFirstName) return `${inv.inviterFirstName} ${inv.inviterLastName ?? ''}`.trim();
    return inv.inviterEmail ?? 'Someone';
  }

  getInviterInitial(inv: ReceivedInvitationDto): string {
    return (inv.inviterFirstName?.[0] ?? inv.inviterEmail?.[0] ?? '?').toUpperCase();
  }

  getTimeAgo(createdAt: string): string {
    const days = Math.floor((Date.now() - new Date(createdAt).getTime()) / 86_400_000);
    if (days === 0) return 'Today';
    if (days === 1) return '1 day ago';
    return `${days} days ago`;
  }

  getInvStatusClass(status: string): string {
    const map: Record<string, string> = {
      Pending: 'fp-inv-status--pending',
      Active: 'fp-inv-status--active',
      Rejected: 'fp-inv-status--rejected',
      Cancelled: 'fp-inv-status--cancelled',
    };
    return `fp-inv-status ${map[status] ?? ''}`;
  }

  getProfileNameFor(profileId: string): string {
    const p = this.profiles().find(x => x.userHealthProfileId === profileId);
    return p ? `${p.firstName} ${p.lastName}` : 'Unknown Profile';
  }

  getGenderLabel(gender: string | null | undefined): string {
    if (!gender) {
      return '-';
    }

    const key = gender.toLowerCase();

    return (this.t().common as any)[key] ?? gender;
  }

  @HostListener('document:keydown.escape')
  onEscape(): void {
    if (this.showRemoveConfirm()) { this.closeRemoveConfirm(); return; }
    if (this.showModifyModal()) { this.closeModifyModal(); return; }
    if (this.showSupervisionModal()) { this.closeSupervisionModal(); return; }
  }
}
