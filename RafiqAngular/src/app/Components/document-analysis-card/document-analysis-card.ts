import {
  Component, inject, computed, signal,
  ChangeDetectionStrategy, HostListener, ElementRef, effect,
} from '@angular/core';
import { CommonModule } from '@angular/common';
import { Router } from '@angular/router';
import { HttpClient } from '@angular/common/http';
import { DocumentAnalysisStateService, TrackedDocument } from '../../Services/document-analysis-state.service';
import { LocalizationService } from '../../Services/localization.service';
import { NotificationService } from '../../Services/notification.service';
import { environment } from '../../Environments/Environment';

const POS_KEY = 'rafiq_dac_pos';

interface DragPos { x: number; y: number; }

function clampPos(x: number, y: number): DragPos {
  const panelW = 340, panelH = 400;
  const vw = window.innerWidth, vh = window.innerHeight;
  return {
    x: Math.max(-(vw - panelW - 20), Math.min(0, x)),
    y: Math.max(-(vh - panelH - 20), Math.min(vh - 108 - 20, y)),
  };
}

function loadPos(): DragPos {
  try {
    const raw = localStorage.getItem(POS_KEY);
    if (raw) return JSON.parse(raw);
  } catch {}
  return { x: 0, y: 0 };
}

@Component({
  selector: 'app-document-analysis-card',
  standalone: true,
  imports: [CommonModule],
  changeDetection: ChangeDetectionStrategy.OnPush,
  templateUrl: './document-analysis-card.html',
  styleUrl: './document-analysis-card.css',
})
export class DocumentAnalysisCardComponent {
  protected readonly state  = inject(DocumentAnalysisStateService);
  protected readonly l10n   = inject(LocalizationService);
  protected readonly router = inject(Router);
  protected readonly http   = inject(HttpClient);
  protected readonly notif  = inject(NotificationService);
  private   readonly el     = inject(ElementRef);

  protected readonly minimized   = signal(false);
  protected readonly documents   = this.state.trackedDocuments;
  protected readonly hasAny      = computed(() => this.documents().length > 0);
  protected readonly activeCount = computed(() =>
    this.documents().filter(d => d.status === 'Pending' || d.status === 'Processing').length
  );
  protected readonly failedCount = computed(() =>
    this.documents().filter(d => d.status === 'Failed').length
  );
  protected readonly retrying    = signal<Set<string>>(new Set());

  // ── Drag state ────────────────────────────────────────────────────────────
  protected readonly dragPos   = signal<DragPos>(loadPos());
  private isDragging  = false;
  private dragStartX  = 0;
  private dragStartY  = 0;
  private posAtStart: DragPos = { x: 0, y: 0 };

  constructor() {}

  // ── Panel/chip positioning ────────────────────────────────────────────────

  protected get panelStyle(): string {
    const { x, y } = this.dragPos();
    return `transform: translate(${x}px, ${y}px)`;
  }

  // ── Drag handlers ─────────────────────────────────────────────────────────

  onHeaderPointerDown(e: PointerEvent): void {
    if ((e.target as HTMLElement).closest('button') !== null) return; // don't hijack minimize btn
    this.isDragging   = true;
    this.dragStartX   = e.clientX;
    this.dragStartY   = e.clientY;
    this.posAtStart   = { ...this.dragPos() };
    (e.currentTarget as HTMLElement).setPointerCapture(e.pointerId);
    e.preventDefault();
  }

  @HostListener('document:pointermove', ['$event'])
  onPointerMove(e: PointerEvent): void {
    if (!this.isDragging) return;
    const dx = e.clientX - this.dragStartX;
    const dy = e.clientY - this.dragStartY;
    this.dragPos.set(clampPos(this.posAtStart.x + dx, this.posAtStart.y + dy));
  }

  @HostListener('document:pointerup')
  onPointerUp(): void {
    if (!this.isDragging) return;
    this.isDragging = false;
    try { localStorage.setItem(POS_KEY, JSON.stringify(this.dragPos())); } catch {}
  }

