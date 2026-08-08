import { Injectable, inject, signal, computed, effect } from '@angular/core';
import { Subject } from 'rxjs';
import { HttpClient } from '@angular/common/http';
import { environment } from '../Environments/Environment';
import { AuthService } from './auth-service';
import {
  SignalRService,
  DocumentAnalysisFailedPayload,
} from './signalr.service';
import { Router } from '@angular/router';
import { LocalizationService } from './localization.service';
import { NotificationService } from './notification.service';
import { localizeKnownApiMessage } from '../Utils/api-error.util';

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
  rawFile?: File;
  rawDesc?: string;
}

export interface PendingReviewRequest {
  uploadType: UploadDocType;
  data: any;
}

export interface ManualEntryRequest {
  uploadType: UploadDocType;
  imagePath?: string;
  rawFile?: File;
  profileId?: string;
}

/** Payload for the global completion modal shown when user is off /medical-records */
export interface CompletionModalData {
  documentId: string;
  title: string;
  uploadType: UploadDocType;
  reviewData: any;
  profileId?: string;
}

const STORAGE_KEY = 'rafiq_dac_docs';
const NOTIF_IDS_KEY = 'rafiq_dac_notified';

@Injectable({ providedIn: 'root' })
export class DocumentAnalysisStateService {
  private readonly signalR  = inject(SignalRService);
  private readonly http     = inject(HttpClient);
  private readonly auth     = inject(AuthService);
  private readonly router   = inject(Router);
  private readonly l10n     = inject(LocalizationService);
  private readonly notif    = inject(NotificationService);

  readonly trackedDocuments = signal<TrackedDocument[]>(this.loadFromStorage());
  readonly hasAny           = computed(() => this.trackedDocuments().length > 0);
  readonly hasActive        = computed(() =>
    this.trackedDocuments().some(d => d.status === 'Pending' || d.status === 'Processing')
  );
  /** True while any analysis is active OR any result is waiting for review */
  readonly hasAttention     = computed(() =>
    this.trackedDocuments().some(d =>
      d.status === 'Pending' || d.status === 'Processing' || d.status === 'ReadyToReview'
    )
  );

  /** True when any result is waiting for review */
  readonly hasPendingReview = computed(() =>
    this.trackedDocuments().some(d => d.status === 'ReadyToReview')
  );

  readonly sidebarStatus = computed<'processing' | 'failed' | 'completed' | 'none'>(() => {
    const docs = this.trackedDocuments();
    if (docs.some(d => d.status === 'Pending' || d.status === 'Processing')) return 'processing';
    if (docs.some(d => d.status === 'Failed')) return 'failed';
    if (docs.some(d => d.status === 'ReadyToReview')) return 'completed';
    return 'none';
  });

  readonly pendingReview = signal<PendingReviewRequest | null>(null);
  readonly manualEntryRequest = signal<ManualEntryRequest | null>(null);

  /** Drives the global completion modal. Null = hidden. */
  readonly completionModal = signal<CompletionModalData | null>(null);

  /** Drives the global failure modal. Null = hidden. */
  readonly failureModal = signal<TrackedDocument | null>(null);

  private readonly _cancelRequested = new Subject<string>();
  readonly cancelRequested$ = this._cancelRequested.asObservable();

  private pollInterval: ReturnType<typeof setInterval> | null = null;
  /** IDs for which we already pushed a notification/modal, to prevent duplicates. */
  private readonly notifiedIds = new Set<string>(this.loadNotifiedIds());

