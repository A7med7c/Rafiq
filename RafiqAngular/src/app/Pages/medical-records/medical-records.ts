import {
  Component, inject, OnInit, OnDestroy, signal, computed,
  ElementRef, ViewChild, HostListener,
} from '@angular/core';
import { CommonModule } from '@angular/common';
import { FormsModule } from '@angular/forms';
import { RouterLink } from '@angular/router';
import { HttpClient } from '@angular/common/http';
import { AuthService } from '../../Services/auth-service';
import { MedicalRecordsService, UnifiedMedicalRecord } from '../../Services/medical-records.service';
import { ScanMedicineBoxResponse, AddUserMedicinePayload } from '../../Modles/dashboard.models';
import { environment } from '../../Environments/Environment';
import { PdfService } from '../../Services/pdf.service';

export type UploadCardKey = 'lab' | 'prescription' | 'imaging' | 'medicine';

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
  type: 'lab' | 'imaging' | 'prescription';
  mode: 'create' | 'edit';
  recordId?: string;
  imagePath: string;
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

export interface Toast {
  id: number;
  message: string;
  type: 'success' | 'error';
}

const PAGE_SIZE = 6;

@Component({
  selector: 'app-medical-records',
  standalone: true,
  imports: [CommonModule, FormsModule, RouterLink],
  templateUrl: './medical-records.html',
  styleUrl: './medical-records.css',
})
export class MedicalRecords implements OnInit, OnDestroy {
  private readonly authService = inject(AuthService);
  private readonly recordsService = inject(MedicalRecordsService);
  private readonly http = inject(HttpClient);
  private readonly base = environment.apiUrl;

  @ViewChild('labInput') labInput!: ElementRef<HTMLInputElement>;
  @ViewChild('prescriptionInput') prescriptionInput!: ElementRef<HTMLInputElement>;
  @ViewChild('imagingInput') imagingInput!: ElementRef<HTMLInputElement>;
  @ViewChild('medicineInput') medicineInput!: ElementRef<HTMLInputElement>;

  readonly allRecords = signal<UnifiedMedicalRecord[]>([]);
  readonly loading = signal(true);
  readonly searchQuery = signal('');
  readonly activeTab = signal<'all' | 'lab' | 'prescription' | 'imaging' | 'medicine'>('all');
  readonly sidebarCollapsed = signal(false);
  readonly selectedRecord = signal<UnifiedMedicalRecord | null>(null);
  readonly dropdownOpen = signal(false);
  readonly actionMenuOpen = signal<string | null>(null);
  readonly deleteTarget = signal<UnifiedMedicalRecord | null>(null);
  readonly deleting = signal(false);
  readonly tabDirection = signal<'left' | 'right'>('left');
  readonly tabAnimating = signal(false);

  readonly lightboxUrl = signal<string | null>(null);
  readonly detailImageFailed = signal(false);
  readonly reviewImageFailed = signal(false);
  readonly scanImageFailed = signal(false);

  readonly labCount = signal(0);
  readonly prescriptionCount = signal(0);
  readonly imagingCount = signal(0);
  readonly medicineCount = signal(0);

  readonly uploadState = signal<Record<UploadCardKey, UploadState>>({
    lab: defaultUploadState(),
    prescription: defaultUploadState(),
    imaging: defaultUploadState(),
    medicine: defaultUploadState(),
  });

  readonly uploadLoading = signal(false);
  readonly uploadLoadingLabel = signal('');
  readonly reviewForm = signal<ReviewForm | null>(null);
  readonly reviewSaving = signal(false);

  readonly scanLoading = signal(false);
  readonly scanResult = signal<ScanMedicineBoxResponse | null>(null);
  readonly scanSaving = signal(false);
  readonly scanMode = signal<'create' | 'edit'>('create');
  readonly scanRecordId = signal<string | null>(null);
  scanForm: ScanForm = this.emptyScanForm();

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

  get userEmail(): string {
    return this.authService.currentUser?.email ?? '';
  }

