import { Injectable, isDevMode, inject } from '@angular/core';
import { firstValueFrom } from 'rxjs';
import { TourEngineService } from '../core/assistant/services/tour-engine.service';
import { SpeechService } from '../core/assistant/services/speech.service';

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

/**
 * TTS facade for the voice agent panel.
 *
 * Primary engine: Azure neural TTS via SpeechService — the SAME engine the
 * guided tour uses, pinned to the native Egyptian voice (ar-EG-ShakirNeural).
 * Because both features share one engine with a generation counter, two voices
 * can never play at the same time.
 *
 * Fallback: browser Web Speech API, only when Azure synthesis fails
 * (e.g. token endpoint unreachable) and no tour is active.
 */
@Injectable({ providedIn: 'root' })
export class VoiceSynthesisService {
  /** Azure is the primary engine so synthesis is always "supported". */
  readonly isSupported: boolean = true;

  private readonly tourEngine = inject(TourEngineService, { optional: true });
  private readonly speechService = inject(SpeechService);

  private get browserTtsAvailable(): boolean {
    return typeof window !== 'undefined' && 'speechSynthesis' in window;
  }

  /**
   * Speaks the text and resolves when playback is done. The TTS locale is
   * inferred from the text itself (Arabic Unicode → ar-EG, otherwise → en-US).
   *
   * Never rejects — synthesis errors resolve silently so the caller can continue.
   */
  async speak(raw: string): Promise<void> {
    // Yield audio ownership to the guided tour when it is active.
    if (this.tourEngine?.isPlaying()) return;

    const text = cleanForSpeech(raw);
    if (!text) return;

    const lang = ARABIC_RE.test(text) ? 'ar-EG' : 'en-US';

    try {
      // Resolves when playback ACTUALLY finishes (SpeakerAudioDestination.onAudioEnd),
      // or immediately if a newer speak()/tour superseded this one.
      await firstValueFrom(this.speechService.speak(text, lang), { defaultValue: undefined });
      return;
    } catch (err) {
      if (isDevMode()) console.debug('[VoiceSynthesis] Azure TTS failed, falling back to browser TTS:', err);
    }

    // Fallback — never while a tour is speaking.
    if (this.tourEngine?.isPlaying()) return;
    await this.browserSpeak(text, lang);
  }

  stop(): void {
    // Never kill the shared Azure pipeline while the tour owns it —
    // that would silence the tour's own narration.
    if (!this.tourEngine?.isPlaying()) {
      this.speechService.stopSpeaking();
    }
    if (this.browserTtsAvailable) {
      window.speechSynthesis.pause();
      window.speechSynthesis.cancel();
    }
  }

  private browserSpeak(text: string, lang: string): Promise<void> {
    return new Promise<void>(resolve => {
      if (!this.browserTtsAvailable) { resolve(); return; }

      window.speechSynthesis.pause();
      window.speechSynthesis.cancel();

      const utterance     = new SpeechSynthesisUtterance(text);
      utterance.lang      = lang;
      utterance.rate      = 1;
      utterance.pitch     = 1;
      utterance.volume    = 1;
      utterance.onend     = () => resolve();
      utterance.onerror   = (e) => {
        if (isDevMode()) console.debug('[VoiceSynthesis] browser TTS error:', e.error);
        resolve();
      };

      window.speechSynthesis.resume();
      window.speechSynthesis.speak(utterance);
    });
  }
}
