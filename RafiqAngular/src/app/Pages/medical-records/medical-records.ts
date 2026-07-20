import {
  Component, OnInit, signal,
  HostListener,
  computed,
  ElementRef,
  ViewChild,
} from '@angular/core';
import { CommonModule } from '@angular/common';
import { inject } from '@angular/core';
import { RecordsContentComponent } from '../../Components/records-content/records-content';
import { FormsModule } from '@angular/forms';
import { Router, RouterLink, RouterLinkActive, ActivatedRoute } from '@angular/router';
import { HttpClient } from '@angular/common/http';
import { AuthService } from '../../Services/auth-service';
import { ProfileCacheService } from '../../Services/profile-cache.service';
import { AiChatService } from '../../Services/ai-chat.service';
import { MedicalRecordsService, UnifiedMedicalRecord } from '../../Services/medical-records.service';
import { ScanMedicineBoxResponse, AddUserMedicinePayload, CreateReminderPayload } from '../../Modles/dashboard.models';
import { MedicationRemindersService } from '../../Services/medication-reminders.service';
import { environment } from '../../Environments/Environment';
import { PdfService } from '../../Services/pdf.service';
import { HealthProfileService } from '../../Services/health-profile.service';
import { NotificationService } from '../../Services/notification.service';
import { switchMap, catchError, of, map } from 'rxjs';
import { toSignal } from '@angular/core/rxjs-interop';
import { FamilyProfilesService, AccessibleProfileDto } from '../../Services/family-profiles.service';
import { LocalizationService } from '../../Services/localization.service';

export type UploadCardKey = 'lab' | 'prescription' | 'imaging' | 'medicine' | 'general';
type RecordTab = 'all' | UploadCardKey;
type AiStatusFilter = 'all' | 'processed' | 'pending';
type UploadedByFilter = 'all' | 'self' | 'manual' | 'medicine';
type SortOption = 'newest' | 'oldest' | 'az' | 'za';

interface RecordFilters {
  type: RecordTab;
  aiStatus: AiStatusFilter;
  uploadedBy: UploadedByFilter;
  fromDate: string;
  toDate: string;
  sortBy: SortOption;
}

interface UploadState {
  uploading: boolean;
  progress: number;
  indeterminate: boolean;
}

const defaultUploadState = (): UploadState => ({
  uploading: false,
  progress: 0,
  indeterminate: false,
});

export interface ReviewLabResult {
  id: string;
  testName: string;
  value: string;
  unit: string;
  normalRange: string;
  status: string;
}

export interface ReviewForm {
  type: 'lab' | 'imaging' | 'prescription' | 'general';
  mode: 'create' | 'edit';
  recordId?: string;
  imagePath: string;
  title: string;
  description: string;
  documentType: string;
  hospitalOrClinic: string;
  documentDate: string;
  summary: string;
  ocrText: string;
  labName: string;
  doctorName: string;
  reportDate: string;
  results: ReviewLabResult[];
  imagingType: string;
  bodyPart: string;
  findings: string;
  impression: string;
  patientName: string;
  prescriptionDate: string;
  prescriptionMedicines: Array<{
    medicineName: string;
    dosage: string;
    frequency: string;
    duration: string;
    instructions: string;
  }>;
  rawResponse: any;
}

interface ScanForm {
  medicineName: string;
  dosage: string;
  dosageForm: string;
  manufacturer: string;
  frequency: string;
  duration: string;
  notes: string;
  imagePath: string;
}

type RepeatOption = 'Once' | 'Daily' | 'Weekly' | 'Monthly';

interface ReminderForm {
  reminderTimes: string[];
  repeatType: RepeatOption;
  startDate: string;
  endDate: string;
  notificationsEnabled: boolean;
  notes: string;
}

interface GeneralUploadForm {
  description: string;
  file: File | null;
  fileName: string;
}

export interface Toast {
  id: number;
  message: string;
  type: 'success' | 'error';
}

const PAGE_SIZE = 5;
const FILTER_SORT_STORAGE_KEY = 'rafiq-medical-records-sort';

const defaultFilters = (sortBy: SortOption = 'newest'): RecordFilters => ({
  type: 'all',
  aiStatus: 'all',
  uploadedBy: 'all',
  fromDate: '',
  toDate: '',
  sortBy,
});

@Component({
  selector: 'app-medical-records',
  standalone: true,
  imports: [CommonModule, RouterLink, RouterLinkActive, RecordsContentComponent],
  templateUrl: './medical-records.html',
  styleUrl: './medical-records.css',
})
export class MedicalRecords implements OnInit {
  protected readonly l10n = inject(LocalizationService);
  protected readonly aiChatService = inject(AiChatService);
  protected readonly t = this.l10n.t;

  private readonly authService    = inject(AuthService);
  protected readonly profileCache = inject(ProfileCacheService);
  private readonly recordsService = inject(MedicalRecordsService);
  private readonly reminderSvc = inject(MedicationRemindersService);
  private readonly healthProfileSvc = inject(HealthProfileService);
  readonly notificationSvc = inject(NotificationService);
  private readonly http = inject(HttpClient);
  private readonly router = inject(Router);
  private readonly route = inject(ActivatedRoute);
  private readonly fpSvc = inject(FamilyProfilesService);
  private readonly base = environment.apiUrl;

  readonly viewingProfile = toSignal<AccessibleProfileDto | null>(
    this.route.queryParamMap.pipe(
      map(params => params.get('profileId')),
      switchMap(profileId => {
        if (!profileId) return of(null);
        return this.fpSvc.getAccessible().pipe(
          map(profiles => profiles.find(p => p.userHealthProfileId === profileId) ?? null),
          catchError(() => of(null))
        );
      })
    ),
    { initialValue: null }
  );

