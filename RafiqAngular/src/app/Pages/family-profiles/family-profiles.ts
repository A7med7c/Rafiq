import {
  ChangeDetectorRef, Component, ElementRef, HostListener, OnInit,
  ViewChild, computed, inject, signal, WritableSignal
} from '@angular/core';
import { CommonModule } from '@angular/common';
import { FormsModule } from '@angular/forms';
import { Router, RouterLink, RouterLinkActive } from '@angular/router';
import { HttpClient } from '@angular/common/http';
import { catchError, of, map, forkJoin } from 'rxjs';
import { LocalizationService } from '../../Services/localization.service';
import { AiChatService } from '../../Services/ai-chat.service';
import { AuthService } from '../../Services/auth-service';
import { ProfileCacheService } from '../../Services/profile-cache.service';
import { NotificationService } from '../../Services/notification.service';
import { ReviewTrackingService } from '../../Services/review-tracking.service';
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
import { DashboardService, HealthSummaryDto } from '../../Services/dashboard.service';
import { AssistantAnchorDirective } from '../../core/assistant/directives/assistant-anchor.directive';
import { AssistantOrchestratorService } from '../../core/assistant/services/assistant-orchestrator.service';

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
  imports: [CommonModule, FormsModule, RouterLink, RouterLinkActive, AssistantAnchorDirective],
  templateUrl: './family-profiles.html',
  styleUrl: './family-profiles.css',
})
export class FamilyProfiles implements OnInit {
  @ViewChild('carouselEl') carouselElRef?: ElementRef<HTMLDivElement>;

  protected readonly l10n = inject(LocalizationService);
  protected readonly aiChatService = inject(AiChatService);
  protected readonly t = this.l10n.t;
  private readonly authSvc        = inject(AuthService);
  protected readonly profileCache = inject(ProfileCacheService);
  private readonly fpSvc = inject(FamilyProfilesService);
  private readonly profileSelectSvc = inject(ProfileSelectionService);
  protected readonly notifSvc = inject(NotificationService); // template access
  private readonly reviewTracking = inject(ReviewTrackingService);
  private readonly assistantOrchestrator = inject(AssistantOrchestratorService);
  private readonly http = inject(HttpClient);
  private readonly changeDetector = inject(ChangeDetectorRef);
  private readonly base = environment.apiUrl;
  readonly router = inject(Router);

  // ─── Sidebar ────────────────────────────────────────────────
  readonly sidebarCollapsed = signal(false);
  readonly mobileSidebarOpen = signal(false);
  readonly dropdownOpen = signal(false);
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
  readonly activeTab = signal<string>('overview');
  readonly tabDirection = signal<'left' | 'right'>('left');
  readonly tabAnimating = signal(false);
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

  // ─── Three-dot menu / Delete profile ────────────────────────
  readonly showProfileMenu = signal(false);
  readonly showDeleteConfirm = signal(false);
  readonly deletingProfile = signal(false);

  // ─── Sent Invitations ────────────────────────────────────────
  readonly sentInvitations = signal<SentInvitationDto[]>([]);
  readonly sentInvitationsLoading = signal(false);

  // ─── Supervision pagination ───────────────────────────────────
  readonly PAGE_SIZE = 2;
  readonly supervisedPage = signal(0);
  readonly supervisingPage = signal(0);
  readonly supervisedVisible = signal(true);
  readonly supervisingVisible = signal(true);
  readonly mobileTabMenuOpen = signal(false);

  // ─── Medications / Reminders tab ────────────────────────────
  readonly fpMedicines = signal<any[]>([]);
  readonly fpMedicinesLoading = signal(false);

  // ─── Computed ───────────────────────────────────────────────
  readonly pendingCount = computed(() =>
    this.receivedInvitations().filter(i => i.status === 'Pending').length
  );
  //────── Health Summary ─────────────────────────────────────────

  private readonly dashboardService = inject(DashboardService);

  readonly healthSummary = signal<HealthSummaryDto | null>(null);
  readonly summaryLoading = signal(false);
  readonly summaryExpanded = signal(false);

