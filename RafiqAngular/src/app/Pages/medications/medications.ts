import {
  Component, inject, OnInit, signal, computed, HostListener,
} from '@angular/core';
import { CommonModule } from '@angular/common';
import { RouterLink, RouterLinkActive } from '@angular/router';
import { AuthService } from '../../Services/auth-service';
import { NotificationService } from '../../Services/notification.service';
import { MedicationRemindersService } from '../../Services/medication-reminders.service';
import { MedicationReminderLogDto, MedicationReminderStatus } from '../../Modles/medication-reminder.models';
import { UserMedicine } from '../../Modles/dashboard.models';

type MedTab = 'schedule' | 'medications';

interface Toast {
  id: number;
  message: string;
  type: 'success' | 'error';
}

@Component({
  selector: 'app-medications',
  standalone: true,
  imports: [CommonModule, RouterLink, RouterLinkActive],
  templateUrl: './medications.html',
  styleUrl: './medications.css',
})
export class Medications implements OnInit {
  private readonly authSvc   = inject(AuthService);
  private readonly notifSvc  = inject(NotificationService);
  private readonly medSvc    = inject(MedicationRemindersService);

  // ── Layout ──────────────────────────────────────────────────────────────
  readonly sidebarCollapsed  = signal(false);
  readonly mobileSidebarOpen = signal(false);
  readonly dropdownOpen      = signal(false);

  // ── Tabs ─────────────────────────────────────────────────────────────────
  readonly activeTab = signal<MedTab>('schedule');

  // ── Today's Schedule ──────────────────────────────────────────────────────
  readonly todayLogs       = signal<MedicationReminderLogDto[]>([]);
  readonly scheduleLoading = signal(true);
  readonly scheduleError   = signal<string | null>(null);

  // ── My Medications ────────────────────────────────────────────────────────
  readonly medicines     = signal<UserMedicine[]>([]);
  readonly medsLoading   = signal(false);
  readonly medsError     = signal<string | null>(null);

  // ── Confirm modal ─────────────────────────────────────────────────────────
  readonly showConfirmModal  = signal(false);
  readonly confirmingLog     = signal<MedicationReminderLogDto | null>(null);
  readonly confirming        = signal(false);

  // ── Toasts ───────────────────────────────────────────────────────────────
  private toastSeq = 0;
  readonly toasts  = signal<Toast[]>([]);

  // ── Derived counts ───────────────────────────────────────────────────────
  readonly confirmedCount = computed(() =>
    this.todayLogs().filter(l => l.status === 'Confirmed').length
  );
  readonly pendingCount = computed(() =>
    this.todayLogs().filter(l => l.status === 'Pending' || l.status === 'Sent').length
  );
  readonly cancelledCount = computed(() =>
    this.todayLogs().filter(l => l.status === 'Cancelled').length
  );
  readonly uniqueScheduledCount = computed(() => {
    const ids = new Set(this.todayLogs().map(l => l.medicineReminderId));
    return ids.size;
  });

  // Logs sorted by time then reminder number — unique "latest" per schedule
  readonly sortedLogs = computed(() =>
    [...this.todayLogs()].sort((a, b) => {
      if (a.scheduledTime < b.scheduledTime) return -1;
      if (a.scheduledTime > b.scheduledTime) return 1;
      return a.reminderNumber - b.reminderNumber;
    })
  );

  readonly unreadCount = this.notifSvc.unreadCount;

  // ── Auth helpers ──────────────────────────────────────────────────────────
  get displayName(): string {
    const u = this.authSvc.currentUser;
    return u?.firstName?.trim() || u?.email || 'there';
  }
  get userEmail(): string { return this.authSvc.currentUser?.email ?? ''; }

  // ── Lifecycle ─────────────────────────────────────────────────────────────
  ngOnInit(): void {
    this.applyResponsiveSidebar();
    this.loadSchedule();
  }

  @HostListener('window:resize')
  onResize(): void { this.applyResponsiveSidebar(); }

  @HostListener('document:click', ['$event'])
  onDocumentClick(e: MouseEvent): void {
    const t = e.target as HTMLElement;
    if (!t.closest('.hdr-user')) this.dropdownOpen.set(false);
  }

  private applyResponsiveSidebar(): void {
    this.sidebarCollapsed.set(window.innerWidth <= 1024);
    if (window.innerWidth > 768) this.mobileSidebarOpen.set(false);
  }

  toggleSidebar(): void       { this.sidebarCollapsed.update(v => !v); }
  toggleMobileSidebar(): void { this.mobileSidebarOpen.update(v => !v); }
  toggleDropdown(): void      { this.dropdownOpen.update(v => !v); }
  logout(): void { this.dropdownOpen.set(false); this.authSvc.logout().subscribe(); }

  // ── Tabs ──────────────────────────────────────────────────────────────────
  setTab(tab: MedTab): void {
    this.activeTab.set(tab);
    if (tab === 'medications' && this.medicines().length === 0 && !this.medsLoading()) {
      this.loadMedicines();
    }
  }