  readonly contextProfileId = computed(() => this.viewingProfile()?.userHealthProfileId ?? undefined);
  readonly contextProfileName = computed(() => {
    const p = this.viewingProfile();
    return p ? `${p.firstName} ${p.lastName}` : null;
  });
  readonly contextReadOnly = computed(() => this.viewingProfile()?.accessRole === 'Viewer');

  @ViewChild('labInput') labInput?: ElementRef<HTMLInputElement>;
  @ViewChild('prescriptionInput') prescriptionInput?: ElementRef<HTMLInputElement>;
  @ViewChild('imagingInput') imagingInput?: ElementRef<HTMLInputElement>;
  @ViewChild('medicineInput') medicineInput?: ElementRef<HTMLInputElement>;

  readonly loading = signal(false);
  readonly allRecords = signal<UnifiedMedicalRecord[]>([]);
  readonly searchQuery = signal('');
  readonly activeTab = signal<RecordTab>('all');

  readonly sidebarCollapsed = signal(false);
  readonly mobileSidebarOpen = signal(false);

  readonly dropdownOpen = signal(false);
  readonly selectedRecord = signal<UnifiedMedicalRecord | null>(null);
  readonly addRecordMenuOpen = signal(false);
  readonly filterMenuOpen = signal(false);
  readonly actionMenuOpen = signal<string | null>(null);
  readonly deleteTarget = signal<UnifiedMedicalRecord | null>(null);
  readonly deleting = signal(false);
  readonly tabDirection = signal<'left' | 'right'>('left');
  readonly tabAnimating = signal(false);
  readonly unreadCount = this.notificationSvc.unreadCount;

  readonly lightboxUrl = signal<string | null>(null);
  readonly detailImageFailed = signal(false);
  readonly reviewImageFailed = signal(false);
  readonly scanImageFailed = signal(false);

  readonly appliedFilters = signal<RecordFilters>(defaultFilters(this.getSavedSortOption()));
  readonly draftFilters = signal<RecordFilters>(defaultFilters(this.getSavedSortOption()));
  readonly recordTypeOptions: Array<{ value: RecordTab; label: string }> = [
    { value: 'all', label: 'All' },
    { value: 'lab', label: 'Lab Analysis' },
    { value: 'prescription', label: 'Prescription' },
    { value: 'imaging', label: 'X-Ray & Imaging' },
    { value: 'medicine', label: 'Medicine Box' },
    { value: 'general', label: 'Other Medical Document' },
  ];
  readonly aiStatusOptions: Array<{ value: AiStatusFilter; label: string }> = [
    { value: 'all', label: 'All' },
    { value: 'processed', label: 'Processed' },
    { value: 'pending', label: 'Pending' },
  ];
  readonly uploadedByOptions: Array<{ value: UploadedByFilter; label: string }> = [
    { value: 'all', label: 'All' },
    { value: 'self', label: 'Self' },
    { value: 'manual', label: 'Manual' },
    { value: 'medicine', label: 'Medicine Box' },
  ];
  readonly sortOptions: Array<{ value: SortOption; label: string }> = [
    { value: 'newest', label: 'Newest First' },
    { value: 'oldest', label: 'Oldest First' },
    { value: 'az', label: 'A-Z' },
    { value: 'za', label: 'Z-A' },
  ];

  readonly uploadState = signal<Record<UploadCardKey, UploadState>>({
    lab: defaultUploadState(),
    prescription: defaultUploadState(),
    imaging: defaultUploadState(),
    medicine: defaultUploadState(),
    general: defaultUploadState(),
  });

  readonly uploadLoading = signal(false);
  readonly uploadLoadingLabel = signal('');
  readonly reviewForm = signal<ReviewForm | null>(null);
  readonly reviewSaving = signal(false);
  readonly generalUploadFormOpen = signal(false);
  generalUploadForm: GeneralUploadForm = this.emptyGeneralUploadForm();

  readonly scanLoading = signal(false);
  readonly scanResult = signal<ScanMedicineBoxResponse | null>(null);
  readonly scanSaving = signal(false);
  readonly scanMode = signal<'create' | 'edit'>('create');
  readonly scanRecordId = signal<string | null>(null);
  readonly scanSavedMedicineName = signal<string | null>(null);
  readonly scanSavedMedicineId = signal<string | null>(null);
  scanForm: ScanForm = this.emptyScanForm();

  readonly showReminderModal = signal(false);
  readonly reminderMedicineId = signal<string | null>(null);
  readonly reminderMedicineName = signal<string | null>(null);
  readonly reminderSaving = signal(false);
  reminderForm: ReminderForm = this.emptyReminderForm();

  readonly repeatOptions: Array<{ value: RepeatOption; label: string }> = [
    { value: 'Once', label: 'Once' },
    { value: 'Daily', label: 'Daily' },
    { value: 'Weekly', label: 'Weekly' },
    { value: 'Monthly', label: 'Monthly' },
  ];

  get reminderFormErrors(): string[] {
    const errors: string[] = [];
    const filledTimes = this.reminderForm.reminderTimes.filter(t => t.trim());
    if (filledTimes.length === 0) {
      errors.push('At least one reminder time is required.');
    } else if (new Set(filledTimes).size < filledTimes.length) {
      errors.push('Reminder times cannot be duplicated.');
    }
    if (!this.reminderForm.startDate) errors.push('Start date is required.');
    if (this.reminderForm.repeatType !== 'Once') {
      if (!this.reminderForm.endDate) errors.push('End date is required.');
      else if (this.reminderForm.startDate && this.reminderForm.endDate < this.reminderForm.startDate) {
        errors.push('End date cannot be before start date.');
      }
    }
    return errors;
  }

  get reminderFormValid(): boolean {
    return this.reminderFormErrors.length === 0;
  }

  readonly currentPage = signal(1);
  readonly pageSize = PAGE_SIZE;

