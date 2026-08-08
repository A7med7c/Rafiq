import { Injectable, inject, signal, computed, effect } from '@angular/core';
import { HttpClient } from '@angular/common/http';
import { environment } from '../Environments/Environment';
import { AuthService } from './auth-service';
import {
  SignalRService,
  DocumentAnalysisFailedPayload,
} from './signalr.service';

export type AnalysisStatus = 'Pending' | 'Processing' | 'Completed' | 'Failed' | 'ReadyToReview';
export type UploadDocType  = 'lab' | 'imaging' | 'prescription' | 'general' | 'medicine';

export interface TrackedDocument {
  documentId: string;
  title: string;
  imagePath: string;
  uploadType: UploadDocType;
  status: AnalysisStatus;
  documentType: string | null;
  aiSummary: string | null;
  failureReason: string | null;
  reviewData?: any;
  enqueuedAt: Date;
  profileId?: string;
}

export interface PendingReviewRequest {
  uploadType: UploadDocType;
  data: any;
}

const STORAGE_KEY = 'rafiq_dac_docs';

@Injectable({ providedIn: 'root' })
export class DocumentAnalysisStateService {
  private readonly signalR  = inject(SignalRService);
  private readonly http     = inject(HttpClient);
  private readonly auth     = inject(AuthService);

  readonly trackedDocuments = signal<TrackedDocument[]>(this.loadFromStorage());
  readonly hasAny           = computed(() => this.trackedDocuments().length > 0);
  readonly hasActive        = computed(() =>
    this.trackedDocuments().some(d => d.status === 'Pending' || d.status === 'Processing')
  );

  readonly pendingReview = signal<PendingReviewRequest | null>(null);

  private pollInterval: ReturnType<typeof setInterval> | null = null;

  constructor() {
    // Persist to localStorage whenever docs change
    effect(() => {
      this.saveToStorage(this.trackedDocuments());
    });

    // Clear on logout (don't leak one user's docs to the next)
    this.auth.currentUser$.subscribe(user => {
      if (!user) {
        this.trackedDocuments.set([]);
      }
    });

    // Drain SignalR completed events
    effect(() => {
      this.signalR.documentAnalysisCompletedEvents();
      const events = this.signalR.drainDocumentAnalysisCompletedEvents();
      if (!events.length) return;
      this.trackedDocuments.update(docs =>
        docs.map(d => {
          const e = events.find(ev => ev.documentId === d.documentId);
          if (!e) return d;
          return { ...d, status: 'Completed' as AnalysisStatus, title: e.title, documentType: e.documentType, aiSummary: e.aiSummary, failureReason: null };
        })
      );
    });

    // Drain SignalR failed events
    effect(() => {
      this.signalR.documentAnalysisFailedEvents();
      const events = this.signalR.drainDocumentAnalysisFailedEvents();
      if (!events.length) return;
      this.trackedDocuments.update(docs =>
        docs.map(d => {
          const e = events.find((ev: DocumentAnalysisFailedPayload) => ev.documentId === d.documentId);
          if (!e) return d;
          return { ...d, status: 'Failed' as AnalysisStatus, failureReason: e.failureReason };
        })
      );
    });

    // Polling fallback for general async docs when SignalR is offline
    effect(() => {
      if (this.hasActive()) this.startPolling();
      else                  this.stopPolling();
    });
  }

  // ── Sync upload tracking (lab / imaging / prescription) ──────────────────

  trackSyncUpload(tempId: string, title: string, uploadType: UploadDocType, profileId?: string): void {
    const doc: TrackedDocument = {
      documentId: tempId, title, imagePath: '', uploadType,
      status: 'Pending', documentType: null, aiSummary: null,
      failureReason: null, enqueuedAt: new Date(), profileId,
    };
    this.trackedDocuments.update(docs => [...docs, doc]);
  }

  completeWithReviewData(tempId: string, data: any): void {
    this.trackedDocuments.update(docs =>
      docs.map(d => d.documentId === tempId
        ? { ...d, status: 'ReadyToReview' as AnalysisStatus, reviewData: data }
        : d)
    );
  }

  failSyncUpload(tempId: string, reason: string): void {
    this.trackedDocuments.update(docs =>
      docs.map(d => d.documentId === tempId
        ? { ...d, status: 'Failed' as AnalysisStatus, failureReason: reason }
        : d)
    );
  }

  // ── Async tracking (general documents via Hangfire) ──────────────────────

  trackDocument(documentId: string, title: string, imagePath: string, profileId?: string): void {
    const doc: TrackedDocument = {
      documentId, title, imagePath, uploadType: 'general',
      status: 'Pending', documentType: null, aiSummary: null,
      failureReason: null, enqueuedAt: new Date(), profileId,
    };
    this.trackedDocuments.update(docs => [...docs, doc]);
  }

  // ── Review handoff ────────────────────────────────────────────────────────

  requestReview(doc: TrackedDocument): void {
    if (!doc.reviewData) return;
    this.pendingReview.set({ uploadType: doc.uploadType, data: doc.reviewData });
    this.dismiss(doc.documentId);
  }

  clearPendingReview(): void {
    this.pendingReview.set(null);
  }

  dismiss(documentId: string): void {
    this.trackedDocuments.update(docs => docs.filter(d => d.documentId !== documentId));
  }

  // ── localStorage ─────────────────────────────────────────────────────────

  private loadFromStorage(): TrackedDocument[] {
    try {
      const raw = localStorage.getItem(STORAGE_KEY);
      if (!raw) return [];
      const docs: TrackedDocument[] = JSON.parse(raw);
      return docs.filter(d => {
        // Sync docs that were mid-flight (Pending) when the tab closed → lost, drop them
        if (d.uploadType !== 'general' && d.status === 'Pending') return false;
        // Drop very old entries (> 24 h) to keep localStorage tidy
        const age = Date.now() - new Date(d.enqueuedAt).getTime();
        return age < 86_400_000;
      });
    } catch { return []; }
  }

  private saveToStorage(docs: TrackedDocument[]): void {
    try {
      // Omit reviewData from storage — can be large; the modal already has it
      const slim = docs.map(({ reviewData, ...d }) => d);
      localStorage.setItem(STORAGE_KEY, JSON.stringify(slim));
    } catch {}
  }

  // ── Polling fallback ─────────────────────────────────────────────────────

  private startPolling(): void {
    if (this.pollInterval) return;
    this.pollInterval = setInterval(() => this.pollPending(), 5000);
  }

  private stopPolling(): void {
    if (!this.pollInterval) return;
    clearInterval(this.pollInterval);
    this.pollInterval = null;
  }

  private pollPending(): void {
    const asyncPending = this.trackedDocuments().filter(
      d => d.uploadType === 'general' && (d.status === 'Pending' || d.status === 'Processing')
    );
    asyncPending.forEach(doc => {
      this.http
        .get<any>(`${environment.apiUrl}/documents/general/status/${doc.documentId}`)
        .subscribe({
          next: res => {
            const status: AnalysisStatus = res?.data?.analysisStatus ?? res?.analysisStatus;
            if (!status || status === doc.status) return;
            this.trackedDocuments.update(docs =>
              docs.map(d => d.documentId !== doc.documentId ? d : {
                ...d, status,
                title:         res?.data?.title         ?? d.title,
                documentType:  res?.data?.documentType  ?? d.documentType,
                aiSummary:     res?.data?.aiSummary     ?? d.aiSummary,
                failureReason: res?.data?.failureReason ?? d.failureReason,
              })
            );
          },
          error: () => {},
        });
    });
  }
}
