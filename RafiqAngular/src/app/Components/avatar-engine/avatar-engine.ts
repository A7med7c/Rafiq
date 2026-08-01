/**
 * @file avatar-engine.ts
 * @description Angular Avatar Engine Component for the living Rafiq Robot PNG avatar.
 * Responsibility: Renders the RafiqLogo.png robot image with CSS keyframe animations, glowing effects,
 * particles, and transform states driven by AvatarService.
 */

import { Component, computed, inject, input } from '@angular/core';
import { CommonModule } from '@angular/common';
import { AvatarService } from '../../core/assistant/services/avatar.service';
import { AvatarState } from '../../core/assistant/models/assistant-state';

@Component({
  selector: 'app-avatar-engine',
  standalone: true,
  imports: [CommonModule],
  templateUrl: './avatar-engine.html',
  styleUrl: './avatar-engine.css'
})
export class AvatarEngineComponent {
  readonly avatarService = inject(AvatarService);

  /** Optional state override from parent component */
  readonly stateOverrideInput = input<AvatarState | null>(null, { alias: 'state' });

  /** Size of the avatar container (e.g. '96px', '120px') */
  readonly sizeInput = input<string>('96px', { alias: 'size' });

  /** Computed active state (input override or AvatarService signal) */
  readonly activeState = computed<AvatarState>(() => {
    return this.stateOverrideInput() ?? this.avatarService.state();
  });

  /** Image source for the PNG robot */
  readonly robotImageSrc = this.avatarService.robotImageSrc;
}