  readonly filteredRecords = computed(() => {
    let list = this.allRecords();
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
    return list;
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
    this.applyResponsiveSidebar();
    this.loadData();
  }
  ngOnDestroy(): void {}

  @HostListener('document:keydown.escape')
  onEsc(): void {
    if (this.lightboxUrl()) { this.lightboxUrl.set(null); return; }
    if (this.deleteTarget() && !this.deleting()) { this.closeDeleteModal(); return; }
    if (this.scanResult()) { this.cancelScanReview(); return; }
    if (this.reviewForm() && !this.reviewSaving()) { this.cancelReview(); return; }
    if (this.selectedRecord()) { this.closeDetails(); return; }
    this.actionMenuOpen.set(null);
    this.dropdownOpen.set(false);
  }

  @HostListener('document:click', ['$event'])
  onDocumentClick(event: MouseEvent): void {
    if (!(event.target as HTMLElement).closest('.hdr-user')) {
      this.dropdownOpen.set(false);
    }
    if (!(event.target as HTMLElement).closest('.record-actions')) {
      this.actionMenuOpen.set(null);
    }
  }

  @HostListener('window:resize')
  onWindowResize(): void {
    this.applyResponsiveSidebar();
  }

  private applyResponsiveSidebar(): void {
    if (window.innerWidth <= 768) this.sidebarCollapsed.set(true);
  }

  toggleSidebar(): void {
    this.sidebarCollapsed.update(v => !v);
  }

  toggleDropdown(): void { this.dropdownOpen.update(v => !v); }
  logout(): void { this.dropdownOpen.set(false); this.authService.logout().subscribe(); }

  loadData(): void {
    this.loading.set(true);
    this.recordsService.getAllData().subscribe({
      next: res => {
        this.labCount.set(res.labs.length);
        this.prescriptionCount.set(res.prescriptions.length);
        this.imagingCount.set(res.imaging.length);
        this.medicineCount.set(res.medicines.length);
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

  goToPage(p: number | '...'): void {
    if (p === '...') return;
    this.currentPage.set(Math.max(1, Math.min(p, this.totalPages())));
  }

  prevPage(): void {
    if (this.currentPage() > 1) this.currentPage.update(p => p - 1);
  }

  nextPage(): void {
    if (this.currentPage() < this.totalPages()) this.currentPage.update(p => p + 1);
  }

  setTab(tab: 'all' | 'lab' | 'prescription' | 'imaging' | 'medicine'): void {
    if (tab === this.activeTab()) return;
    const order: Array<'all' | 'lab' | 'prescription' | 'imaging' | 'medicine'> = ['all', 'lab', 'prescription', 'imaging', 'medicine'];
    const currentIndex = order.indexOf(this.activeTab());
    const nextIndex = order.indexOf(tab);
    this.tabDirection.set(nextIndex >= currentIndex ? 'left' : 'right');
    this.activeTab.set(tab);
    this.currentPage.set(1);
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
    };
    return m[type] ?? 'fa-file-medical';
  }

  getRecordIconClass(type: string): string {
    const m: Record<string, string> = {
      lab: 'rec-ico-blue',
      imaging: 'rec-ico-teal',
      prescription: 'rec-ico-purple',
      medicine: 'rec-ico-orange',
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

  private extractFile(event: Event): File | null {
    const input = event.target as HTMLInputElement;
    const file = input.files?.[0] ?? null;
    input.value = '';
    return file;
  }

  private uploadAndReview(type: 'lab' | 'imaging' | 'prescription', file: File): void {
    const urls: Record<'lab' | 'imaging' | 'prescription', string> = {
      lab: `${this.base}/documents/upload/lab`,
      imaging: `${this.base}/documents/upload/imaging`,
      prescription: `${this.base}/prescriptions/upload`,
    };
    const labels: Record<'lab' | 'imaging' | 'prescription', string> = {
      lab: 'Analysing Lab Report...',
      imaging: 'Analysing Imaging Report...',
      prescription: 'Extracting Prescription...',
    };

    this.uploadLoading.set(true);
    this.uploadLoadingLabel.set(labels[type]);
    this.setUploading(type, true);

    const form = new FormData();
    form.append('image', file);

    this.http.post<{ data: any }>(urls[type], form).subscribe({
      next: res => {
        this.uploadLoading.set(false);
        this.setUploading(type, false);
        const data = res?.data ?? (res as any);
        this.openReviewModal(type, data);
      },
      error: err => {
        this.uploadLoading.set(false);
        this.setUploading(type, false);
        this.showToast(err?.error?.message || 'Upload failed. Please try again.', 'error');
      },
    });
  }

  private openReviewModal(
    type: 'lab' | 'imaging' | 'prescription',
    data: any,
    mode: 'create' | 'edit' = 'create',
    recordId?: string
  ): void {
    const form: ReviewForm = {
      type,
      mode,
      recordId,
      imagePath: data.imageUrl ?? data.imagePath ?? '',
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
    } else {
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
        this.showToast(err?.error?.message || 'Scan failed. Please try again.', 'error');
      },
    });
  }

  cancelScanReview(): void {
    this.scanResult.set(null);
    this.scanMode.set('create');
    this.scanRecordId.set(null);
    this.scanForm = this.emptyScanForm();
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
      : this.http.post(`${this.base}/user-medicines`, payload);

    request$.subscribe({
      next: () => {
        this.scanSaving.set(false);
        this.scanResult.set(null);
        this.scanMode.set('create');
        this.scanRecordId.set(null);
        this.scanForm = this.emptyScanForm();
        this.showToast(mode === 'edit' ? 'Medicine record updated successfully.' : 'Medicine saved to your records.', 'success');
        this.loadData();
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
