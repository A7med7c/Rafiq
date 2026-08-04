import { Component, OnInit, inject, signal } from '@angular/core';
import { CommonModule } from '@angular/common';
import { ActivatedRoute, Router } from '@angular/router';
import { Share } from '@capacitor/share';
import { LocalizationService } from '../../Services/localization.service';
import { NotificationService } from '../../Services/notification.service';
import { MedicalRecordsService, UnifiedMedicalRecord } from '../../Services/medical-records.service';
import { PdfService } from '../../Services/pdf.service';
import { environment } from '../../Environments/Environment';
import { BottomNav } from '../../shared/bottom-nav/bottom-nav';
import { MobileHeader } from '../../shared/mobile-header/mobile-header';

@Component({
  selector: 'app-record-detail',
  standalone: true,
  imports: [CommonModule, BottomNav, MobileHeader],
  templateUrl: './record-detail.html',
  styleUrl: './record-detail.css',
})
export class RecordDetail implements OnInit {
  protected readonly l10n = inject(LocalizationService);
  protected readonly t = this.l10n.t;
  protected readonly notifSvc = inject(NotificationService);
  private readonly recordsService = inject(MedicalRecordsService);
  private readonly pdfService = inject(PdfService);
  private readonly route = inject(ActivatedRoute);
  readonly router = inject(Router);
  private readonly base = environment.apiUrl;

  readonly record = signal<UnifiedMedicalRecord | null>(null);
  readonly loading = signal(true);
  readonly notFound = signal(false);
  readonly imageFailed = signal(false);
  readonly lightboxUrl = signal<string | null>(null);

  readonly showDeleteConfirm = signal(false);
  readonly deleting = signal(false);
  readonly deleteError = signal('');

  private profileId: string | undefined;

  ngOnInit(): void {
    const id = this.route.snapshot.paramMap.get('id');
    this.profileId = this.route.snapshot.queryParamMap.get('profileId') ?? undefined;
    if (!id) {
      this.loading.set(false);
      this.notFound.set(true);
      return;
    }

    this.recordsService.getAllData(this.profileId).subscribe({
      next: res => {
        const all = this.recordsService.toUnifiedRecords(res);
        const found = all.find(r => r.id === id) ?? null;
        this.record.set(found);
        this.notFound.set(!found);
        this.loading.set(false);
      },
      error: () => {
        this.loading.set(false);
        this.notFound.set(true);
      },
    });
  }

  goBack(): void {
    this.router.navigate(['/medical-records']);
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

  downloadRecord(): void {
    const rec = this.record();
    if (rec) this.pdfService.download(rec);
  }

  async shareRecord(): Promise<void> {
    const rec = this.record();
    if (!rec) return;
    try {
      await Share.share({
        title: rec.name,
        text: `${rec.name} — ${rec.typeLabel} (${rec.date})\n${rec.summary ?? ''}`.trim(),
        dialogTitle: rec.name,
      });
    } catch {
      // User cancelled the share sheet — nothing to do.
    }
  }

  editRecord(): void {
    const rec = this.record();
    if (!rec) return;
    this.router.navigate(['/medical-records'], { queryParams: { editId: rec.id, profileId: this.profileId ?? null } });
  }

  openDeleteConfirm(): void {
    this.deleteError.set('');
    this.showDeleteConfirm.set(true);
  }

  closeDeleteConfirm(): void {
    if (this.deleting()) return;
    this.showDeleteConfirm.set(false);
  }

  confirmDelete(): void {
    const rec = this.record();
    if (!rec || this.deleting()) return;
    this.deleting.set(true);
    this.recordsService.deleteRecord(rec).subscribe({
      next: () => {
        this.deleting.set(false);
        this.router.navigate(['/medical-records']);
      },
      error: () => {
        this.deleting.set(false);
        this.deleteError.set(this.t().records.deleteConfirm);
      },
    });
  }
}