  // ── Data loading ──────────────────────────────────────────────────────────
  loadSchedule(): void {
    this.scheduleLoading.set(true);
    this.scheduleError.set(null);
    this.medSvc.getToday().subscribe({
      next:  data => { this.todayLogs.set(data); this.scheduleLoading.set(false); },
      error: err  => {
        this.scheduleError.set(err?.error?.message ?? 'Could not load today\'s schedule.');
        this.scheduleLoading.set(false);
      },
    });
  }

  loadMedicines(): void {
    this.medsLoading.set(true);
    this.medsError.set(null);
    this.medSvc.getUserMedicines().subscribe({
      next:  data => { this.medicines.set(data); this.medsLoading.set(false); },
      error: err  => {
        this.medsError.set(err?.error?.message ?? 'Could not load medications.');
        this.medsLoading.set(false);
      },
    });
  }

  // ── Confirm medication ────────────────────────────────────────────────────
  openConfirm(log: MedicationReminderLogDto): void {
    this.confirmingLog.set(log);
    this.showConfirmModal.set(true);
  }

  closeConfirm(): void {
    if (this.confirming()) return;
    this.showConfirmModal.set(false);
    this.confirmingLog.set(null);
  }

  submitConfirm(): void {
    const log = this.confirmingLog();
    if (!log || this.confirming()) return;

    this.confirming.set(true);
    this.medSvc.confirm(log.id).subscribe({
      next: () => {
        this.todayLogs.update(list =>
          list.map(l => l.id === log.id ? { ...l, status: 'Confirmed' as MedicationReminderStatus, confirmedAt: new Date().toISOString() } : l)
        );
        this.confirming.set(false);
        this.showConfirmModal.set(false);
        this.confirmingLog.set(null);
        this.toast(`${log.medicineName} marked as taken. Great job! 💊`, 'success');
        this.notifSvc.push({
          title: 'Medication Confirmed',
          body:  `${log.medicineName} ${log.dosage} taken at ${this.formatTime(log.scheduledTime)}`,
          type:  'reminder',
        });
      },
      error: err => {
        this.toast(err?.error?.message ?? 'Could not confirm medication.', 'error');
        this.confirming.set(false);
      },
    });
  }

  // ── Toast ─────────────────────────────────────────────────────────────────
  toast(message: string, type: 'success' | 'error'): void {
    const id = ++this.toastSeq;
    this.toasts.update(t => [...t, { id, message, type }]);
    setTimeout(() => this.toasts.update(t => t.filter(x => x.id !== id)), 4500);
  }
  dismissToast(id: number): void { this.toasts.update(t => t.filter(x => x.id !== id)); }

  // ── Display helpers ───────────────────────────────────────────────────────
  formatTime(time: string): string {
    // "08:00:00" → "8:00 AM"
    const [h, m] = time.split(':').map(Number);
    const period = h >= 12 ? 'PM' : 'AM';
    const hour   = h % 12 || 12;
    return `${hour}:${String(m).padStart(2, '0')} ${period}`;
  }

  formatDate(dateStr: string): string {
    return new Date(dateStr).toLocaleDateString('en-US', {
      weekday: 'short', month: 'short', day: 'numeric',
    });
  }

  statusLabel(status: MedicationReminderStatus): string {
    const map: Record<MedicationReminderStatus, string> = {
      Pending:   'Upcoming',
      Sent:      'Reminder Sent',
      Confirmed: 'Completed',
      Cancelled: 'Cancelled',
    };
    return map[status] ?? status;
  }

  statusClass(status: MedicationReminderStatus): string {
    const map: Record<MedicationReminderStatus, string> = {
      Pending:   'pill pill-yellow-sm',
      Sent:      'pill pill-blue-sm',
      Confirmed: 'pill pill-green-sm',
      Cancelled: 'pill pill-red-sm',
    };
    return map[status] ?? 'pill';
  }

  statusIcon(status: MedicationReminderStatus): string {
    const map: Record<MedicationReminderStatus, string> = {
      Pending:   'fa-clock',
      Sent:      'fa-bell',
      Confirmed: 'fa-circle-check',
      Cancelled: 'fa-ban',
    };
    return map[status] ?? 'fa-circle';
  }

  canConfirm(status: MedicationReminderStatus): boolean {
    return status === 'Pending' || status === 'Sent';
  }

  reminderAttemptLabel(n: number): string {
    return n === 1 ? '1st reminder' : n === 2 ? '2nd reminder' : '3rd reminder';
  }

  sourceLabel(source: string): string {
    const map: Record<string, string> = {
      Manual:       'Manual',
      Prescription: 'Prescription',
      MedicineBox:  'Box Scan',
      '1': 'Manual',
      '2': 'Prescription',
      '3': 'Box Scan',
    };
    return map[source] ?? source;
  }

  sourceClass(source: string): string {
    if (source === 'Prescription' || source === '2') return 'pill pill-purple-sm';
    if (source === 'MedicineBox'  || source === '3') return 'pill pill-teal-sm';
    return 'pill pill-blue-sm';
  }

  medIconClass(index: number): string {
    const classes = ['med-ico-orange', 'med-ico-yellow', 'med-ico-pink', 'med-ico-blue', 'med-ico-teal'];
    return classes[index % classes.length];
  }

  todayLabel(): string {
    return new Date().toLocaleDateString('en-US', {
      weekday: 'long', month: 'long', day: 'numeric',
    });
  }
}