  constructor() {
    // Persist to localStorage whenever docs change
    effect(() => {
      this.saveToStorage(this.trackedDocuments());
    });

    // Clear on logout (don't leak one user's docs to the next)
    this.auth.currentUser$.subscribe(user => {
      if (!user) {
        this.trackedDocuments.set([]);
        this.notifiedIds.clear();
        this.completionModal.set(null);
        this.failureModal.set(null);
        this.manualEntryRequest.set(null);
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

    // Watch for newly ReadyToReview docs and trigger global completion modal + notification ONLY when outside /medical-records
    effect(() => {
      const docs = this.trackedDocuments();
      const readyDocs = docs.filter(d => d.status === 'ReadyToReview');
      for (const doc of readyDocs) {
        const isOnRecords = this.router.url.startsWith('/medical-records');
        const notifKey = doc.documentId + '_success';
        if (isOnRecords) {
          // User is on /medical-records -> no bell notification
          this.markNotified(notifKey);
        } else {
          // User is outside /medical-records -> send ONE bell notification and show completion modal
          if (!this.isNotified(notifKey)) {
            this.markNotified(notifKey);
            this.pushBellNotification(doc, 'success');
            this.showCompletionModal(doc);
          }
        }
      }
    });

    // Watch for newly Failed docs and trigger global failure modal + notification ONLY when outside /medical-records
    effect(() => {
      const docs = this.trackedDocuments();
      const failedDocs = docs.filter(d => d.status === 'Failed');
      for (const doc of failedDocs) {
        const isOnRecords = this.router.url.startsWith('/medical-records');
        const notifKey = doc.documentId + '_fail';
        if (isOnRecords) {
          // User is on /medical-records -> no bell notification
          this.markNotified(notifKey);
        } else {
          // User is outside /medical-records -> send ONE bell notification and show failure modal
          if (!this.isNotified(notifKey)) {
            this.markNotified(notifKey);
            this.pushBellNotification(doc, 'error');
            this.showFailureModal(doc);
          }
        }
      }
    });
  }

  private pushBellNotification(doc: TrackedDocument, type: 'success' | 'error'): void {
    if (type === 'error') {
      this.notif.push({
        title: 'Medical document analysis failed',
        titleAr: 'فشل تحليل المستند الطبي',
        body: 'We couldn\'t analyze the document using AI.',
        bodyAr: 'تعذر تحليل المستند باستخدام الذكاء الاصطناعي.',
        type: 'system',
        sourceId: doc.documentId,
      });
      return;
    }

    const enBodyMap: Record<string, string> = {
      lab: 'Your Lab Report is ready for review.',
      imaging: 'Your Imaging Report is ready for review.',
      prescription: 'Your Prescription is ready for review.',
      general: 'Your medical document is ready for review.',
      medicine: 'Your medicine record is ready for review.',
    };

    const arBodyMap: Record<string, string> = {
      lab: 'تقرير التحليل المعملي جاهز للمراجعة.',
      imaging: 'تقرير الأشعة جاهز للمراجعة.',
      prescription: 'الوصفة الطبية جاهزة للمراجعة.',
      general: 'المستند الطبي جاهز للمراجعة.',
      medicine: 'سجل الدواء جاهز للمراجعة.',
    };

    this.notif.push({
      title: 'Medical document analysis completed',
      titleAr: 'اكتمل تحليل المستند الطبي',
      body: enBodyMap[doc.uploadType] ?? 'Your medical document is ready for review.',
      bodyAr: arBodyMap[doc.uploadType] ?? 'المستند الطبي جاهز للمراجعة.',
      type: 'system',
      sourceId: doc.documentId,
    });
  }

  // ── Sync upload tracking (lab / imaging / prescription) ──────────────────

  trackSyncUpload(tempId: string, title: string, uploadType: UploadDocType, profileId?: string, file?: File): void {
    const doc: TrackedDocument = {
      documentId: tempId, title, imagePath: '', uploadType,
      status: 'Pending', documentType: null, aiSummary: null,
      failureReason: null, enqueuedAt: new Date(), profileId,
      rawFile: file
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
    const sanitized = this.sanitizeErrorMessage(reason);
    this.trackedDocuments.update(docs =>
      docs.map(d => d.documentId === tempId
        ? { ...d, status: 'Failed' as AnalysisStatus, failureReason: sanitized }
        : d)
    );
  }

  // ── Async tracking (general documents via Hangfire) ──────────────────────

  trackDocument(documentId: string, title: string, imagePath: string, profileId?: string, file?: File, desc?: string): void {
    const doc: TrackedDocument = {
      documentId, title, imagePath, uploadType: 'general',
      status: 'Processing', documentType: null, aiSummary: null,
      failureReason: null, enqueuedAt: new Date(), profileId,
      rawFile: file, rawDesc: desc
    };
    this.trackedDocuments.update(docs => [...docs, doc]);
  }

  // ── Review & Manual Entry handoff ─────────────────────────────────────────

  requestReview(doc: TrackedDocument): void {
    if (!doc.reviewData) return;
    this.pendingReview.set({ uploadType: doc.uploadType, data: doc.reviewData });
    this.dismiss(doc.documentId);
  }

  requestManualEntry(doc: TrackedDocument): void {
    this.manualEntryRequest.set({
      uploadType: doc.uploadType,
      imagePath: doc.imagePath,
      rawFile: doc.rawFile,
      profileId: doc.profileId,
    });
    this.dismiss(doc.documentId);
  }

  clearManualEntryRequest(): void {
    this.manualEntryRequest.set(null);
  }

  cancelAnalysis(documentId: string): void {
    this._cancelRequested.next(documentId);
    this.dismiss(documentId);
  }

  clearPendingReview(): void {
    this.pendingReview.set(null);
  }

  dismiss(documentId: string): void {
    this.trackedDocuments.update(docs => docs.filter(d => d.documentId !== documentId));
  }

  sanitizeErrorMessage(rawMessage?: string | null): string {
    const da = this.l10n.t().documentAnalysis;
    const defaultMsg = da?.docAnalysisFailedDesc || 'We couldn\'t extract data from the file.';
    if (!rawMessage || typeof rawMessage !== 'string') return defaultMsg;

    const lower = rawMessage.toLowerCase();
    if (
      lower.includes('httprequestexception') ||
      lower.includes('bad gateway') ||
      lower.includes('502') ||
      lower.includes('500') ||
      lower.includes('503') ||
      lower.includes('504') ||
      lower.includes('404') ||
      lower.includes('no such host') ||
      lower.includes('iti.net.eg') ||
      lower.includes('localhost') ||
      lower.includes('stack trace') ||
      lower.includes('exception') ||
      lower.includes('failed to fetch') ||
      lower.includes('networkerror') ||
      lower.includes('cors') ||
      lower.includes('internal server error') ||
      lower.includes('object reference') ||
      lower.includes('nullreference')
    ) {
      return defaultMsg;
    }

    return localizeKnownApiMessage(rawMessage, this.l10n.t());
  }

  // ── Completion modal (global, shown when user is off /medical-records) ────

  /**
   * Show the global completion modal for a ReadyToReview document.
   * Only fires once per documentId (dedup).
   */
  showCompletionModal(doc: TrackedDocument): boolean {
    if (!doc.reviewData) return false;
    if (this.notifiedIds.has(doc.documentId)) return false;
    this.notifiedIds.add(doc.documentId);
    this.saveNotifiedIds();
    this.completionModal.set({
      documentId: doc.documentId,
      title: doc.title,
      uploadType: doc.uploadType,
      reviewData: doc.reviewData,
      profileId: doc.profileId,
    });
    return true;
  }

  dismissCompletionModal(): void {
    this.completionModal.set(null);
  }

  /** Convert completion modal → pendingReview and dismiss both. */
  acceptCompletionModal(): PendingReviewRequest | null {
    const modal = this.completionModal();
    if (!modal) return null;
    const request: PendingReviewRequest = { uploadType: modal.uploadType, data: modal.reviewData };
    this.pendingReview.set(request);
    this.completionModal.set(null);
    // Remove from tracked (it was ReadyToReview)
    this.dismiss(modal.documentId);
    return request;
  }

  showFailureModal(doc: TrackedDocument): void {
    this.failureModal.set(doc);
    this.markNotified(doc.documentId + '_fail');
  }

  dismissFailureModal(): void {
    this.failureModal.set(null);
  }

  retryAnalysis(doc: TrackedDocument): void {
    // Dismiss failure modal if open
    if (this.failureModal()?.documentId === doc.documentId) {
      this.dismissFailureModal();
    }

    // Immediately update state back to Processing and clear old error
    this.trackedDocuments.update(docs =>
      docs.map(d => d.documentId === doc.documentId
        ? { ...d, status: 'Processing' as AnalysisStatus, failureReason: null }
        : d)
    );
    
    // Remove the notification marks so it can notify again on final state if user is away
    this.notifiedIds.delete(doc.documentId + '_fail');
    this.notifiedIds.delete(doc.documentId + '_success');
    this.saveNotifiedIds();

    const pid = doc.profileId ? `?profileId=${doc.profileId}` : '';

    if (doc.uploadType === 'general' && !doc.rawFile) {
      // It's a true async document queued in Hangfire
      this.http.post<any>(`${environment.apiUrl}/documents/general/${doc.documentId}/retry`, {}).subscribe({
        error: (err) => {
          const reason = this.sanitizeErrorMessage(err?.error?.message ?? this.l10n.t().documentAnalysis.retryFailedBody);
          this.failSyncUpload(doc.documentId, reason);
        }
      });
      return;
    }

    // Otherwise, it's a sync upload (lab/imaging/prescription) or a general document where we retained the file
    if (!doc.rawFile) {
      this.failSyncUpload(doc.documentId, this.l10n.t().documentAnalysis.docAnalysisFailedDesc || 'We couldn\'t analyze the document right now.');
      return;
    }

    const form = new FormData();
    form.append('image', doc.rawFile);
    // bypass duplicate check automatically on retry
    form.append('bypassFamilyDuplicateCheck', 'true');

    if (doc.uploadType === 'general' && doc.rawDesc) {
       form.append('description', doc.rawDesc);
    }

    let url = '';
    if (doc.uploadType === 'general') {
       url = `${environment.apiUrl}/documents/general/upload-async${pid}`;
    } else if (doc.uploadType === 'lab') {
       url = `${environment.apiUrl}/documents/upload/lab${pid}`;
    } else if (doc.uploadType === 'imaging') {
       url = `${environment.apiUrl}/documents/upload/imaging${pid}`;
    } else if (doc.uploadType === 'prescription') {
       url = `${environment.apiUrl}/prescriptions/upload${pid}`;
    } else if (doc.uploadType === 'medicine') {
       url = `${environment.apiUrl}/medicines/extract${pid}`;
    }

    this.http.post<any>(url, form).subscribe({
      next: res => {
        if (res && res.success === false) {
           this.failSyncUpload(doc.documentId, this.sanitizeErrorMessage(res.message));
           return;
        }
        if (doc.uploadType === 'general' || doc.uploadType === 'medicine') {
           // For async endpoints, they return immediately and signalR will update the state
           this.trackedDocuments.update(docs => docs.map(d => d.documentId === doc.documentId ? { ...d, status: 'Processing' as AnalysisStatus, failureReason: null } : d));
        } else {
           // For sync endpoints, this IS the completion
           const data = res?.data ?? res;
           this.completeWithReviewData(doc.documentId, data);
        }
      },
      error: err => {
         const rawReason = err?.error?.message ?? err?.message ?? this.l10n.t().records.aiAnalysisFailed;
         const reason = this.sanitizeErrorMessage(rawReason);
         this.failSyncUpload(doc.documentId, reason);
      }
    });
  }

  /**
   * Mark a documentId as notified so no duplicate modal/notification fires.
   * Used by records-content when it opens the review form directly.
   */
  markNotified(documentId: string): void {
    if (!this.notifiedIds.has(documentId)) {
      this.notifiedIds.add(documentId);
      this.saveNotifiedIds();
    }
  }

  /** Returns true if a notification/modal has already been shown for this documentId. */
  isNotified(documentId: string): boolean {
    return this.notifiedIds.has(documentId);
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

  private loadNotifiedIds(): string[] {
    try {
      const raw = localStorage.getItem(NOTIF_IDS_KEY);
      if (!raw) return [];
      const ids: string[] = JSON.parse(raw);
      return ids;
    } catch { return []; }
  }

  private saveNotifiedIds(): void {
    try {
      localStorage.setItem(NOTIF_IDS_KEY, JSON.stringify([...this.notifiedIds]));
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
