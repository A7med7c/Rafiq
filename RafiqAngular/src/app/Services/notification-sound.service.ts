import { Injectable, isDevMode } from '@angular/core';

@Injectable({ providedIn: 'root' })
export class NotificationSoundService {
  // public/sounds/ → served at the root with no prefix (angular.json maps public → /)
  private static readonly soundUrl = 'sounds/medicine-reminder.mp3';

  private audio: HTMLAudioElement | null = null;
  private unlocked = false;
  private playPending = false;

  constructor() {
    if (typeof Audio === 'undefined' || typeof document === 'undefined') {
      return;
    }

    this.audio = new Audio(NotificationSoundService.soundUrl);
    this.audio.preload = 'auto';
    this.audio.volume = 1;

    // Browsers block audio.play() until the user has interacted with the page.
    // Register passive listeners so the first gesture (click, key, touch, or
    // pointer) silently unlocks the audio element.  After that, every future
    // play() call succeeds even when triggered programmatically from SignalR.
    this.registerUnlockListeners();
  }

  play(): void {
    if (!this.audio) {
      return;
    }

    if (!this.unlocked) {
      // Audio is not yet unlocked; flag that playback was requested so we
      // can play as soon as the first user gesture unlocks the element.
      this.playPending = true;
      return;
    }

    this.audio.currentTime = 0;

    this.audio.play().catch(err => {
      if (isDevMode()) {
        console.debug('[NotificationSoundService] Playback blocked or failed:', err);
      }
    });
  }

  private registerUnlockListeners(): void {
    const eventTypes = ['click', 'keydown', 'touchstart', 'pointerdown'] as const;

    const unlock = () => {
      // Remove all listeners immediately so they don't fire again.
      eventTypes.forEach(type => document.removeEventListener(type, unlock));

      if (this.unlocked || !this.audio) {
        return;
      }

      // A muted play/pause satisfies the autoplay policy.  After this
      // promise resolves the browser considers the element "user-activated"
      // and subsequent play() calls succeed without a gesture.
      const savedVolume = this.audio.volume;
      this.audio.volume = 0;

      this.audio
        .play()
        .then(() => {
          this.audio!.pause();
          this.audio!.currentTime = 0;
          this.audio!.volume = savedVolume;
          this.unlocked = true;

          // If a reminder arrived before the first gesture, play it now.
          if (this.playPending) {
            this.playPending = false;
            this.play();
          }
        })
        .catch(() => {
          this.audio!.volume = savedVolume;
        });
    };

    eventTypes.forEach(type =>
      document.addEventListener(type, unlock, { passive: true })
    );
  }
}
