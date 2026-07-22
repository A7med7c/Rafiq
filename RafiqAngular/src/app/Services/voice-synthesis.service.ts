import { Injectable, isDevMode } from '@angular/core';

/** Arabic Unicode blocks: Arabic, Arabic Supplement, Arabic Presentation Forms-A and -B */
const ARABIC_RE = /[؀-ۿݐ-ݿﭐ-﷿ﹰ-﻿]/;

/** Wraps the Web Speech Synthesis API (TTS). */
@Injectable({ providedIn: 'root' })
export class VoiceSynthesisService {
  readonly isSupported: boolean = 'speechSynthesis' in window;

  /**
   * Speaks the text and resolves when done. The TTS locale is inferred from
   * the text itself (Arabic Unicode → ar-EG, otherwise → en-US), so the
   * voice always matches the AI's actual response language regardless of the
   * app's UI language.
   *
   * Never rejects — synthesis errors resolve silently so the caller can continue.
   */
  speak(text: string): Promise<void> {
    return new Promise<void>(resolve => {
      if (!this.isSupported || !text.trim()) { resolve(); return; }

      window.speechSynthesis.cancel();

      const utterance     = new SpeechSynthesisUtterance(text);
      utterance.lang      = ARABIC_RE.test(text) ? 'ar-EG' : 'en-US';
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
