export enum Gender {
  Male = 1,
  Female = 2
}

export enum BloodType {
  APositive = 1,  // A+
  ANegative = 2,  // A-
  BPositive = 3,  // B+
  BNegative = 4,  // B-
  ABPositive = 5, // AB+
  ABNegative = 6, // AB-
  OPositive = 7,  // O+
  ONegative = 8   // O-
}

export enum DocumentType {
  LabReport = 1,
  Prescription = 2,
  Radiology = 3,
  MedicalReport = 4,
  Other = 5
}

export enum AllergySeverity {
  Mild = 1,
  Moderate = 2,
  Severe = 3
}

export enum DiseaseStatus {
  Active = 1,
  Controlled = 2,
  Resolved = 3
}

export enum LabResultStatus {
  Low = 1,
  Normal = 2,
  High = 3,
  Critical = 4
}