  readonly SUMMARY_CHAR_LIMIT = 260;

  getTruncatedSummary(full: string): string {
    if (this.summaryExpanded() || full.length <= this.SUMMARY_CHAR_LIMIT) return full;
    return full.slice(0, this.SUMMARY_CHAR_LIMIT).trimEnd() + '…';
  }

  isSummaryTruncatable(full: string): boolean {
    return full.length > this.SUMMARY_CHAR_LIMIT;
  }



  loadHealthSummary() {
    const profileId = this.selectedProfile()?.userHealthProfileId;
    this.summaryLoading.set(true);
    this.summaryExpanded.set(false);
    const obs$ = profileId
      ? this.dashboardService.getHealthSummaryForProfile(profileId)
      : this.dashboardService.getHealthSummary();

    obs$.subscribe({
      next: summary => {
        this.healthSummary.set(summary);
        this.summaryLoading.set(false);
      },
      error: () => {
        this.healthSummary.set(null);
        this.summaryLoading.set(false);
      }
    });
  }
  selectedTab = signal('overview');
  selectTab(tab: string) {

    this.selectedTab.set(tab);

    if (tab === 'health-summary') {

      this.loadHealthSummary();

    }

  }
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
    this.applyResponsiveSidebar();
    this.loadProfiles();
    this.loadReceivedInvitations();
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
    this.activeTab.set('overview');
    this.profileSelectSvc.select(p.userHealthProfileId);
    this.fpMedicines.set([]);
    this.detailLoading.set(true);
    this.selectedDetail.set(null);
    this.healthSummary.set(null);
    this.fpSvc.getById(p.userHealthProfileId).pipe(catchError(() => of(null))).subscribe(d => {
      this.selectedDetail.set(d);
      this.detailLoading.set(false);
    });
  }

  summaryText(s: HealthSummaryDto): string {
    const parts: string[] = [`Status: ${s.overallStatus}${s.overallStatusNote ? ' — ' + s.overallStatusNote : ''}`];
    if (s.conditions.length) parts.push(`Conditions: ${s.conditions.join(', ')}`);
    if (s.allergies.length) parts.push(`Allergies: ${s.allergies.map(a => `${a.name} (${a.severity})`).join(', ')}`);
    parts.push(`Medications: ${s.medications.count} active${s.medications.hasIssues && s.medications.issueNote ? ' — ' + s.medications.issueNote : ''}`);
    parts.push(`Lab results: ${s.labResults.status}${s.labResults.abnormalCount > 0 ? ` (${s.labResults.abnormalCount} abnormal)` : ''}`);
    if (s.insights.length) parts.push(`Insights: ${s.insights.join('; ')}`);
    if (s.recommendations.length) parts.push(`Recommendations: ${s.recommendations.join('; ')}`);
    return parts.join('\n');
  }

  navigateToRecords(): void {
    const p = this.selectedProfile();
    if (!p) return;
    this.router.navigate(['/medical-records'], { queryParams: { profileId: p.userHealthProfileId } });
  }

  switchTab(tab: string): void {
    if (tab === this.activeTab()) return;
    const order = ['overview', 'records', 'appointments', 'medications', 'reminders', 'summary'];
    const currentIndex = order.indexOf(this.activeTab());
    const nextIndex = order.indexOf(tab);
    this.animateTabContent(nextIndex >= currentIndex ? 'left' : 'right');

    this.activeTab.set(tab);
    if (tab === 'summary') {
      this.loadHealthSummary();
    }
    this.mobileTabMenuOpen.set(false);
  }

  toggleMobileTabMenu(): void { this.mobileTabMenuOpen.update(v => !v); }
  closeMobileTabMenu(): void  { this.mobileTabMenuOpen.set(false); }

  activeTabLabel(): string {
    const t = this.t().family;
    const map: Record<string, string> = {
      overview:     t.overviewTab || 'Overview',
      records:      t.medicalRecordsTab || 'Medical Records',
      appointments: t.appointmentsTab || 'Appointments',
      medications:  t.medicationsTab || 'Medications',
      reminders:    t.remindersTab || 'Reminders',
      summary:      t.healthSummaryTab || 'Health Summary',
    };
    return map[this.activeTab()] ?? (t.overviewTab || 'Overview');
  }

  activeTabIcon(): string {
    const map: Record<string, string> = {
      overview: 'fa-house',
      records: 'fa-folder-open',
      appointments: 'fa-calendar-check',
      medications: 'fa-pills',
      reminders: 'fa-bell',
      summary: 'fa-chart-simple',
    };
    return map[this.activeTab()] ?? 'fa-house';
  }

  private animateTabContent(direction: 'left' | 'right'): void {
    this.tabDirection.set(direction);
    this.tabAnimating.set(false);
    setTimeout(() => this.tabAnimating.set(true));
    setTimeout(() => this.tabAnimating.set(false), 260);
  }

  // ─── Medications / Reminders ────────────────────────────────
  loadFpMedicines(): void {
    const profileId = this.selectedProfile()?.userHealthProfileId;
    if (!profileId) return;
    this.fpMedicinesLoading.set(true);
    this.http.get<any>(`${this.base}/user-medicines?profileId=${profileId}`).pipe(
      map(r => r.data ?? []),
      catchError(() => of([])),
    ).subscribe(list => {
      this.fpMedicines.set(list);
      this.fpMedicinesLoading.set(false);
    });
  }

  // ─── Carousel ───────────────────────────────────────────────
  scrollCarousel(dir: -1 | 1): void {
    this.carouselElRef?.nativeElement.scrollBy({ left: dir * 300, behavior: 'smooth' });
  }

  // ─── Add modal ──────────────────────────────────────────────
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

  closeAddModal(): void { this.showAddModal.set(false); }
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

  onCreateImageSelected(event: Event): void {
    this.errorMessage = '';
    const input = event.target as HTMLInputElement;
    const file = input.files?.[0] ?? null;
    if (!file) { return; }

    this.validateAndPreviewImage(
      file,
      (f, preview) => { this.createForm.profileImage = f; this.createForm.profileImagePreview = preview; },
      (message) => { this.errorMessage = message; input.value = ''; }
    );
  }

  removeCreateImage(input: HTMLInputElement): void {
    this.createForm.profileImage = null;
    this.createForm.profileImagePreview = null;
    input.value = '';
  }

  onEditImageSelected(event: Event): void {
    this.editError = '';
    const input = event.target as HTMLInputElement;
    const file = input.files?.[0] ?? null;
    if (!file) { return; }

    this.validateAndPreviewImage(
      file,
      (f, preview) => {
        this.editForm.profileImage = f;
        this.editForm.profileImagePreview = preview;
        this.editForm.removeImage = false;
      },
      (message) => { this.editError = message; input.value = ''; }
    );
  }

  removeEditImage(input: HTMLInputElement): void {
    this.editForm.profileImage = null;
    this.editForm.profileImagePreview = null;
    this.editForm.removeImage = true;
    input.value = '';
  }

  submitCreateManaged(): void {
    const f = this.createForm;
    if (!f.firstName.trim() || !f.lastName.trim() || !f.dateOfBirth || !f.gender || !f.relationship) {
      this.errorMessage = 'Please fill in all required fields.';
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
      this.errorMessage = 'Please fill in all required fields.';
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
      this.editError = 'Please fill in all required fields.';
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
          const lastName  = this.editForm.lastName.trim();
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

  // ─── Three-dot menu ─────────────────────────────────────────
  canShowProfileMenu(p: AccessibleProfileDto | null): boolean {
    if (!p) return false;
    return p.profileType === 'Managed' && p.userId === null && p.accessRole === 'Owner';
  }

  toggleProfileMenu(): void { this.showProfileMenu.update(v => !v); }
  closeProfileMenu(): void { this.showProfileMenu.set(false); }

  openDeleteProfileConfirm(): void {
    this.showProfileMenu.set(false);
    this.showDeleteConfirm.set(true);
  }

  closeDeleteConfirm(): void { this.showDeleteConfirm.set(false); }

  confirmDeleteProfile(): void {
    const profile = this.selectedProfile();
    if (!profile) return;
    this.deletingProfile.set(true);
    this.fpSvc.deleteProfile(profile.userHealthProfileId)
      .pipe(catchError(() => of(null)))
      .subscribe(() => {
        this.deletingProfile.set(false);
        this.closeDeleteConfirm();
        this.loadProfiles();
      });
  }

  // ─── Sidebar boilerplate ────────────────────────────────────
  @HostListener('document:keydown.escape')
  onEscape(): void {
    if (this.showDeleteConfirm()) { this.closeDeleteConfirm(); return; }
    if (this.showRemoveConfirm()) { this.closeRemoveConfirm(); return; }
    if (this.showModifyModal()) { this.closeModifyModal(); return; }
    if (this.showSupervisionModal()) { this.closeSupervisionModal(); return; }
    if (this.showProfileMenu()) { this.closeProfileMenu(); return; }
  }

  @HostListener('window:resize')
  onWindowResize(): void { this.applyResponsiveSidebar(); }

  private applyResponsiveSidebar(): void {
    this.sidebarCollapsed.set(window.innerWidth <= 1024);
    if (window.innerWidth > 768) this.mobileSidebarOpen.set(false);
  }

  toggleSidebar(): void { this.sidebarCollapsed.update(v => !v); }
  toggleMobileSidebar(): void { this.mobileSidebarOpen.update(v => !v); }
  toggleDropdown(): void { this.dropdownOpen.update(v => !v); }

  logout(): void { this.dropdownOpen.set(false); this.authSvc.logout().subscribe(); }

  goToMyProfile(): void { this.dropdownOpen.set(false); this.router.navigate(['/my-profile']); }
  openRatingPopup(): void { this.dropdownOpen.set(false); this.reviewTracking.openManually(); }

  @HostListener('document:click', ['$event'])
  onDocumentClick(e: MouseEvent): void {
    if (!(e.target as HTMLElement).closest('.hdr-user')) this.dropdownOpen.set(false);
    if (!(e.target as HTMLElement).closest('.fp-menu-wrap')) this.showProfileMenu.set(false);
  }

  // ─── Display helpers ────────────────────────────────────────
  get todayStr(): string {
    return new Date().toISOString().split('T')[0];
  }

  get displayName(): string {
    const u = this.authSvc.currentUser;
    return u?.firstName?.trim() || u?.email || 'there';
  }

  get userEmail(): string { return this.authSvc.currentUser?.email ?? ''; }

  get avatarUrl(): string { return this.authSvc.avatarUrl; }

  get hasProfileImage(): boolean { return !!this.authSvc.currentUser?.profileImageUrl; }

  get userInitials(): string {
    const u = this.authSvc.currentUser;
    if (!u) return '?';
    const f = (u.firstName ?? '')[0] ?? '';
    const l = (u.lastName ?? '')[0] ?? '';
    return (f + l).toUpperCase() || (u.email ?? '?')[0].toUpperCase();
  }

  get avatarBgColor(): string {
    const palette = ['#0EAFD7', '#7C3AED', '#16A34A', '#EA580C', '#0D9488'];
    const seed = this.displayName || this.userEmail || 'U';
    let h = 0;
    for (let i = 0; i < seed.length; i++) h = seed.charCodeAt(i) + ((h << 5) - h);
    return palette[Math.abs(h) % palette.length];
  }

  getAvatarColor(i: number): string { return this.avatarColors[i % this.avatarColors.length]; }

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

  getStatusLabel(p: AccessibleProfileDto): string {   const key = (p.relationship ?? 'member').toLowerCase();
return p.isSelf ? (this.t().family as any)[key] ?? this.t().family.member : 'Active'; }

  getStatusBadgeClass(p: AccessibleProfileDto): string {
    return p.isSelf ? 'fp-badge fp-badge--green' : 'fp-badge fp-badge--blue';
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

  getAge(dob: string): number {
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

  startPageTour(): void {
    if (this.assistantOrchestrator.tourEngine.isPlaying()) return;
    this.assistantOrchestrator.startTour('family-profiles-tour');
  }
}
