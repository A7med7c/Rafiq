import { Injectable, isDevMode } from '@angular/core';

/** Arabic Unicode blocks: Arabic, Arabic Supplement, Arabic Presentation Forms-A and -B */
const ARABIC_RE = /[؀-ۿݐ-ݿﭐ-﷿ﹰ-﻿]/;

/** Strip markdown and emoji so TTS doesn't read symbol names aloud. */
function cleanForSpeech(raw: string): string {
  return raw
    // Emoji (Extended_Pictographic covers ⭐, 🌟, ✨, all faces, etc.)
    .replace(/\p{Extended_Pictographic}/gu, '')
    // Markdown bold/italic: ***x*** / **x** / *x* → x
    .replace(/\*{1,3}([^*\n]+)\*{1,3}/g, '$1')
    // Remaining lone asterisks used as bullets
    .replace(/\*/g, '')
    // Markdown headers: ## Title → Title
    .replace(/^#{1,6}\s*/gm, '')
    // List dashes/bullets at line start
    .replace(/^\s*[-•]\s*/gm, '')
    // Backticks (inline code)
    .replace(/`+/g, '')
    // Collapse line breaks into a pause
    .replace(/\n+/g, ' ')
    // Collapse extra spaces
    .replace(/\s{2,}/g, ' ')
    .trim();
}

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
  speak(raw: string): Promise<void> {
    return new Promise<void>(resolve => {
      const text = cleanForSpeech(raw);
      if (!this.isSupported || !text) { resolve(); return; }

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
