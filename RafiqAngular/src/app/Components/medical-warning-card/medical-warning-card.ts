import { Component, Input, inject } from '@angular/core';
import { CommonModule } from '@angular/common';
import { LocalizationService } from '../../Services/localization.service';

export interface MedicalWarningData {
  requiresMedicalAttention?: boolean;
  medicalAttentionReason?: string;
  recommendedSpecialty?: string;
  attentionLevel?: string;
  confidenceScore?: number;
}

@Component({
  selector: 'app-medical-warning-card',
  standalone: true,
  imports: [CommonModule],
  templateUrl: './medical-warning-card.html',
  styleUrl: './medical-warning-card.css'
})
export class MedicalWarningCardComponent {
  @Input() data!: MedicalWarningData;
  @Input() compact = false;

  public loc = inject(LocalizationService);
  t = this.loc.t;
}

