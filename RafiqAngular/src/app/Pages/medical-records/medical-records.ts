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
  readonly sidebarCollapsed = signal(true);
  readonly selectedRecord = signal<UnifiedMedicalRecord | null>(null);
  readonly dropdownOpen = signal(false);

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

  ngOnInit(): void { this.loadData(); }
  ngOnDestroy(): void {}

  @HostListener('document:keydown.escape')
  onEsc(): void {
    if (this.lightboxUrl()) { this.lightboxUrl.set(null); return; }
    if (this.scanResult()) { this.cancelScanReview(); return; }
    if (this.reviewForm()) { this.cancelReview(); return; }
    if (this.selectedRecord()) { this.closeDetails(); return; }
    this.dropdownOpen.set(false);
  }

  @HostListener('document:click', ['$event'])
  onDocumentClick(event: MouseEvent): void {
    if (!(event.target as HTMLElement).closest('.hdr-user')) {
      this.dropdownOpen.set(false);
    }
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
    this.activeTab.set(tab);
    this.currentPage.set(1);
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

  private openReviewModal(type: 'lab' | 'imaging' | 'prescription', data: any): void {
    const form: ReviewForm = {
      type,
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
      request$ = this.http.post(`${this.base}/documents/labs`, {
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
      });
    } else if (rf.type === 'imaging') {
      request$ = this.http.post(`${this.base}/documents/imaging`, {
        imagingType: rf.imagingType,
        bodyPart: rf.bodyPart,
        findings: rf.findings,
        impression: rf.impression,
        doctorName: rf.doctorName,
        reportDate: rf.reportDate,
        summary: rf.summary,
        ocrText: rf.ocrText,
        imageUrl: rf.imagePath,
      });
    } else {
      request$ = this.http.post(`${this.base}/prescriptions`, {
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
      });
    }

    request$.subscribe({
      next: () => {
        this.reviewSaving.set(false);
        this.reviewForm.set(null);
        this.showToast('Record confirmed and saved.', 'success');
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

    this.http.post(`${this.base}/user-medicines`, payload).subscribe({
      next: () => {
        this.scanSaving.set(false);
        this.scanResult.set(null);
        this.scanForm = this.emptyScanForm();
        this.showToast('Medicine saved to your records.', 'success');
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
