import { Injectable, isDevMode } from '@angular/core';

@Injectable({ providedIn: 'root' })
export class NotificationSoundService {
  private static readonly soundUrl = 'assets/sounds/medicine-reminder.mp3';

  private audio: HTMLAudioElement | null = null;

  constructor() {
    if (typeof Audio === 'undefined') {
      return;
    }

    this.audio = new Audio(NotificationSoundService.soundUrl);
    this.audio.preload = 'auto';
  }

  play(): void {
    if (!this.audio) {
      return;
    }

    this.audio.currentTime = 0;

    this.audio.play().catch(err => {
      if (isDevMode()) {
        console.debug('[NotificationSoundService] Playback blocked or failed:', err);
      }
    });
  }
}
