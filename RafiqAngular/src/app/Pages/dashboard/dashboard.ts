import { Component, inject, OnInit, signal } from '@angular/core';
import { CommonModule } from '@angular/common';
import { AuthService } from '../../Services/auth-service';
import { DashboardService } from '../../Services/dashboard.service';
import { MedicalRecord, ReminderDisplayItem } from '../../Modles/dashboard.models';

const DEMO_RECORDS: MedicalRecord[] = [
  { id: 'd1', type: 'prescription', title: 'Prescription - Amoxicillin', subtitle: 'Dr. Mohamed Ali',   date: 'Today, 9:15 AM',          source: 'Dr. Mohamed Ali',   status: 'Processed', statusColor: 'success' },
  { id: 'd2', type: 'lab',          title: 'Blood Test Results',          subtitle: 'Central Lab',        date: 'Yesterday, 4:30 PM',      source: 'Central Lab',       status: 'Processed', statusColor: 'success' },
  { id: 'd3', type: 'imaging',      title: 'Chest X-Ray',                 subtitle: 'City Medical Center',date: 'Jul 4, 2026',             source: 'City Medical Center',status: 'Processed', statusColor: 'success' },
  { id: 'd4', type: 'lab',          title: 'Complete Blood Count',         subtitle: 'Dr. Layla Ibrahim',  date: 'Jun 28, 2026',            source: 'Al-Salama Lab',     status: 'Processed', statusColor: 'success' },
  { id: 'd5', type: 'lab',          title: 'Vitamin D Test',               subtitle: 'Dr. Karim Mansour',  date: 'Jun 15, 2026',            source: 'MedLab Egypt',      status: 'Processed', statusColor: 'success' },
];

const DEMO_REMINDERS: ReminderDisplayItem[] = [
  { id: 'r1', medicineName: 'Amoxicillin',   dosage: '500mg',   frequency: 'Every 8 hours', reminderTime: 'Next: 2:00 PM',        isEnabled: true, repeatType: 'Every 8 hours' },
  { id: 'r2', medicineName: 'Vitamin D3',    dosage: '1 tablet', frequency: 'Daily',         reminderTime: 'Next: Tomorrow 8:00 AM', isEnabled: true, repeatType: 'Daily' },
  { id: 'r3', medicineName: 'Antihistamine', dosage: '1 tablet', frequency: 'Daily',         reminderTime: 'Next: 9:00 PM',        isEnabled: true, repeatType: 'Daily' },
];

@Component({
  selector: 'app-dashboard',
  standalone: true,
  imports: [CommonModule],
  templateUrl: './dashboard.html',
  styleUrl: './dashboard.css',
})
export class Dashboard implements OnInit {
  private readonly authService    = inject(AuthService);
  private readonly dashboardService = inject(DashboardService);

  readonly records          = signal<MedicalRecord[]>([]);
  readonly reminders        = signal<ReminderDisplayItem[]>([]);
  readonly recordsLoading   = signal(true);
  readonly remindersLoading = signal(true);
  readonly sidebarCollapsed = signal(false);
  readonly today            = new Date();

  get displayName(): string {
    const u = this.authService.currentUser;
    if (!u) return 'there';
    return u.firstName?.trim() || u.email;
  }

  get greeting(): string {
    const h = new Date().getHours();
    if (h < 12) return 'Good morning';
    if (h < 17) return 'Good afternoon';
    return 'Good evening';
  }

  ngOnInit(): void {
    this.dashboardService.getMedicalRecords().subscribe({
      next:  d => { this.records.set(d); this.recordsLoading.set(false); },
      error: () => { this.records.set([]); this.recordsLoading.set(false); },
    });

    this.dashboardService.getMedicinesWithReminders().subscribe({
      next:  d => { this.reminders.set(d.length ? d : DEMO_REMINDERS); this.remindersLoading.set(false); },
      error: () => { this.reminders.set(DEMO_REMINDERS); this.remindersLoading.set(false); },
    });
  }

  getRecordIcon(type: string): string {
    switch (type) {
      case 'lab':          return 'fa-flask';
      case 'imaging':      return 'fa-x-ray';
      case 'prescription': return 'fa-prescription-bottle-medical';
      default:             return 'fa-file-medical';
    }
  }

  getRecordIconClass(type: string): string {
    switch (type) {
      case 'lab':          return 'rec-ico-blue';
      case 'imaging':      return 'rec-ico-teal';
      case 'prescription': return 'rec-ico-purple';
      default:             return 'rec-ico-gray';
    }
  }

  getMedIconClass(index: number): string {
    const classes = ['med-ico-orange', 'med-ico-yellow', 'med-ico-pink', 'med-ico-blue', 'med-ico-teal'];
    return classes[index % classes.length];
  }
}