  // ── Panel/chip toggle ──────────────────────────────────────────────────────

  toggle(): void { this.minimized.update(v => !v); }

  // ── Actions ───────────────────────────────────────────────────────────────

  dismiss(doc: TrackedDocument): void {
    this.state.dismiss(doc.documentId);
  }

  cancel(doc: TrackedDocument): void {
    this.state.cancelAnalysis(doc.documentId);
  }

  requestReview(doc: TrackedDocument): void {
    this.state.requestReview(doc);
    const target = doc.profileId
      ? `/medical-records?profileId=${doc.profileId}`
      : '/medical-records';
    if (!this.router.url.startsWith('/medical-records')) {
      void this.router.navigateByUrl(target);
    } else if (doc.profileId) {
      // Already on medical-records, but may be on wrong profile — re-navigate with profileId
      void this.router.navigateByUrl(target);
    }
  }

  viewDocument(doc: TrackedDocument): void {
    this.state.dismiss(doc.documentId);
    void this.router.navigate(['/medical-records']);
  }

  retry(doc: TrackedDocument): void {
    this.state.retryAnalysis(doc);
  }

  continueManually(doc: TrackedDocument): void {
    this.state.requestManualEntry(doc);
    const target = doc.profileId
      ? `/medical-records?profileId=${doc.profileId}`
      : '/medical-records';
    if (!this.router.url.startsWith('/medical-records')) {
      void this.router.navigateByUrl(target);
    } else if (doc.profileId) {
      void this.router.navigateByUrl(target);
    }
  }

  typeIcon(type: string): string {
    return ({ lab: 'fa-flask', imaging: 'fa-x-ray', prescription: 'fa-prescription-bottle-medical', medicine: 'fa-pills', general: 'fa-file-medical' })[type] ?? 'fa-file-medical';
  }

  statusLabel(doc: TrackedDocument): string {
    const da = this.l10n.t().documentAnalysis;
    if (doc.status === 'Processing')    return da.statusProcessing;
    if (doc.status === 'Completed')     return da.statusCompleted;
    if (doc.status === 'ReadyToReview') return da.statusReadyToReview ?? 'Ready to review';
    if (doc.status === 'Pending')       return da.statusPending;
    if (doc.status === 'Failed') {
      return this.state.sanitizeErrorMessage(doc.failureReason);
    }
    return da.statusPending;
  }

  displayTitle(doc: TrackedDocument): string {
    const da = this.l10n.t().documentAnalysis;
    if (doc.status === 'Failed') {
      const titles: Record<string, string> = {
        lab: da.failedLabTitle || 'Lab Report Analysis Failed',
        imaging: da.failedImagingTitle || 'Imaging Report Analysis Failed',
        prescription: da.failedPrescriptionTitle || 'Prescription Analysis Failed',
        medicine: da.failedMedicineTitle || 'Medicine Scan Failed',
        general: da.failedGeneralTitle || 'Document Analysis Failed',
      };
      return titles[doc.uploadType] ?? (da.failedGeneralTitle || 'Document Analysis Failed');
    }

    if (doc.uploadType === 'medicine' && doc.reviewData?.medicineName) {
      return doc.reviewData.medicineName;
    }

    if (doc.status === 'Pending' || doc.status === 'Processing') {
      const recordsT = this.l10n.t().records;
      const processingTitles: Record<string, string> = {
        lab: recordsT?.analysingLab || da.statusProcessing,
        imaging: recordsT?.analysingImaging || da.statusProcessing,
        prescription: recordsT?.extractingPrescription || da.statusProcessing,
        medicine: recordsT?.scanningMedicineBox || da.statusProcessing,
        general: doc.title || da.statusProcessing,
      };
      return processingTitles[doc.uploadType] ?? (doc.title || da.statusProcessing);
    }

    return doc.title || da.statusCompleted;
  }

  protected trackByDocId(_: number, doc: TrackedDocument): string {
    return doc.documentId;
  }
}
