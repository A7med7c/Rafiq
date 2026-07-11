import { UserMedicine } from './dashboard.models';

export type MedicationReminderStatus = 'Pending' | 'Sent' | 'Confirmed' | 'Cancelled';

export interface MedicationReminderLogDto {
  id: string; medicineReminderId: string;
  userHealthProfileId: string;
  medicineName: string; dosage:
  string; scheduledDate: string; // "2026-07-11" 
  scheduledTime: string; // "08:00:00" 
  reminderNumber: number; // 1, 2, or 3 
  status: MedicationReminderStatus;
  sentAt?: string; confirmedAt?:
  string; createdAt: string;
}
export type { UserMedicine };