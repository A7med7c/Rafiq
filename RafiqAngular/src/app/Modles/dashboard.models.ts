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

// ===== General Medical Document =====
export interface GeneralMedicalDocument {
  id: string;
  title: string;
  description?: string;
  aiSummary?: string;
  imagePath?: string;
  documentType?: string;
  doctorName?: string | null;
  hospitalOrClinic?: string | null;
  documentDate?: string | null;
  createdAt?: string;
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

// ===== Medicine Box Scan (matches ScanMedicineBoxResponseDto) =====
export interface ScanMedicineBoxResponse {
  medicineName: string | null;
  strength: string | null;
  dosageForm: string | null;
  manufacturer: string | null;
  imagePath: string;
}

// ===== Add UserMedicine Payload =====
export interface AddUserMedicinePayload {
  medicineName: string;
  dosage: string;
  frequency: string;
  duration: string;
  notes?: string;
  imagePath?: string;
  source: number; // 1=Manual, 2=Prescription, 3=MedicineBox
}

// ===== Create Reminder Payload =====
export interface CreateReminderPayload {
  userMedicineId: string;
  times: string[];        // ["HH:mm", ...]
  startDate: string;      // "YYYY-MM-DD"
  endDate: string;        // "YYYY-MM-DD"
  repeatType: string;     // "Once" | "Daily" | "Weekly" | "Monthly"
}

// ===== Update Reminder Payload =====
export interface UpdateReminderPayload {
  id: string;
  reminderTime: string;   // "HH:mm"
  startDate: string;      // "YYYY-MM-DD"
  endDate: string;        // "YYYY-MM-DD"
  repeatType: string;     // "Once" | "Daily" | "Weekly" | "Monthly"
}
