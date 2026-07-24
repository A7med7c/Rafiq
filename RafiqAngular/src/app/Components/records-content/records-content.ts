import {
  Component, Input, OnInit, OnChanges, OnDestroy, SimpleChanges,
  inject, signal, computed, effect,
  ElementRef, ViewChild, HostListener, ViewEncapsulation,
} from '@angular/core';
import { DOCUMENT } from '@angular/common';
import { CommonModule } from '@angular/common';
import { FormsModule } from '@angular/forms';
import { Router } from '@angular/router';
import { HttpClient } from '@angular/common/http';
import { MedicalRecordsService, UnifiedMedicalRecord } from '../../Services/medical-records.service';
import { ScanMedicineBoxResponse, AddUserMedicinePayload } from '../../Modles/dashboard.models';
import { environment } from '../../Environments/Environment';
import { PdfService } from '../../Services/pdf.service';
import { HealthProfileService } from '../../Services/health-profile.service';
import { of, forkJoin, Subscription } from 'rxjs';
import { LocalizationService } from '../../Services/localization.service';
import { switchMap, map } from 'rxjs';

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

const PAGE_SIZE = 2;
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
  selector: 'app-records-content',
  standalone: true,
  imports: [CommonModule, FormsModule],
  templateUrl: './records-content.html',
  styleUrl: '../../Pages/medical-records/medical-records.css',
  encapsulation: ViewEncapsulation.None,
})
export class RecordsContentComponent implements OnInit, OnChanges, OnDestroy {
  @Input() profileId: string | undefined;
  /** When true, hides the upload cards grid (used in family profiles) */
  @Input() compact = false;
  @Input() readOnly = false;

  @ViewChild('labInput') labInput!: ElementRef<HTMLInputElement>;
  @ViewChild('prescriptionInput') prescriptionInput!: ElementRef<HTMLInputElement>;
  @ViewChild('imagingInput') imagingInput!: ElementRef<HTMLInputElement>;
  @ViewChild('medicineInput') medicineInput!: ElementRef<HTMLInputElement>;
  @ViewChild('generalInput') generalInput!: ElementRef<HTMLInputElement>;
  @ViewChild('manualImageInput') manualImageInput!: ElementRef<HTMLInputElement>;
  @ViewChild('manualMedImageInput') manualMedImageInput!: ElementRef<HTMLInputElement>;

  protected readonly l10n = inject(LocalizationService);
  protected readonly t = this.l10n.t;

  private readonly recordsService = inject(MedicalRecordsService);
  private readonly healthProfileSvc = inject(HealthProfileService);
  private readonly pdfService = inject(PdfService);
  private readonly http = inject(HttpClient);
  private readonly router = inject(Router);
  private readonly base = environment.apiUrl;

  readonly allRecords = signal<UnifiedMedicalRecord[]>([]);
  readonly loading = signal(true);
  readonly searchQuery = signal('');
  readonly activeTab = signal<RecordTab>('all');
  readonly selectedRecord = signal<UnifiedMedicalRecord | null>(null);
  readonly dropdownOpen = signal(false);
  readonly addRecordMenuOpen = signal(false);
  readonly filterMenuOpen = signal(false);
  readonly actionMenuOpen = signal<string | null>(null);
  readonly deleteTarget = signal<UnifiedMedicalRecord | null>(null);
  readonly deleting = signal(false);
  readonly tabDirection = signal<'left' | 'right'>('left');
  readonly tabAnimating = signal(false);
  readonly mobileTabMenuOpen = signal(false);

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
  private _uploadSub: Subscription | null = null;
  private _pendingUploadType: 'lab' | 'imaging' | 'prescription' | 'general' | null = null;
  readonly reviewForm = signal<ReviewForm | null>(null);
  readonly reviewSaving = signal(false);
  readonly generalUploadFormOpen = signal(false);
  generalUploadForm: GeneralUploadForm = this.emptyGeneralUploadForm();

  readonly scanLoading = signal(false);
  readonly scanResult = signal<ScanMedicineBoxResponse | null>(null);
  readonly scanSaving = signal(false);
  readonly scanMode = signal<'create' | 'edit'>('create');
  readonly scanRecordId = signal<string | null>(null);
  scanForm: ScanForm = this.emptyScanForm();

  // Manual entry & AI failure recovery
  readonly showAiFailDialog = signal(false);
  readonly aiFailIsUnreadable = signal(false);
  private _failedFile: File | null = null;
  private _failedType: 'lab' | 'imaging' | 'prescription' | 'general' | 'medicine' | null = null;
  private _failedDesc = '';

  // Task 1: per-medicine add-to-medications state
  readonly addingMedIndex = signal<number | null>(null);
  readonly addedMedIndices = signal<Set<number>>(new Set());
  readonly addingAllMeds = signal(false);

