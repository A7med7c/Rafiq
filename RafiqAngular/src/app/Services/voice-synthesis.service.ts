import { Injectable, isDevMode } from '@angular/core';

/** Wraps the Web Speech Synthesis API (TTS). */
@Injectable({ providedIn: 'root' })
export class VoiceSynthesisService {
  readonly isSupported: boolean = 'speechSynthesis' in window;

  /**
   * Speaks the text and resolves when done.
   * Never rejects — synthesis errors resolve silently so the caller can continue.
   */
  speak(text: string, lang: string): Promise<void> {
    return new Promise<void>(resolve => {
      if (!this.isSupported || !text.trim()) { resolve(); return; }

      window.speechSynthesis.cancel();

      const utterance     = new SpeechSynthesisUtterance(text);
      utterance.lang      = lang === 'ar' ? 'ar-EG' : 'en-US';
      utterance.rate      = 1;
      utterance.pitch     = 1;
      utterance.volume    = 1;
      utterance.onend     = () => resolve();
      utterance.onerror   = (e) => {
        if (isDevMode()) console.debug('[VoiceSynthesis] error:', e.error);
        resolve();
      };

      window.speechSynthesis.speak(utterance);
    });
  }

  stop(): void {
    if (this.isSupported) window.speechSynthesis.cancel();
  }
}
