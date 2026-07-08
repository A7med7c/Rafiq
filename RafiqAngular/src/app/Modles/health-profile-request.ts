/** Request body for POST /api/patient-profiles */
export interface CreateAllergyRequest {
  name: string;
  /** String name of AllergySeverity enum: "Mild" | "Moderate" | "Severe" */
  severity: string;
}

export interface CreateChronicDiseaseRequest {
  name: string;
  /** ISO date string "YYYY-MM-DD" or null */
  diagnosedAt: string | null;
  /** String name of DiseaseStatus enum: "Active" | "Controlled" | "Resolved" */
  status: string;
}

export interface CreatePatientProfileRequest {
  /** ISO date string "YYYY-MM-DD" */
  dateOfBirth: string;
  /** String name of Gender enum: "Male" | "Female" */
  gender: string;
  /** String name of BloodType enum: "APositive" | "ANegative" | etc. */
  bloodType: string;
  height: number;
  weight: number;
  allergies: CreateAllergyRequest[];
  chronicDiseases: CreateChronicDiseaseRequest[];
}