  // Manual entry mode tracking
  readonly manualReviewMode = signal(false);
  readonly manualMedicineMode = signal(false);
  readonly scanSource = signal<1 | 3>(3);

  readonly reviewDateError = signal<string | null>(null);

  readonly todayStr = (() => {
    const d = new Date();
    return `${d.getFullYear()}-${String(d.getMonth() + 1).padStart(2, '0')}-${String(d.getDate()).padStart(2, '0')}`;
  })();

  // Task 2: change-detection snapshots
  private _reviewFormSnapshot: string | null = null;
  private _scanFormSnapshot: string | null = null;

  /** Post-success "set a reminder now?" prompt shown after a medicine is saved. */
  readonly showReminderPromptModal = signal(false);
  readonly reminderPromptMode = signal<'single' | 'multi'>('single');
  readonly reminderPromptMedicineId = signal<string | null>(null);

  readonly currentPage = signal(1);
  readonly pageSize = PAGE_SIZE;
  readonly toasts = signal<Toast[]>([]);
  private toastCounter = 0;

  // Manual entry mode (lab / imaging / prescription / general)
  readonly isManualMode = signal(false);
  readonly manualImageFile = signal<File | null>(null);
  readonly manualImagePreviewUrl = signal<string | null>(null);
  readonly manualImageUploading = signal(false);

  // Manual entry mode — medicine box
  readonly isMedicineManualMode = signal(false);
  readonly manualMedicineImageFile = signal<File | null>(null);
  readonly manualMedicineImagePreviewUrl = signal<string | null>(null);
  readonly manualMedicineImageUploading = signal(false);

  private readonly _doc = inject(DOCUMENT);

  constructor() {
    effect(() => {
      const open = !!(
        this.selectedRecord() || this.deleteTarget() || this.reviewForm() ||
        this.generalUploadFormOpen() || this.uploadLoading() || this.scanLoading() ||
        this.scanResult() || this.showReminderPromptModal() || this.lightboxUrl() ||
        this.showAiFailDialog()
      );
      const container = this._doc.querySelector('.dsh-body') as HTMLElement | null;
      if (container) {
        container.style.overflowY = open ? 'hidden' : '';
      }
      const sidebar = this._doc.querySelector('.dsh-sb') as HTMLElement | null;
      if (sidebar) {
        sidebar.classList.toggle('sb--modal-blur', open);
      }
    });
  }

  ngOnDestroy(): void {
    const container = this._doc.querySelector('.dsh-body') as HTMLElement | null;
    if (container) container.style.overflowY = '';
    const sidebar = this._doc.querySelector('.dsh-sb') as HTMLElement | null;
    if (sidebar) sidebar.classList.remove('sb--modal-blur');
    this.clearManualImage('review');
    this.clearManualImage('medicine');
  }

  private _searchQuery = '';
  get searchQueryValue(): string { return this._searchQuery; }
  set searchQueryValue(v: string) {
    this._searchQuery = v;
    this.searchQuery.set(v);
    this.currentPage.set(1);
  }

  readonly filteredRecords = computed(() => {
    let list = this.applySmartFilters(this.allRecords(), this.appliedFilters());
    const q = this.searchQuery().trim().toLowerCase();
    const tab = this.activeTab();
    if (tab !== 'all') list = list.filter(r => r.type === tab);
    if (q) {
      list = list.filter(r =>
        r.name.toLowerCase().includes(q) ||
        r.typeLabel.toLowerCase().includes(q) ||
        r.uploadedBy.toLowerCase().includes(q) ||
        (r.summary && r.summary.toLowerCase().includes(q))
      );
    }
    return this.sortRecords(list, this.appliedFilters().sortBy);
  });

  readonly countedRecords = computed(() =>
    this.applySmartFilters(this.allRecords(), this.appliedFilters())
  );

  readonly labCount = computed(() => this.countedRecords().filter(r => r.type === 'lab').length);
  readonly prescriptionCount = computed(() => this.countedRecords().filter(r => r.type === 'prescription').length);
  readonly imagingCount = computed(() => this.countedRecords().filter(r => r.type === 'imaging').length);
  readonly medicineCount = computed(() => this.countedRecords().filter(r => r.type === 'medicine').length);
  readonly generalCount = computed(() => this.countedRecords().filter(r => r.type === 'general').length);

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

  ngOnInit(): void { this.loadData(); }

  ngOnChanges(changes: SimpleChanges): void {
    if (changes['profileId'] && !changes['profileId'].firstChange) {
      this.allRecords.set([]);
      this.currentPage.set(1);
      this.loadData();
    }
  }

