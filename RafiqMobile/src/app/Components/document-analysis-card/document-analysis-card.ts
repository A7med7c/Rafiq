import {
  Component, inject, computed, signal,
  ChangeDetectionStrategy, HostListener, ElementRef,
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
  protected readonly retrying    = signal<Set<string>>(new Set());

  // ── Drag state ────────────────────────────────────────────────────────────
  protected readonly dragPos   = signal<DragPos>(loadPos());
  private isDragging  = false;
  private dragStartX  = 0;
  private dragStartY  = 0;
  private posAtStart: DragPos = { x: 0, y: 0 };

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

  requestReview(doc: TrackedDocument): void {
    this.state.requestReview(doc);
    if (!this.router.url.startsWith('/medical-records')) {
      void this.router.navigate(['/medical-records']);
    }
  }

  viewDocument(doc: TrackedDocument): void {
    this.state.dismiss(doc.documentId);
    void this.router.navigate(['/medical-records']);
  }

  retry(doc: TrackedDocument): void {
    if (this.retrying().has(doc.documentId)) return;
    this.retrying.update(s => { const n = new Set(s); n.add(doc.documentId); return n; });

    this.http
      .post<any>(`${environment.apiUrl}/documents/general/${doc.documentId}/retry`, {})
      .subscribe({
        next: () => {
          this.state.trackedDocuments.update(docs =>
            docs.map(d => d.documentId === doc.documentId
              ? { ...d, status: 'Pending' as const, failureReason: null }
              : d)
          );
          this.retrying.update(s => { const n = new Set(s); n.delete(doc.documentId); return n; });
        },
        error: () => {
          this.retrying.update(s => { const n = new Set(s); n.delete(doc.documentId); return n; });
                  },
      });
  }

  typeIcon(type: string): string {
    return ({ lab: 'fa-flask', imaging: 'fa-x-ray', prescription: 'fa-prescription-bottle-medical', general: 'fa-file-medical' })[type] ?? 'fa-file-medical';
  }

  statusLabel(doc: TrackedDocument): string {
    const da = this.l10n.t().documentAnalysis;
    if (doc.status === 'Processing')    return da.statusProcessing;
    if (doc.status === 'Completed')     return da.statusCompleted;
    if (doc.status === 'Failed')        return doc.failureReason ?? da.statusFailed;
    if (doc.status === 'ReadyToReview') return da.statusReadyToReview ?? 'Ready to review';
    return da.statusPending;
  }

  protected trackByDocId(_: number, doc: TrackedDocument): string {
    return doc.documentId;
  }
}
