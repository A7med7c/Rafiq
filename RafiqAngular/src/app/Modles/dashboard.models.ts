// ===== Lab Report =====
export interface LabResult {
  id: string;
  testName: string;
  value: string;
  unit: string;
  normalRange: string;
  status: string;
}

export interface LabReport {
  id: string;
  labName: string;
  doctorName?: string;
  reportDate?: string;
  ocrText?: string;
  summary?: string;
  imageUrl?: string;
  createdAt: string;
  results: LabResult[];
}

// ===== Imaging Report =====
export interface ImagingReport {
  id: string;
  imagingType: string;
  bodyPart: string;
  findings: string;
  impression: string;
  doctorName?: string;
  reportDate?: string;
  imageUrl?: string;
  ocrText?: string;
  summary?: string;
  createdAt: string;
}

// ===== Prescription =====
export interface PrescriptionMedicine {
  id: string;
  medicineName: string;
  dosage: string;
  frequency: string;
  duration: string;
  instructions?: string;
}

export interface Prescription {
  id: string;
  doctorName: string;
  patientName: string;
  prescriptionDate?: string;
  imagePath?: string;
  createdAt: string;
  medicines: PrescriptionMedicine[];
}

// ===== Unified Medical Record (for display) =====
export type MedicalRecordType = 'lab' | 'imaging' | 'prescription';

export interface MedicalRecord {
  id: string;
  type: MedicalRecordType;
  title: string;
  subtitle?: string;
  date: string;
  source?: string;
  status: string;
  statusColor: 'success' | 'warning' | 'info' | 'danger';
}

// ===== User Medicine =====
export interface UserMedicine {
  id: string;
  medicineName: string;
  dosage: string;
  frequency: string;
  duration: string;
  notes?: string;
  imagePath?: string;
  source: string;
  createdAt: string;
  updatedAt?: string;
}

// ===== Medicine Reminder =====
export interface MedicineReminder {
  id: string;
  userMedicineId: string;
  reminderTime: string;
  startDate: string;
  endDate: string;
  repeatType: string;
  isEnabled: boolean;
  lastTriggeredAt?: string;
  createdAt: string;
  updatedAt?: string;
}

// ===== Merged Reminder Display Item =====
export interface ReminderDisplayItem {
  id: string;
  medicineName: string;
  dosage: string;
  frequency: string;
  reminderTime: string;
  isEnabled: boolean;
  repeatType: string;
}