  @HostListener('document:keydown.escape')
  onEsc(): void {
    if (this.lightboxUrl()) { this.lightboxUrl.set(null); return; }
    if (this.showReminderPromptModal()) { this.closeReminderPrompt(); return; }
    if (this.deleteTarget() && !this.deleting()) { this.closeDeleteModal(); return; }
    if (this.scanResult()) { this.cancelScanReview(); return; }
    if (this.reviewForm() && !this.reviewSaving()) { this.cancelReview(); return; }
    if (this.generalUploadFormOpen() && !this.uploadLoading()) { this.cancelGeneralUpload(); return; }
    if (this.selectedRecord()) { this.closeDetails(); return; }
    this.addRecordMenuOpen.set(false);
    this.filterMenuOpen.set(false);
    this.actionMenuOpen.set(null);
  }

  @HostListener('document:click', ['$event'])
  onDocumentClick(event: MouseEvent): void {
    if (!(event.target as HTMLElement).closest('.record-actions')) {
      this.actionMenuOpen.set(null);
    }
    if (!(event.target as HTMLElement).closest('.add-record-menu-wrap')) {
      this.addRecordMenuOpen.set(false);
    }
    if (!(event.target as HTMLElement).closest('.filter-menu-wrap')) {
      this.filterMenuOpen.set(false);
    }
  }

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
    this.recordsService.getAllData(this.profileId).subscribe({
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
    return [...records].sort((a, b) => {
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
    try { localStorage.setItem(FILTER_SORT_STORAGE_KEY, sortBy); } catch {}
  }

  typeLabel(type: RecordTab): string {
    const labels: Record<RecordTab, string> = {
      all: 'All', lab: 'Lab Analysis', prescription: 'Prescription',
      imaging: 'X-Ray & Imaging', medicine: 'Medicine Box', general: 'Other Medical Document',
    };
    return labels[type];
  }

  uploadedByLabel(uploadedBy: UploadedByFilter): string {
    const labels: Record<UploadedByFilter, string> = {
      all: 'All', self: 'Self', manual: 'Manual', medicine: 'Medicine Box',
    };
    return labels[uploadedBy];
  }

  sortLabel(sortBy: SortOption): string {
    const labels: Record<SortOption, string> = {
      newest: 'Newest First', oldest: 'Oldest First', az: 'A-Z', za: 'Z-A',
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
    if (this.currentPage() > 1) { this.animateTable('right'); this.currentPage.update(p => p - 1); }
  }

  nextPage(): void {
    if (this.currentPage() < this.totalPages()) { this.animateTable('left'); this.currentPage.update(p => p + 1); }
  }

  isLastRow(index: number): boolean {
    return index >= this.pagedRecords().length - 2;
  }

  setTab(tab: RecordTab): void {
    if (tab === this.activeTab()) return;
    const order: RecordTab[] = ['all', 'lab', 'prescription', 'imaging', 'medicine', 'general'];
    const currentIndex = order.indexOf(this.activeTab());
    const nextIndex = order.indexOf(tab);
    this.animateTable(nextIndex >= currentIndex ? 'left' : 'right');
    this.activeTab.set(tab);
    this.currentPage.set(1);
    this.mobileTabMenuOpen.set(false);
  }

  toggleMobileTabMenu(): void { this.mobileTabMenuOpen.update(v => !v); }
  closeMobileTabMenu(): void  { this.mobileTabMenuOpen.set(false); }

  activeTabLabel(): string {
    const t = this.t().records;
    const map: Record<string, string> = {
      all:          t.allRecords,
      lab:          t.labAnalysisTab,
      prescription: t.prescriptionsTab,
      imaging:      t.xraysImagingTab,
      medicine:     t.medicineBoxesTab,
      general:      t.otherDocumentsTab,
    };
    return map[this.activeTab()] ?? t.allRecords;
  }

  activeTabIcon(): string {
    const map: Record<string, string> = {
      all: 'fa-layer-group', lab: 'fa-flask', prescription: 'fa-prescription-bottle-medical',
      imaging: 'fa-x-ray', medicine: 'fa-pills', general: 'fa-file-medical',
    };
    return map[this.activeTab()] ?? 'fa-layer-group';
  }


  private animateTable(direction: 'left' | 'right'): void {
    this.tabDirection.set(direction);
    this.tabAnimating.set(false);
    setTimeout(() => this.tabAnimating.set(true));
    setTimeout(() => this.tabAnimating.set(false), 260);
  }

  getRecordIcon(type: string): string {
    const m: Record<string, string> = {
      lab: 'fa-flask', imaging: 'fa-x-ray', prescription: 'fa-prescription-bottle-medical',
      medicine: 'fa-pills', general: 'fa-file-medical',
    };
    return m[type] ?? 'fa-file-medical';
  }

  getRecordIconClass(type: string): string {
    const m: Record<string, string> = {
      lab: 'rec-ico-blue', imaging: 'rec-ico-teal', prescription: 'rec-ico-purple',
      medicine: 'rec-ico-orange', general: 'rec-ico-gray',
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
    if (this.readOnly) return;
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
    if (this.readOnly) return;
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
      this.isMedicineManualMode.set(false);
      this.clearManualImage('medicine');
      this._scanFormSnapshot = this.snapshotScanForm(this.scanForm);
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

  closeDetails(): void { this.selectedRecord.set(null); }
  downloadRecord(record: UnifiedMedicalRecord): void { this.pdfService.download(record); }

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
    if (this.readOnly) return;
    const map: Record<string, ElementRef<HTMLInputElement> | undefined> = {
      'Lab Analysis': this.labInput,
      'Prescription': this.prescriptionInput,
      'X-Ray & Imaging': this.imagingInput,
      'Medicine Box': this.medicineInput,
      'General Medical Document': this.generalInput,
    };
    (map[type] ?? this.labInput)?.nativeElement.click();
  }

  onLabFileSelected(e: Event): void { const f = this.extractFile(e); if (f) this.uploadAndReview('lab', f); }
  onPrescriptionFileSelected(e: Event): void { const f = this.extractFile(e); if (f) this.uploadAndReview('prescription', f); }
  onImagingFileSelected(e: Event): void { const f = this.extractFile(e); if (f) this.uploadAndReview('imaging', f); }
  onMedicineFileSelected(e: Event): void { const f = this.extractFile(e); if (f) this.startMedicineScan(f); }
  onGeneralFileSelected(e: Event): void {
    const f = this.extractFile(e);
    if (f) this.uploadAndReview('general', f);
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
    if (!file) { this.showToast('Please choose an image before uploading.', 'error'); return; }
    this.uploadAndReview('general', file, this.generalUploadForm.description);
  }

  private extractFile(event: Event): File | null {
    const input = event.target as HTMLInputElement;
    const file = input.files?.[0] ?? null;
    input.value = '';
    return file;
  }

  private uploadAndReview(type: 'lab' | 'imaging' | 'prescription' | 'general', file: File, description = ''): void {
    const pid = this.profileId ? `?profileId=${this.profileId}` : '';
    const urls: Record<'lab' | 'imaging' | 'prescription' | 'general', string> = {
      lab: `${this.base}/documents/upload/lab${pid}`,
      imaging: `${this.base}/documents/upload/imaging${pid}`,
      prescription: `${this.base}/prescriptions/upload${pid}`,
      general: `${this.base}/documents/general/upload`,
    };
    const labels: Record<'lab' | 'imaging' | 'prescription' | 'general', string> = {
      lab: this.t().records.analysingLab,
      imaging: this.t().records.analysingImaging,
      prescription: this.t().records.extractingPrescription,
      general: this.t().records.analysingDocument,
    };

    this.uploadLoading.set(true);
    this.uploadLoadingLabel.set(labels[type]);
    this.setUploading(type, true);
    this._pendingUploadType = type;

    const form = new FormData();
    form.append('image', file);
    if (type === 'general') form.append('description', description.trim());

    this._uploadSub = this.http.post<{ data: any }>(urls[type], form).subscribe({
      next: res => {
        this._uploadSub = null;
        this._pendingUploadType = null;
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
        this._uploadSub = null;
        this._pendingUploadType = null;
        this.uploadLoading.set(false);
        this.setUploading(type, false);
        const errCode = err?.error?.errorCode as string | undefined;
        const v = this.t().uploadValidation;
        if (errCode === 'WRONG_DOCUMENT_TYPE_LAB_REPORT') {
          this.showToast(v.lab, 'error');
        } else if (errCode === 'WRONG_DOCUMENT_TYPE_IMAGING_REPORT') {
          this.showToast(v.imaging, 'error');
        } else if (errCode === 'WRONG_DOCUMENT_TYPE_PRESCRIPTION') {
          this.showToast(v.prescription, 'error');
        } else if (errCode === 'SHOULD_BE_LAB_REPORT') {
          this.showToast(v.generalShouldBeLab, 'error');
        } else if (errCode === 'SHOULD_BE_IMAGING_REPORT') {
          this.showToast(v.generalShouldBeImaging, 'error');
        } else if (errCode === 'SHOULD_BE_PRESCRIPTION') {
          this.showToast(v.generalShouldBePrescription, 'error');
        } else if (errCode === 'SHOULD_BE_MEDICINE_BOX') {
          this.showToast(v.generalShouldBeMedicine, 'error');
        } else if (errCode === 'NOT_MEDICAL_DOCUMENT') {
          this.showToast(v.generalNotMedical, 'error');
        } else if (errCode === 'UNREADABLE_DOCUMENT_LAB_REPORT' || errCode === 'UNREADABLE_DOCUMENT_IMAGING_REPORT' || errCode === 'UNREADABLE_DOCUMENT_PRESCRIPTION') {
          this._failedFile = file;
          this._failedType = type;
          this._failedDesc = description;
          this.aiFailIsUnreadable.set(true);
          this.showAiFailDialog.set(true);
        } else {
          this._failedFile = file;
          this._failedType = type;
          this._failedDesc = description;
          this.aiFailIsUnreadable.set(false);
          this.showAiFailDialog.set(true);
        }
      },
    });
  }

  cancelUpload(): void {
    this._uploadSub?.unsubscribe();
    this._uploadSub = null;
    if (this._pendingUploadType) {
      this.setUploading(this._pendingUploadType, false);
      this._pendingUploadType = null;
    }
    this.uploadLoading.set(false);
    this.uploadLoadingLabel.set('');
  }

  private openReviewModal(
    type: 'lab' | 'imaging' | 'prescription' | 'general',
    data: any,
    mode: 'create' | 'edit' = 'create',
    recordId?: string
  ): void {
    const form: ReviewForm = {
      type, mode, recordId,
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
    this._reviewFormSnapshot = this.snapshotReviewForm(form);
  }

  addLabResult(): void {
    const rf = this.reviewForm();
    if (!rf) return;
    rf.results = [...rf.results, { id: crypto.randomUUID(), testName: '', value: '', unit: '', normalRange: '', status: 'Normal' }];
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
    rf.prescriptionMedicines = [...rf.prescriptionMedicines, { medicineName: '', dosage: '', frequency: '', duration: '', instructions: '' }];
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
    this._reviewFormSnapshot = null;
    this.addedMedIndices.set(new Set());
    this.addingMedIndex.set(null);
    this.addingAllMeds.set(false);
    this.manualReviewMode.set(false);
    this.manualImageUploading.set(false);
    this.reviewDateError.set(null);
  }

  confirmAndSave(): void {
    const rf = this.reviewForm();
    if (!rf) return;

    const dateStr = rf.type === 'prescription' ? rf.prescriptionDate : rf.reportDate;
    if (dateStr && dateStr > this.todayStr) {
      this.reviewDateError.set('Date cannot be later than today.');
      return;
    }
    this.reviewDateError.set(null);

    if (rf.mode === 'edit' && this._reviewFormSnapshot !== null &&
        this.snapshotReviewForm(rf) === this._reviewFormSnapshot) {
      this.showToast('No changes detected. Please edit at least one field before saving.', 'error');
      return;
    }

    this.reviewSaving.set(true);

    const pid = this.profileId ? `?profileId=${this.profileId}` : '';
    let request$;

    if (rf.type === 'lab') {
      const payload = {
        labName: rf.labName, doctorName: rf.doctorName, reportDate: rf.reportDate,
        summary: rf.summary, ocrText: rf.ocrText, imageUrl: rf.imagePath,
        results: rf.results.map(r => ({ testName: r.testName, value: r.value, unit: r.unit, normalRange: r.normalRange, status: r.status })),
      };
      request$ = rf.mode === 'edit' && rf.recordId
        ? this.http.put(`${this.base}/documents/labs/${rf.recordId}`, payload)
        : this.http.post(`${this.base}/documents/labs${pid}`, payload);
    } else if (rf.type === 'imaging') {
      const payload = {
        imagingType: rf.imagingType, bodyPart: rf.bodyPart, findings: rf.findings,
        impression: rf.impression, doctorName: rf.doctorName, reportDate: rf.reportDate,
        summary: rf.summary, ocrText: rf.ocrText, imageUrl: rf.imagePath,
      };
      request$ = rf.mode === 'edit' && rf.recordId
        ? this.http.put(`${this.base}/documents/imaging/${rf.recordId}`, payload)
        : this.http.post(`${this.base}/documents/imaging${pid}`, payload);
    } else if (rf.type === 'prescription') {
      const payload = {
        doctorName: rf.doctorName, patientName: rf.patientName,
        prescriptionDate: rf.prescriptionDate, imagePath: rf.imagePath,
        medicines: rf.prescriptionMedicines.map(m => ({
          medicineName: m.medicineName, dosage: m.dosage, frequency: m.frequency,
          duration: m.duration, instructions: m.instructions,
        })),
      };
      request$ = rf.mode === 'edit' && rf.recordId
        ? this.http.put(`${this.base}/prescriptions/${rf.recordId}`, payload)
        : this.http.post(`${this.base}/prescriptions${pid}`, payload);
    } else {
      const payload = {
        title: rf.title, description: rf.description, aiSummary: rf.summary, imagePath: rf.imagePath,
        documentType: rf.documentType, doctorName: rf.doctorName, hospitalOrClinic: rf.hospitalOrClinic,
        documentDate: rf.documentDate, ocrText: rf.ocrText,
      };
      request$ = rf.mode === 'edit' && rf.recordId
        ? this.http.put(`${this.base}/documents/general/${rf.recordId}`, payload)
        : this.http.post(`${this.base}/documents/general${pid}`, payload);
    }

    request$.subscribe({
      next: () => {
        this.reviewSaving.set(false);
        this.reviewForm.set(null);
        this._reviewFormSnapshot = null;
        this.addedMedIndices.set(new Set());
        this.addingMedIndex.set(null);
        this.addingAllMeds.set(false);
        this.manualReviewMode.set(false);
        this.manualImageUploading.set(false);
        this.reviewDateError.set(null);
        this.showToast(rf.mode === 'edit' ? 'Record updated successfully.' : 'Record saved successfully.', 'success');
        this.loadData();

        if (rf.type === 'prescription' && rf.mode !== 'edit') {
          this.openReminderPrompt('multi');
        }
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
          frequency: '', duration: '', notes: '',
          imagePath: data.imagePath ?? '',
        };
        this.scanImageFailed.set(false);
        this.scanMode.set('create');
        this.scanRecordId.set(null);
        this.scanResult.set(data);
        this._scanFormSnapshot = this.snapshotScanForm(this.scanForm);
      },
      error: err => {
        this.scanLoading.set(false);
        this.setUploading('medicine', false);
        const errCode = err?.error?.errorCode as string | undefined;
        if (errCode === 'WRONG_DOCUMENT_TYPE_MEDICINE_BOX') {
          this.showToast(this.t().uploadValidation.medicine, 'error');
        } else if (errCode === 'UNREADABLE_DOCUMENT_MEDICINE_BOX') {
          this._failedFile = file;
          this._failedType = 'medicine';
          this._failedDesc = '';
          this.aiFailIsUnreadable.set(true);
          this.showAiFailDialog.set(true);
        } else {
          this._failedFile = file;
          this._failedType = 'medicine';
          this._failedDesc = '';
          this.aiFailIsUnreadable.set(false);
          this.showAiFailDialog.set(true);
        }
      },
    });
  }

  cancelScanReview(): void {
    this.scanResult.set(null);
    this.scanMode.set('create');
    this.scanRecordId.set(null);
    this.scanForm = this.emptyScanForm();
    this._scanFormSnapshot = null;
    this.manualMedicineMode.set(false);
    this.scanSource.set(3);
    this.manualImageUploading.set(false);
  }

  onManualImageSelected(e: Event): void {
    const f = this.extractFile(e);
    if (f) this.uploadManualImage(f, 'review');
  }

  onManualMedImageSelected(e: Event): void {
    const f = this.extractFile(e);
    if (f) this.uploadManualImage(f, 'medicine');
  }

  private uploadManualImage(file: File, target: 'review' | 'medicine'): void {
    this.manualImageUploading.set(true);
    const form = new FormData();
    form.append('image', file);
    this.http.post<{ data: { path: string } }>(`${this.base}/documents/upload-image`, form).subscribe({
      next: res => {
        this.manualImageUploading.set(false);
        const path = res?.data?.path ?? '';
        if (target === 'review') {
          const rf = this.reviewForm();
          if (rf) { this.reviewForm.set({ ...rf, imagePath: path }); this.reviewImageFailed.set(false); }
        } else {
          this.scanForm.imagePath = path;
          this.scanImageFailed.set(false);
        }
      },
      error: () => {
        this.manualImageUploading.set(false);
        this.showToast('Failed to upload image. Please try again.', 'error');
      },
    });
  }

  clearManualImage(target: 'review' | 'medicine'): void {
    if (target === 'review') {
      const rf = this.reviewForm();
      if (rf) this.reviewForm.set({ ...rf, imagePath: '' });
    } else {
      this.scanForm.imagePath = '';
    }
  }

  openManualEntry(type: UploadCardKey): void {
    if (this.readOnly) return;
    if (type === 'medicine') {
      this.scanForm = this.emptyScanForm();
      this.scanImageFailed.set(false);
      this.manualMedicineMode.set(true);
      this.scanSource.set(1);
      this.scanMode.set('create');
      this.scanRecordId.set(null);
      this._scanFormSnapshot = null;
      this.scanResult.set({ medicineName: '', strength: '', dosageForm: '', manufacturer: '', imagePath: '' });
      return;
    }
    const typeMap: Record<string, 'lab' | 'imaging' | 'prescription' | 'general'> = {
      lab: 'lab', imaging: 'imaging', prescription: 'prescription', general: 'general',
    };
    const reviewType = typeMap[type];
    if (!reviewType) return;
    const seedData: Record<string, any> = {
      lab: { results: [{ testName: '', value: '', unit: '', normalRange: '', status: 'Normal' }] },
      prescription: { medicines: [{ medicineName: '', dosage: '', frequency: '', duration: '', instructions: '' }] },
      imaging: {},
      general: {},
    };
    this.manualReviewMode.set(true);
    this.openReviewModal(reviewType, seedData[reviewType], 'create');
  }

  saveScanResult(): void {
    if (!this.scanForm.medicineName.trim()) return;
    if (this.manualMedicineImageUploading()) return;

    if (this.scanMode() === 'edit' && this._scanFormSnapshot !== null &&
        this.snapshotScanForm(this.scanForm) === this._scanFormSnapshot) {
      this.showToast('No changes detected. Please edit at least one field before saving.', 'error');
      return;
    }

    this.scanSaving.set(true);
    const payload: AddUserMedicinePayload = {
      medicineName: this.scanForm.medicineName.trim(),
      dosage: this.scanForm.dosage.trim() || 'N/A',
      frequency: this.scanForm.frequency.trim() || 'As directed',
      duration: this.scanForm.duration.trim() || 'As needed',
      notes: this.scanForm.notes.trim() || undefined,
      imagePath: this.scanForm.imagePath || undefined,
      source: this.scanSource(),
    };

    const mode = this.scanMode();

    let request$;
    if (mode === 'edit' && this.scanRecordId()) {
      const pidQ = this.profileId ? `?profileId=${this.profileId}` : '';
      request$ = this.http.put(`${this.base}/user-medicines/${this.scanRecordId()}${pidQ}`, payload);
    } else if (this.profileId) {
      request$ = this.http.post(`${this.base}/user-medicines?profileId=${this.profileId}`, payload);
    } else {
      request$ = this.healthProfileSvc.getMyProfile().pipe(
        switchMap(res => this.http.post(`${this.base}/user-medicines?profileId=${res.data.id}`, payload))
      );
    }

    request$.subscribe({
      next: (res: any) => {
        this.scanSaving.set(false);
        this.scanResult.set(null);
        this.scanMode.set('create');
        this.scanRecordId.set(null);
        this.scanForm = this.emptyScanForm();
        this._scanFormSnapshot = null;
        this.manualMedicineMode.set(false);
        this.scanSource.set(3);
        this.showToast(mode === 'edit' ? 'Medicine record updated successfully.' : 'Medicine saved to your records.', 'success');
        this.loadData();

        const savedMedicineId = res?.data?.id ?? null;
        if (mode !== 'edit' && savedMedicineId) {
          this.openReminderPrompt('single', savedMedicineId);
        }
      },
      error: err => {
        this.scanSaving.set(false);
        this.showToast(err?.error?.message || 'Failed to save medicine.', 'error');
      },
    });
  }

  // ── Post-success reminder prompt ─────────────────────────────────────────
  private openReminderPrompt(mode: 'single' | 'multi', medicineId?: string): void {
    this.reminderPromptMode.set(mode);
    this.reminderPromptMedicineId.set(medicineId ?? null);
    this.showReminderPromptModal.set(true);
  }

  closeReminderPrompt(): void {
    this.showReminderPromptModal.set(false);
    this.reminderPromptMedicineId.set(null);
  }

  goToSetReminder(): void {
    const mode = this.reminderPromptMode();
    const medicineId = this.reminderPromptMedicineId();
    this.showReminderPromptModal.set(false);
    this.reminderPromptMedicineId.set(null);

    const queryParams: Record<string, string> = { tab: 'medications' };
    if (mode === 'single' && medicineId) queryParams['medicineId'] = medicineId;
    this.router.navigate(['/medications'], { queryParams });
  }

  // ── Task 2: snapshot helpers ─────────────────────────────────────────────
  private snapshotReviewForm(rf: ReviewForm): string {
    if (rf.type === 'lab') {
      return JSON.stringify({
        labName: rf.labName, doctorName: rf.doctorName, reportDate: rf.reportDate,
        summary: rf.summary, ocrText: rf.ocrText,
        results: rf.results.map(r => ({ testName: r.testName, value: r.value, unit: r.unit, normalRange: r.normalRange, status: r.status })),
      });
    }
    if (rf.type === 'imaging') {
      return JSON.stringify({
        imagingType: rf.imagingType, bodyPart: rf.bodyPart, findings: rf.findings,
        impression: rf.impression, doctorName: rf.doctorName, reportDate: rf.reportDate,
        summary: rf.summary, ocrText: rf.ocrText,
      });
    }
    if (rf.type === 'prescription') {
      return JSON.stringify({
        doctorName: rf.doctorName, patientName: rf.patientName, prescriptionDate: rf.prescriptionDate,
        prescriptionMedicines: rf.prescriptionMedicines.map(m => ({
          medicineName: m.medicineName, dosage: m.dosage, frequency: m.frequency,
          duration: m.duration, instructions: m.instructions,
        })),
      });
    }
    return JSON.stringify({ title: rf.title, description: rf.description, summary: rf.summary, documentType: rf.documentType, doctorName: rf.doctorName, hospitalOrClinic: rf.hospitalOrClinic, documentDate: rf.documentDate, ocrText: rf.ocrText });
  }

  private snapshotScanForm(sf: ScanForm): string {
    return JSON.stringify({
      medicineName: sf.medicineName, dosage: sf.dosage, dosageForm: sf.dosageForm,
      manufacturer: sf.manufacturer, frequency: sf.frequency, duration: sf.duration, notes: sf.notes,
    });
  }

  // ── Task 1: add prescription medicines to Medications module ─────────────
  addPrescriptionMedToMedications(index: number): void {
    const rf = this.reviewForm();
    if (!rf || rf.type !== 'prescription') return;
    const med = rf.prescriptionMedicines[index];
    if (!med?.medicineName?.trim() || this.addedMedIndices().has(index)) return;

    this.addingMedIndex.set(index);
    const payload: AddUserMedicinePayload = {
      medicineName: med.medicineName.trim(),
      dosage: med.dosage.trim() || 'N/A',
      frequency: med.frequency.trim() || 'As directed',
      duration: med.duration.trim() || 'As needed',
      notes: med.instructions.trim() || undefined,
      source: 2,
    };

    const profileId$ = this.profileId
      ? of(this.profileId)
      : this.healthProfileSvc.getMyProfile().pipe(map((r: any) => r.data.id));

    profileId$.pipe(
      switchMap(pid => this.http.post(`${this.base}/user-medicines?profileId=${pid}`, payload))
    ).subscribe({
      next: () => {
        this.addingMedIndex.set(null);
        this.addedMedIndices.update(s => new Set([...s, index]));
        this.showToast(`${med.medicineName} added to your medications.`, 'success');
      },
      error: err => {
        this.addingMedIndex.set(null);
        this.showToast(err?.error?.message || 'Failed to add medicine.', 'error');
      },
    });
  }

  addAllPrescriptionMedsToMedications(): void {
    const rf = this.reviewForm();
    if (!rf || rf.type !== 'prescription') return;

    const notYetAdded = rf.prescriptionMedicines
      .map((m, i) => ({ m, i }))
      .filter(({ m, i }) => m.medicineName.trim() && !this.addedMedIndices().has(i));

    if (notYetAdded.length === 0) return;

    this.addingAllMeds.set(true);
    const profileId$ = this.profileId
      ? of(this.profileId)
      : this.healthProfileSvc.getMyProfile().pipe(map((r: any) => r.data.id));

    profileId$.pipe(
      switchMap(pid =>
        forkJoin(notYetAdded.map(({ m }) =>
          this.http.post(`${this.base}/user-medicines?profileId=${pid}`, {
            medicineName: m.medicineName.trim(),
            dosage: m.dosage.trim() || 'N/A',
            frequency: m.frequency.trim() || 'As directed',
            duration: m.duration.trim() || 'As needed',
            notes: m.instructions.trim() || undefined,
            source: 2,
          } as AddUserMedicinePayload)
        ))
      )
    ).subscribe({
      next: () => {
        this.addingAllMeds.set(false);
        this.addedMedIndices.update(s => new Set([...s, ...notYetAdded.map(({ i }) => i)]));
        this.showToast('All medicines added to your medications.', 'success');
      },
      error: err => {
        this.addingAllMeds.set(false);
        this.showToast(err?.error?.message || 'Failed to add medicines.', 'error');
      },
    });
  }

  private emptyScanForm(): ScanForm {
    return { medicineName: '', dosage: '', dosageForm: '', manufacturer: '', frequency: '', duration: '', notes: '', imagePath: '' };
  }

  private emptyGeneralUploadForm(): GeneralUploadForm {
    return { description: '', file: null, fileName: '' };
  }

  private setUploading(key: UploadCardKey, uploading: boolean): void {
    this.uploadState.update(s => ({ ...s, [key]: { uploading, progress: 0, indeterminate: uploading } }));
  }

  getUploadState(key: UploadCardKey): UploadState { return this.uploadState()[key]; }

  showToast(message: string, type: 'success' | 'error'): void {
    const id = ++this.toastCounter;
    this.toasts.update(t => [...t, { id, message, type }]);
    setTimeout(() => this.removeToast(id), 4500);
  }

  removeToast(id: number): void { this.toasts.update(t => t.filter(x => x.id !== id)); }

  progressOffset(pct: number): number {
    const c = 2 * Math.PI * 14;
    return c - (pct / 100) * c;
  }

  readonly RING_CIRCUMFERENCE = 2 * Math.PI * 14;

  closeAiFailDialog(): void {
    this.showAiFailDialog.set(false);
    this.aiFailIsUnreadable.set(false);
    this._failedFile = null;
    this._failedType = null;
    this._failedDesc = '';
  }

  continueManually(): void {
    const type = this._failedType;
    this.closeAiFailDialog();
    if (type) this.openManualEntry(type);
  }

  retryUpload(): void {
    const type = this._failedType;
    const file = this._failedFile;
    if (!type || !file) return;
    this.showAiFailDialog.set(false);
    if (type === 'medicine') {
      this.startMedicineScan(file);
    } else {
      this.uploadAndReview(type, file, this._failedDesc);
    }
  }
}
