import { UserMedicine } from './dashboard.models';

export type MedicationReminderStatus = 'Pending' | 'Sent' | 'Confirmed' | 'Cancelled' | 'Overdue';

export interface MedicationReminderLogDto {
  id: string;
  medicineReminderId: string;
  userHealthProfileId: string;
  medicineName: string;
  dosage: string;
  scheduledDate: string;    // "2026-07-11"
  /** The wall-clock time the user originally configured for this dose, e.g. "20:00:00". */
  reminderTime: string;
  /** The actual fire time for this specific attempt, e.g. "20:10:00" for attempt 2. */
  scheduledTime: string;
  reminderNumber: number;   // 1, 2, or 3
  status: MedicationReminderStatus;
  /**
   * True for exactly one log per dose occurrence: the attempt the user should confirm
   * right now.  Computed server-side — do not re-derive this on the frontend.
   */
  isActionable: boolean;
  sentAt?: string;
  confirmedAt?: string;
  createdAt: string;
}

export type { UserMedicine };
