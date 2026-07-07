import { Component, inject } from '@angular/core';
import { Router } from '@angular/router';
import { CommonModule } from '@angular/common';

@Component({
  selector: 'app-onboarding-ai-upload',
  standalone: true,
  imports: [CommonModule],
  templateUrl: './onboarding-ai-upload.html',
  styleUrl: './onboarding-ai-upload.css',
})
export class OnboardingAiUpload {
  private readonly router = inject(Router);

  readonly docTypes = [
    { id: 'lab',          label: 'Lab Report'    },
    { id: 'prescription', label: 'Prescription'  },
    { id: 'radiology',    label: 'Radiology'     },
    { id: 'medical',      label: 'Medical Report'},
    { id: 'other',        label: 'Other'         },
  ];

  readonly extractItems = [
    'Blood Type',
    'Allergies',
    'Chronic Diseases',
    'Medications',
    'And more...',
  ];

  readonly resultItems = [
    { value: 'A+',        category: 'Blood Type' },
    { value: 'Penicillin',category: 'Allergy'    },
    { value: 'Diabetes',  category: 'Disease'    },
    { value: 'Metformin', category: 'Medication' },
  ];

  uploadedFiles: Record<string, File | null> = {};

  goBack(): void {
    this.router.navigate(['/onboarding/step4']);
  }

  triggerUpload(docId: string): void {
    const el = document.getElementById('file-' + docId) as HTMLInputElement;
    el?.click();
  }

  onFileSelected(event: Event, docId: string): void {
    const input = event.target as HTMLInputElement;
    if (input.files?.[0]) {
      this.uploadedFiles[docId] = input.files[0];
    }
  }

  hasUpload(docId: string): boolean {
    return !!this.uploadedFiles[docId];
  }

  chooseFiles(): void {
    const input = document.createElement('input');
    input.type = 'file';
    input.multiple = true;
    input.accept = '.pdf,.jpg,.jpeg,.png,.doc,.docx';
    input.onchange = (e: Event) => {
      const files = (e.target as HTMLInputElement).files;
      if (files) {
        Array.from(files).forEach((f, i) => {
          const key = this.docTypes[i % this.docTypes.length].id;
          this.uploadedFiles[key] = f;
        });
      }
    };
    input.click();
  }
}
