/**
 * @file avatar.service.ts
 * @description Robot Avatar Engine Controller Service.
 * Responsibility: Controls the avatar state (Idle, Wave, Listening, Thinking, Speaking, PointLeft, PointRight, Celebrate, Error)
 * for the living Rafiq Robot PNG avatar using Angular signals.
 */

import { Injectable, signal } from '@angular/core';
import { AvatarState, AssistantState } from '../models/assistant-state';

@Injectable({
  providedIn: 'root',
})
export class AvatarService {
  /** Path to the robot PNG asset */
  readonly robotImageSrc = 'images/RafiqLogo.png';

  /** Primary avatar state signal */
  private readonly _state = signal<AvatarState>('idle');
  readonly state = this._state.asReadonly();

  private previousState: AvatarState = 'idle';
  private actionTimeout: ReturnType<typeof setTimeout> | null = null;

  /** Sets the current avatar state directly */
  setState(newState: AvatarState): void {
    if (this.actionTimeout) {
      clearTimeout(this.actionTimeout);
      this.actionTimeout = null;
    }
    this._state.set(newState);
  }

  /** Maps orchestrator AssistantState ('idle' | 'listening' | 'speaking' | 'processing' | 'error') to AvatarState */
  setAssistantState(assistantState: AssistantState): void {
    switch (assistantState) {
      case 'listening':
        this.setState('listening');
        break;
      case 'speaking':
        this.setState('speaking');
        break;
      case 'processing':
        this.setState('thinking');
        break;
      case 'error':
        this.setState('error');
        break;
      case 'idle':
      default:
        this.setState('idle');
        break;
    }
  }

  /** Triggers a temporary wave gesture animation */
  triggerWave(durationMs: number = 2400): void {
    this.triggerTemporaryState('wave', durationMs);
  }

  /** Triggers a temporary celebration animation */
  triggerCelebrate(durationMs: number = 2800): void {
    this.triggerTemporaryState('celebrate', durationMs);
  }

  /** Triggers a temporary pointing gesture animation (left or right) */
  triggerPoint(direction: 'left' | 'right', durationMs: number = 2000): void {
    const state: AvatarState = direction === 'left' ? 'pointLeft' : 'pointRight';
    this.triggerTemporaryState(state, durationMs);
  }

  private triggerTemporaryState(tempState: AvatarState, durationMs: number): void {
    if (this.actionTimeout) {
      clearTimeout(this.actionTimeout);
    }
    this.previousState = this._state();
    this._state.set(tempState);

    this.actionTimeout = setTimeout(() => {
      this._state.set(this.previousState);
      this.actionTimeout = null;
    }, durationMs);
  }
}
