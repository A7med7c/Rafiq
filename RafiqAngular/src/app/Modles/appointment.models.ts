export enum AppointmentType {
  DoctorVisit = 1,
  LabTest     = 2,
  Imaging     = 3,
  Vaccination = 4,
  Dentist     = 5,
  Therapy     = 6,
  FollowUp    = 7,
  Other       = 8,
}

export enum AppointmentStatus {
  Upcoming = "Upcoming",
    Completed = "Completed",
    Cancelled = "Cancelled",
    Missed = "Missed"
}

export interface AppointmentDto {
  id: string;
  appointmentType: AppointmentType;
  customType?: string;
  title: string;
  provider: string;
  appointmentDateTime: string;
  reminderOffsetMinutes?: number;
  notes?: string;
  status: AppointmentStatus;
  createdAt: string;
  updatedAt?: string;
}

export interface CreateAppointmentRequest {
  appointmentType: AppointmentType;
  customType?: string;
  title: string;
  provider: string;
  appointmentDateTime: string;
  reminderOffsetMinutes?: number;
  notes?: string;
}

export type UpdateAppointmentRequest = CreateAppointmentRequest;

export const APPOINTMENT_TYPE_LABELS: Record<AppointmentType, string> = {
  [AppointmentType.DoctorVisit]: 'Doctor Visit',
  [AppointmentType.LabTest]:     'Lab / Blood Test',
  [AppointmentType.Imaging]:     'X-Ray & Imaging',
  [AppointmentType.Vaccination]: 'Vaccination',
  [AppointmentType.Dentist]:     'Dental Checkup',
  [AppointmentType.Therapy]:     'Therapy Session',
  [AppointmentType.FollowUp]:    'Follow-up Visit',
  [AppointmentType.Other]:       'Other',
};

export const APPOINTMENT_TYPE_ICONS: Record<AppointmentType, string> = {
  [AppointmentType.DoctorVisit]: 'fa-user-doctor',
  [AppointmentType.LabTest]:     'fa-flask',
  [AppointmentType.Imaging]:     'fa-x-ray',
  [AppointmentType.Vaccination]: 'fa-syringe',
  [AppointmentType.Dentist]:     'fa-tooth',
  [AppointmentType.Therapy]:     'fa-brain',
  [AppointmentType.FollowUp]:    'fa-rotate-right',
  [AppointmentType.Other]:       'fa-calendar-plus',
};