  readonly toasts = signal<Toast[]>([]);
  private toastCounter = 0;

  private _searchQuery = '';
  get searchQueryValue(): string { return this._searchQuery; }
  set searchQueryValue(v: string) {
    this._searchQuery = v;
    this.searchQuery.set(v);
    this.currentPage.set(1);
  }

  get displayName(): string {
    const u = this.authService.currentUser;
    if (!u) return 'there';
    return u.firstName?.trim() || u.email;
  }

  get userEmail(): string { return this.authService.currentUser?.email ?? ''; }

  get avatarUrl(): string { return this.authService.avatarUrl; }

  get hasProfileImage(): boolean { return !!this.authService.currentUser?.profileImageUrl; }

  get userInitials(): string {
    const u = this.authService.currentUser;
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

  @HostListener('window:resize')
  onWindowResize(): void { this.applyResponsiveSidebar(); }
  readonly countedRecords = computed(() =>
    this.applySmartFilters(this.allRecords(), this.appliedFilters())
  );

  readonly labCount = computed(() => this.countedRecords().filter(r => r.type === 'lab').length);
  readonly prescriptionCount = computed(() => this.countedRecords().filter(r => r.type === 'prescription').length);
  readonly imagingCount = computed(() => this.countedRecords().filter(r => r.type === 'imaging').length);
  readonly medicineCount = computed(() => this.countedRecords().filter(r => r.type === 'medicine').length);
  readonly generalCount = computed(() => this.countedRecords().filter(r => r.type === 'general').length);

  readonly filteredRecords = computed(() =>
    this.sortRecords(this.countedRecords(), this.appliedFilters().sortBy)
  );

  readonly activeFilterChips = computed(() => {
    const filters = this.appliedFilters();
    const chips: Array<{ key: keyof RecordFilters; label: string }> = [];
    if (filters.type !== 'all') chips.push({ key: 'type', label: this.typeLabel(filters.type) });
    if (filters.aiStatus !== 'all') chips.push({ key: 'aiStatus', label: filters.aiStatus === 'processed' ? 'Processed' : 'Pending' });
    if (filters.uploadedBy !== 'all') chips.push({ key: 'uploadedBy', label: this.uploadedByLabel(filters.uploadedBy) });
    if (filters.fromDate) chips.push({ key: 'fromDate', label: `From ${filters.fromDate}` });
    if (filters.toDate) chips.push({ key: 'toDate', label: `To ${filters.toDate}` });
    if (filters.sortBy !== 'newest') chips.push({ key: 'sortBy', label: this.sortLabel(filters.sortBy) });
    return chips;
  });

  readonly pagedRecords = computed(() => {
    const all = this.filteredRecords();
    const start = (this.currentPage() - 1) * this.pageSize;
    return all.slice(start, start + this.pageSize);
  });

  readonly totalPages = computed(() =>
    Math.max(1, Math.ceil(this.filteredRecords().length / this.pageSize))
  );

  readonly pageNumbers = computed(() => {
    const total = this.totalPages();
    const cur = this.currentPage();
    const pages: (number | '...')[] = [];
    if (total <= 7) {
      for (let i = 1; i <= total; i++) pages.push(i);
    } else {
      pages.push(1);
      if (cur > 3) pages.push('...');
      for (let i = Math.max(2, cur - 1); i <= Math.min(total - 1, cur + 1); i++) pages.push(i);
      if (cur < total - 2) pages.push('...');
      pages.push(total);
    }
    return pages;
  });

  ngOnInit(): void {
    this.profileCache.ensure();
    this.applyResponsiveSidebar();
    this.loadData();
  }
  ngOnDestroy(): void { }

  @HostListener('document:keydown.escape')
  onEsc(): void {
    if (this.lightboxUrl()) { this.lightboxUrl.set(null); return; }
    if (this.deleteTarget() && !this.deleting()) { this.closeDeleteModal(); return; }
    if (this.showReminderModal()) { this.closeReminderModal(); return; }
    if (this.scanSavedMedicineName()) { this.dismissScanSuccess(); return; }
    if (this.scanResult()) { this.cancelScanReview(); return; }
    if (this.reviewForm() && !this.reviewSaving()) { this.cancelReview(); return; }
    if (this.generalUploadFormOpen() && !this.uploadLoading()) { this.cancelGeneralUpload(); return; }
    if (this.selectedRecord()) { this.closeDetails(); return; }
    this.addRecordMenuOpen.set(false);
    this.filterMenuOpen.set(false);
    this.actionMenuOpen.set(null);
    this.dropdownOpen.set(false);
  }

  @HostListener('document:click', ['$event'])
  onDocumentClick(event: MouseEvent): void {
    if (!(event.target as HTMLElement).closest('.hdr-user')) {
      this.dropdownOpen.set(false);
    }
  }

  private applyResponsiveSidebar(): void {
    this.sidebarCollapsed.set(window.innerWidth <= 1024);
    if (window.innerWidth > 768) this.mobileSidebarOpen.set(false);
  }

  toggleSidebar(): void { this.sidebarCollapsed.update(v => !v); }
  toggleMobileSidebar(): void { this.mobileSidebarOpen.update(v => !v); }
  toggleDropdown(): void { this.dropdownOpen.update(v => !v); }
  logout(): void { this.dropdownOpen.set(false); this.authService.logout().subscribe(); }

  goToMyProfile(): void { this.dropdownOpen.set(false); this.router.navigate(['/my-profile']); }

  toggleAddRecordMenu(event: MouseEvent): void {
    event.stopPropagation();
    this.filterMenuOpen.set(false);
    this.addRecordMenuOpen.update(v => !v);
  }

  selectUploadType(type: 'Lab Analysis' | 'Prescription' | 'X-Ray & Imaging' | 'Medicine Box' | 'General Medical Document'): void {
    this.addRecordMenuOpen.set(false);
    this.triggerUpload(type);
  }

  toggleFilterMenu(event: MouseEvent): void {
    event.stopPropagation();
    this.addRecordMenuOpen.set(false);
    this.draftFilters.set({ ...this.appliedFilters() });
    this.filterMenuOpen.update(v => !v);
  }

  updateDraftFilter(key: keyof RecordFilters, value: string): void {
    this.draftFilters.update(filters => ({ ...filters, [key]: value } as RecordFilters));
  }

  applyFilters(): void {
    const next = { ...this.draftFilters() };
    this.appliedFilters.set(next);
    this.saveSortOption(next.sortBy);
    this.filterMenuOpen.set(false);
    this.currentPage.set(1);
  }

  resetFilters(): void {
    const reset = defaultFilters(this.appliedFilters().sortBy);
    this.draftFilters.set(reset);
    this.appliedFilters.set(reset);
    this.currentPage.set(1);
  }

  clearAllFilters(): void {
    this.resetFilters();
    this.filterMenuOpen.set(false);
  }

  removeFilterChip(key: keyof RecordFilters): void {
    const current = this.appliedFilters();
    const next = { ...current };
    if (key === 'type') next.type = 'all';
    if (key === 'aiStatus') next.aiStatus = 'all';
    if (key === 'uploadedBy') next.uploadedBy = 'all';
    if (key === 'fromDate') next.fromDate = '';
    if (key === 'toDate') next.toDate = '';
    if (key === 'sortBy') next.sortBy = 'newest';
    this.appliedFilters.set(next);
    this.draftFilters.set(next);
    this.saveSortOption(next.sortBy);
    this.currentPage.set(1);
  }

  loadData(): void {
    this.loading.set(true);
    this.recordsService.getAllData().subscribe({
      next: res => {
        this.allRecords.set(this.recordsService.toUnifiedRecords(res));
        this.currentPage.set(Math.min(this.currentPage(), this.totalPages()));
        this.loading.set(false);
      },
      error: () => {
        this.allRecords.set([]);
        this.loading.set(false);
      },
    });
  }

  private applySmartFilters(records: UnifiedMedicalRecord[], filters: RecordFilters): UnifiedMedicalRecord[] {
    return records.filter(record => {
      if (filters.type !== 'all' && record.type !== filters.type) return false;
      if (filters.aiStatus === 'processed' && !record.hasAiSummary) return false;
      if (filters.aiStatus === 'pending' && record.hasAiSummary) return false;
      if (!this.matchesUploadedBy(record, filters.uploadedBy)) return false;

      const recordTime = this.getRecordTime(record);
      if (filters.fromDate && recordTime < new Date(`${filters.fromDate}T00:00:00`).getTime()) return false;
      if (filters.toDate && recordTime > new Date(`${filters.toDate}T23:59:59`).getTime()) return false;
      return true;
    });
  }

  private sortRecords(records: UnifiedMedicalRecord[], sortBy: SortOption): UnifiedMedicalRecord[] {
    const sorted = [...records];
    return sorted.sort((a, b) => {
      if (sortBy === 'az') return a.name.localeCompare(b.name);
      if (sortBy === 'za') return b.name.localeCompare(a.name);
      const diff = this.getRecordTime(b) - this.getRecordTime(a);
      return sortBy === 'newest' ? diff : -diff;
    });
  }

  private matchesUploadedBy(record: UnifiedMedicalRecord, uploadedBy: UploadedByFilter): boolean {
    if (uploadedBy === 'all') return true;
    const source = `${record.uploadedBy ?? ''} ${record.rawRecord?.source ?? ''}`.toLowerCase();
    if (uploadedBy === 'self') return source.includes('self');
    if (uploadedBy === 'manual') return source.includes('manual');
    return source.includes('medicine') || record.type === 'medicine';
  }

  private getRecordTime(record: UnifiedMedicalRecord): number {
    const rawDate =
      record.rawRecord?.createdAt ??
      record.rawRecord?.reportDate ??
      record.rawRecord?.prescriptionDate ??
      record.date;
    const time = new Date(rawDate).getTime();
    return Number.isNaN(time) ? 0 : time;
  }

  private getSavedSortOption(): SortOption {
    try {
      const saved = localStorage.getItem(FILTER_SORT_STORAGE_KEY) as SortOption | null;
      return saved && ['newest', 'oldest', 'az', 'za'].includes(saved) ? saved : 'newest';
    } catch {
      return 'newest';
    }
  }

  private saveSortOption(sortBy: SortOption): void {
    try {
      localStorage.setItem(FILTER_SORT_STORAGE_KEY, sortBy);
    } catch { }
  }

  typeLabel(type: RecordTab): string {
    const labels: Record<RecordTab, string> = {
      all: 'All',
      lab: 'Lab Analysis',
      prescription: 'Prescription',
      imaging: 'X-Ray & Imaging',
      medicine: 'Medicine Box',
      general: 'Other Medical Document',
    };
    return labels[type];
  }

  uploadedByLabel(uploadedBy: UploadedByFilter): string {
    const labels: Record<UploadedByFilter, string> = {
      all: 'All',
      self: 'Self',
      manual: 'Manual',
      medicine: 'Medicine Box',
    };
    return labels[uploadedBy];
  }

  sortLabel(sortBy: SortOption): string {
    const labels: Record<SortOption, string> = {
      newest: 'Newest First',
      oldest: 'Oldest First',
      az: 'A-Z',
      za: 'Z-A',
    };
    return labels[sortBy];
  }

  goToPage(p: number | '...'): void {
    if (p === '...') return;
    const nextPage = Math.max(1, Math.min(p, this.totalPages()));
    if (nextPage === this.currentPage()) return;

    this.animateTable(nextPage > this.currentPage() ? 'left' : 'right');
    this.currentPage.set(nextPage);
  }

  prevPage(): void {
    if (this.currentPage() > 1) {
      this.animateTable('right');
      this.currentPage.update(p => p - 1);
    }
  }

  nextPage(): void {
    if (this.currentPage() < this.totalPages()) {
      this.animateTable('left');
      this.currentPage.update(p => p + 1);
    }
  }

  setTab(tab: RecordTab): void {
    if (tab === this.activeTab()) return;
    const order: RecordTab[] = ['all', 'lab', 'prescription', 'imaging', 'medicine', 'general'];
    const currentIndex = order.indexOf(this.activeTab());
    const nextIndex = order.indexOf(tab);
    this.animateTable(nextIndex >= currentIndex ? 'left' : 'right');
    this.activeTab.set(tab);
    this.currentPage.set(1);
  }

  private animateTable(direction: 'left' | 'right'): void {
    this.tabDirection.set(direction);
    this.tabAnimating.set(false);
    setTimeout(() => this.tabAnimating.set(true));
    setTimeout(() => this.tabAnimating.set(false), 260);
  }

  getRecordIcon(type: string): string {
    const m: Record<string, string> = {
      lab: 'fa-flask',
      imaging: 'fa-x-ray',
      prescription: 'fa-prescription-bottle-medical',
      medicine: 'fa-pills',
      general: 'fa-file-medical',
    };
    return m[type] ?? 'fa-file-medical';
  }

  getRecordIconClass(type: string): string {
    const m: Record<string, string> = {
      lab: 'rec-ico-blue',
      imaging: 'rec-ico-teal',
      prescription: 'rec-ico-purple',
      medicine: 'rec-ico-orange',
      general: 'rec-ico-gray',
    };
    return m[type] ?? 'rec-ico-gray';
  }

  viewDetails(record: UnifiedMedicalRecord): void {
    this.detailImageFailed.set(false);
    this.selectedRecord.set(record);
  }

  toggleActionMenu(recordId: string, event: MouseEvent): void {
    event.stopPropagation();
    this.actionMenuOpen.update(openId => openId === recordId ? null : recordId);
  }

  openDeleteModal(record: UnifiedMedicalRecord): void {
    this.actionMenuOpen.set(null);
    this.deleteTarget.set(record);
  }

  closeDeleteModal(): void {
    if (this.deleting()) return;
    this.deleteTarget.set(null);
  }

  deleteRecord(): void {
    const record = this.deleteTarget();
    if (!record) return;
    this.deleting.set(true);
    this.recordsService.deleteRecord(record).subscribe({
      next: () => {
        this.deleting.set(false);
        this.deleteTarget.set(null);
        this.showToast('Record deleted successfully.', 'success');
        this.loadData();
      },
      error: err => {
        this.deleting.set(false);
        this.showToast(err?.error?.message || 'Failed to delete record. Please try again.', 'error');
      },
    });
  }

  editRecord(record: UnifiedMedicalRecord): void {
    if (record.type === 'medicine') {
      this.actionMenuOpen.set(null);
      this.scanForm = {
        medicineName: record.rawRecord.medicineName ?? '',
        dosage: record.rawRecord.dosage ?? '',
        dosageForm: record.rawRecord.dosageForm ?? '',
        manufacturer: record.rawRecord.manufacturer ?? '',
        frequency: record.rawRecord.frequency ?? '',
        duration: record.rawRecord.duration ?? '',
        notes: record.rawRecord.notes ?? '',
        imagePath: record.rawRecord.imagePath ?? '',
      };
      this.scanImageFailed.set(false);
      this.scanMode.set('edit');
      this.scanRecordId.set(record.id);
      this.scanResult.set({
        medicineName: this.scanForm.medicineName,
        strength: this.scanForm.dosage,
        dosageForm: this.scanForm.dosageForm,
        manufacturer: this.scanForm.manufacturer,
        imagePath: this.scanForm.imagePath,
      });
      return;
    }
    this.actionMenuOpen.set(null);
    this.selectedRecord.set(null);
    this.openReviewModal(record.type, record.rawRecord, 'edit', record.id);
  }

  private readonly pdfService = inject(PdfService);

  closeDetails(): void { this.selectedRecord.set(null); }
  downloadRecord(record: UnifiedMedicalRecord): void { this.pdfService.download(record) }

  getImageUrl(rawUrl: string | undefined | null): string | null {
    if (!rawUrl) return null;
    if (/^https?:\/\//i.test(rawUrl)) return rawUrl;
    const serverOrigin = this.base.replace(/\/api\/?$/i, '');
    return `${serverOrigin}/${rawUrl.replace(/^\/+/, '')}`;
  }

  getRecordImageUrl(record: UnifiedMedicalRecord): string | null {
    return this.getImageUrl(record.rawRecord?.imageUrl ?? record.rawRecord?.imagePath ?? null);
  }

  openLightbox(url: string): void { this.lightboxUrl.set(url); }
  closeLightbox(): void { this.lightboxUrl.set(null); }

  triggerUpload(type: string): void {
    if (type === 'General Medical Document') {
      this.openGeneralUploadForm();
      return;
    }

    const map: Record<string, ElementRef<HTMLInputElement> | undefined> = {
      'Lab Analysis': this.labInput,
      'Prescription': this.prescriptionInput,
      'X-Ray & Imaging': this.imagingInput,
      'Medicine Box': this.medicineInput,
    };
    (map[type] ?? this.labInput)?.nativeElement.click();
  }

  onLabFileSelected(e: Event): void {
    const f = this.extractFile(e);
    if (f) this.uploadAndReview('lab', f);
  }

  onPrescriptionFileSelected(e: Event): void {
    const f = this.extractFile(e);
    if (f) this.uploadAndReview('prescription', f);
  }

  onImagingFileSelected(e: Event): void {
    const f = this.extractFile(e);
    if (f) this.uploadAndReview('imaging', f);
  }

  onMedicineFileSelected(e: Event): void {
    const f = this.extractFile(e);
    if (f) this.startMedicineScan(f);
  }

  onGeneralFileSelected(e: Event): void {
    const f = this.extractFile(e);
    if (!f) return;
    this.generalUploadForm.file = f;
    this.generalUploadForm.fileName = f.name;
  }

  openGeneralUploadForm(): void {
    this.generalUploadForm = this.emptyGeneralUploadForm();
    this.generalUploadFormOpen.set(true);
  }

  cancelGeneralUpload(): void {
    if (this.uploadLoading()) return;
    this.generalUploadForm = this.emptyGeneralUploadForm();
    this.generalUploadFormOpen.set(false);
  }

  submitGeneralUpload(): void {
    const file = this.generalUploadForm.file;
    if (!file) {
      this.showToast('Please choose an image before uploading.', 'error');
      return;
    }
    this.uploadAndReview('general', file, this.generalUploadForm.description);
  }

  private extractFile(event: Event): File | null {
    const input = event.target as HTMLInputElement;
    const file = input.files?.[0] ?? null;
    input.value = '';
    return file;
  }

  private uploadAndReview(type: 'lab' | 'imaging' | 'prescription' | 'general', file: File, description = ''): void {
    const urls: Record<'lab' | 'imaging' | 'prescription' | 'general', string> = {
      lab: `${this.base}/documents/upload/lab`,
      imaging: `${this.base}/documents/upload/imaging`,
      prescription: `${this.base}/prescriptions/upload`,
      general: `${this.base}/documents/general/upload`,
    };
    const labels: Record<'lab' | 'imaging' | 'prescription' | 'general', string> = {
      lab: 'Analysing Lab Report...',
      imaging: 'Analysing Imaging Report...',
      prescription: 'Extracting Prescription...',
      general: 'Analysing Medical Document...',
    };

    this.uploadLoading.set(true);
    this.uploadLoadingLabel.set(labels[type]);
    this.setUploading(type, true);

    const form = new FormData();
    form.append('image', file);
    if (type === 'general') {
      form.append('description', description.trim());
    }

    this.http.post<{ data: any }>(urls[type], form).subscribe({
      next: res => {
        this.uploadLoading.set(false);
        this.setUploading(type, false);
        const data = res?.data ?? (res as any);
        if (type === 'general') {
          data.description = description.trim();
          this.generalUploadFormOpen.set(false);
          this.generalUploadForm = this.emptyGeneralUploadForm();
        }
        this.openReviewModal(type, data);
      },
      error: err => {
        this.uploadLoading.set(false);
        this.setUploading(type, false);
        const errCode = err?.error?.errorCode as string | undefined;
        const v = this.t().uploadValidation;
        if (errCode === 'WRONG_DOCUMENT_TYPE_LAB_REPORT') {
          this.showToast(v.lab, 'error');
        } else if (errCode === 'UNREADABLE_DOCUMENT_LAB_REPORT') {
          this.showToast(v.labUnreadable, 'error');
        } else if (errCode === 'WRONG_DOCUMENT_TYPE_IMAGING_REPORT') {
          this.showToast(v.imaging, 'error');
        } else if (errCode === 'UNREADABLE_DOCUMENT_IMAGING_REPORT') {
          this.showToast(v.imagingUnreadable, 'error');
        } else if (errCode === 'WRONG_DOCUMENT_TYPE_PRESCRIPTION') {
          this.showToast(v.prescription, 'error');
        } else if (errCode === 'UNREADABLE_DOCUMENT_PRESCRIPTION') {
          this.showToast(v.prescriptionUnreadable, 'error');
        } else {
          this.showToast(err?.error?.message || 'Upload failed. Please try again.', 'error');
        }
      },
    });
  }

  private openReviewModal(
    type: 'lab' | 'imaging' | 'prescription' | 'general',
    data: any,
    mode: 'create' | 'edit' = 'create',
    recordId?: string
  ): void {
    const form: ReviewForm = {
      type,
      mode,
      recordId,
      imagePath: data.imageUrl ?? data.imagePath ?? '',
      title: data.title ?? data.documentTitle ?? '',
      description: data.description ?? '',
      documentType: data.documentType ?? '',
      hospitalOrClinic: data.hospitalOrClinic ?? '',
      documentDate: data.documentDate ?? data.reportDate ?? '',
      summary: data.summary ?? data.aiSummary ?? '',
      ocrText: data.ocrText ?? '',
      labName: data.labName ?? '',
      doctorName: data.doctorName ?? '',
      reportDate: data.reportDate ?? data.prescriptionDate ?? '',
      results: (data.results ?? data.labResults ?? []).map((r: any) => ({
        id: r.id ?? crypto.randomUUID(),
        testName: r.testName ?? '',
        value: r.value ?? '',
        unit: r.unit ?? '',
        normalRange: r.normalRange ?? r.referenceRange ?? '',
        status: r.status ?? 'Normal',
      })),
      imagingType: data.imagingType ?? '',
      bodyPart: data.bodyPart ?? '',
      findings: data.findings ?? '',
      impression: data.impression ?? '',
      patientName: data.patientName ?? '',
      prescriptionDate: data.prescriptionDate ?? '',
      prescriptionMedicines: (data.medicines ?? data.prescriptionMedicines ?? []).map((m: any) => ({
        medicineName: m.medicineName ?? '',
        dosage: m.dosage ?? '',
        frequency: m.frequency ?? '',
        duration: m.duration ?? '',
        instructions: m.instructions ?? m.notes ?? '',
      })),
      rawResponse: data,
    };
    this.reviewImageFailed.set(false);
    this.reviewForm.set(form);
  }

  addLabResult(): void {
    const rf = this.reviewForm();
    if (!rf) return;
    rf.results = [
      ...rf.results,
      { id: crypto.randomUUID(), testName: '', value: '', unit: '', normalRange: '', status: 'Normal' },
    ];
    this.reviewForm.set({ ...rf });
  }

  removeLabResult(id: string): void {
    const rf = this.reviewForm();
    if (!rf) return;
    rf.results = rf.results.filter(r => r.id !== id);
    this.reviewForm.set({ ...rf });
  }

  addPrescriptionMed(): void {
    const rf = this.reviewForm();
    if (!rf) return;
    rf.prescriptionMedicines = [
      ...rf.prescriptionMedicines,
      { medicineName: '', dosage: '', frequency: '', duration: '', instructions: '' },
    ];
    this.reviewForm.set({ ...rf });
  }

  removePrescriptionMed(i: number): void {
    const rf = this.reviewForm();
    if (!rf) return;
    rf.prescriptionMedicines = rf.prescriptionMedicines.filter((_, idx) => idx !== i);
    this.reviewForm.set({ ...rf });
  }

  cancelReview(): void {
    this.reviewSaving.set(false);
    this.reviewForm.set(null);
  }

  confirmAndSave(): void {
    const rf = this.reviewForm();
    if (!rf) return;
    this.reviewSaving.set(true);

    let request$;
    if (rf.type === 'lab') {
      const payload = {
        labName: rf.labName,
        doctorName: rf.doctorName,
        reportDate: rf.reportDate,
        summary: rf.summary,
        ocrText: rf.ocrText,
        imageUrl: rf.imagePath,
        results: rf.results.map(r => ({
          testName: r.testName,
          value: r.value,
          unit: r.unit,
          normalRange: r.normalRange,
          status: r.status,
        })),
      };
      request$ = rf.mode === 'edit' && rf.recordId
        ? this.http.put(`${this.base}/documents/labs/${rf.recordId}`, payload)
        : this.http.post(`${this.base}/documents/labs`, payload);
    } else if (rf.type === 'imaging') {
      const payload = {
        imagingType: rf.imagingType,
        bodyPart: rf.bodyPart,
        findings: rf.findings,
        impression: rf.impression,
        doctorName: rf.doctorName,
        reportDate: rf.reportDate,
        summary: rf.summary,
        ocrText: rf.ocrText,
        imageUrl: rf.imagePath,
      };
      request$ = rf.mode === 'edit' && rf.recordId
        ? this.http.put(`${this.base}/documents/imaging/${rf.recordId}`, payload)
        : this.http.post(`${this.base}/documents/imaging`, payload);
    } else if (rf.type === 'prescription') {
      const payload = {
        doctorName: rf.doctorName,
        patientName: rf.patientName,
        prescriptionDate: rf.prescriptionDate,
        imagePath: rf.imagePath,
        medicines: rf.prescriptionMedicines.map(m => ({
          medicineName: m.medicineName,
          dosage: m.dosage,
          frequency: m.frequency,
          duration: m.duration,
          instructions: m.instructions,
        })),
      };
      request$ = rf.mode === 'edit' && rf.recordId
        ? this.http.put(`${this.base}/prescriptions/${rf.recordId}`, payload)
        : this.http.post(`${this.base}/prescriptions`, payload);
    } else {
      const payload = {
        title: rf.title,
        description: rf.description,
        aiSummary: rf.summary,
        imagePath: rf.imagePath,
      };
      request$ = rf.mode === 'edit' && rf.recordId
        ? this.http.put(`${this.base}/documents/general/${rf.recordId}`, payload)
        : this.http.post(`${this.base}/documents/general`, payload);
    }

    request$.subscribe({
      next: () => {
        this.reviewSaving.set(false);
        this.reviewForm.set(null);
        this.showToast(rf.mode === 'edit' ? 'Record updated successfully.' : 'Record confirmed and saved.', 'success');
        this.loadData();
      },
      error: err => {
        this.reviewSaving.set(false);
        this.showToast(err?.error?.message || 'Failed to save record. Please try again.', 'error');
      },
    });
  }

  private startMedicineScan(file: File): void {
    this.scanLoading.set(true);
    this.setUploading('medicine', true);
    const form = new FormData();
    form.append('image', file);

    this.http.post<{ data: ScanMedicineBoxResponse }>(`${this.base}/user-medicines/scan-box`, form).subscribe({
      next: res => {
        this.scanLoading.set(false);
        this.setUploading('medicine', false);
        const data = res?.data ?? (res as unknown as ScanMedicineBoxResponse);
        this.scanForm = {
          medicineName: data.medicineName ?? '',
          dosage: data.strength ?? '',
          dosageForm: data.dosageForm ?? '',
          manufacturer: data.manufacturer ?? '',
          frequency: '',
          duration: '',
          notes: '',
          imagePath: data.imagePath ?? '',
        };
        this.scanImageFailed.set(false);
        this.scanMode.set('create');
        this.scanRecordId.set(null);
        this.scanResult.set(data);
      },
      error: err => {
        this.scanLoading.set(false);
        this.setUploading('medicine', false);
        const errCode = err?.error?.errorCode as string | undefined;
        const v = this.t().uploadValidation;
        if (errCode === 'WRONG_DOCUMENT_TYPE_MEDICINE_BOX') {
          this.showToast(v.medicine, 'error');
        } else if (errCode === 'UNREADABLE_DOCUMENT_MEDICINE_BOX') {
          this.showToast(v.medicineUnreadable, 'error');
        } else {
          this.showToast(err?.error?.message || 'Scan failed. Please try again.', 'error');
        }
      },
    });
  }

  private resetScanState(): void {
    this.scanResult.set(null);
    this.scanMode.set('create');
    this.scanRecordId.set(null);
    this.scanForm = this.emptyScanForm();
    this.scanSavedMedicineName.set(null);
    this.scanSavedMedicineId.set(null);
  }

  cancelScanReview(): void {
    this.resetScanState();
  }

  dismissScanSuccess(): void {
    this.resetScanState();
    this.router.navigate(['/medications'], { queryParams: { tab: 'medications' } });
  }

  openReminderFromScan(): void {
    const id = this.scanSavedMedicineId();
    const name = this.scanSavedMedicineName();
    this.resetScanState();
    if (!id) return;
    this.reminderMedicineId.set(id);
    this.reminderMedicineName.set(name);
    this.reminderForm = this.emptyReminderForm();
    this.showReminderModal.set(true);
  }

  closeReminderModal(): void {
    this.showReminderModal.set(false);
    this.reminderMedicineId.set(null);
    this.reminderMedicineName.set(null);
    this.reminderForm = this.emptyReminderForm();
  }

  saveReminder(): void {
    if (!this.reminderFormValid) return;
    const medicineId = this.reminderMedicineId();
    if (!medicineId) return;
    this.reminderSaving.set(true);
    const startDate = this.reminderForm.startDate;
    const payload: CreateReminderPayload = {
      userMedicineId: medicineId,
      times: this.reminderForm.reminderTimes.filter(t => t.trim()),
      startDate,
      endDate: this.reminderForm.repeatType === 'Once' ? startDate : this.reminderForm.endDate,
      repeatType: this.reminderForm.repeatType,
    };
    this.reminderSvc.createReminder(medicineId, payload).subscribe({
      next: () => {
        this.reminderSaving.set(false);
        this.closeReminderModal();
        this.showToast('Reminder set successfully.', 'success');
        this.router.navigate(['/medications'], { queryParams: { tab: 'medications' } });
      },
      error: err => {
        this.reminderSaving.set(false);
        this.showToast(err?.error?.message || 'Failed to set reminder. Please try again.', 'error');
      },
    });
  }

  saveScanResult(): void {
    if (!this.scanForm.medicineName.trim()) return;
    this.scanSaving.set(true);
    const payload: AddUserMedicinePayload = {
      medicineName: this.scanForm.medicineName.trim(),
      dosage: this.scanForm.dosage.trim() || 'N/A',
      frequency: this.scanForm.frequency.trim() || 'As directed',
      duration: this.scanForm.duration.trim() || 'As needed',
      notes: this.scanForm.notes.trim() || undefined,
      imagePath: this.scanForm.imagePath || undefined,
      source: 3,
    };

    const mode = this.scanMode();
    const request$ = mode === 'edit' && this.scanRecordId()
      ? this.http.put(`${this.base}/user-medicines/${this.scanRecordId()}`, payload)
      : this.healthProfileSvc.getMyProfile().pipe(
        switchMap(res =>
          this.http.post(`${this.base}/user-medicines?profileId=${res.data.id}`, payload)
        )
      );

    request$.subscribe({
      next: (res: any) => {
        this.scanSaving.set(false);
        if (mode === 'edit') {
          this.resetScanState();
          this.showToast('Medicine record updated successfully.', 'success');
          this.loadData();
        } else {
          const savedId = res?.data?.id ?? null;
          const savedName = this.scanForm.medicineName.trim();
          this.scanSavedMedicineId.set(savedId);
          this.scanSavedMedicineName.set(savedName);
          this.loadData();
        }
      },
      error: err => {
        this.scanSaving.set(false);
        this.showToast(err?.error?.message || 'Failed to save medicine.', 'error');
      },
    });
  }

  private emptyScanForm(): ScanForm {
    return {
      medicineName: '',
      dosage: '',
      dosageForm: '',
      manufacturer: '',
      frequency: '',
      duration: '',
      notes: '',
      imagePath: '',
    };
  }

  private emptyReminderForm(): ReminderForm {
    return {
      reminderTimes: [''],
      repeatType: 'Daily',
      startDate: '',
      endDate: '',
      notificationsEnabled: true,
      notes: '',
    };
  }

  addReminderTime(): void {
    this.reminderForm.reminderTimes = [...this.reminderForm.reminderTimes, ''];
  }

  removeReminderTime(index: number): void {
    this.reminderForm.reminderTimes = this.reminderForm.reminderTimes.filter((_, i) => i !== index);
  }

  setRepeatType(type: RepeatOption): void {
    this.reminderForm.repeatType = type;
    if (type === 'Once' && this.reminderForm.startDate) {
      this.reminderForm.endDate = this.reminderForm.startDate;
    }
  }

  onReminderStartDateChange(): void {
    if (this.reminderForm.repeatType === 'Once') {
      this.reminderForm.endDate = this.reminderForm.startDate;
    }
  }

  private emptyGeneralUploadForm(): GeneralUploadForm {
    return {
      description: '',
      file: null,
      fileName: '',
    };
  }

  private setUploading(key: UploadCardKey, uploading: boolean): void {
    this.uploadState.update(s => ({
      ...s,
      [key]: { uploading, progress: 0, indeterminate: uploading },
    }));
  }

  getUploadState(key: UploadCardKey): UploadState {
    return this.uploadState()[key];
  }

  private showToast(message: string, type: 'success' | 'error'): void {
    const id = ++this.toastCounter;
    this.toasts.update(t => [...t, { id, message, type }]);
    setTimeout(() => this.removeToast(id), 4500);
  }

  removeToast(id: number): void {
    this.toasts.update(t => t.filter(x => x.id !== id));
  }

  progressOffset(pct: number): number {
    const c = 2 * Math.PI * 14;
    return c - (pct / 100) * c;
  }

  readonly RING_CIRCUMFERENCE = 2 * Math.PI * 14;
}
