import { Injectable, isDevMode } from '@angular/core';

/**
 * Wraps the Web Speech Recognition API (STT).
 *
 * Uses continuous mode + a manual silence timer so the mic doesn't cut off
 * after a brief pause between words. The user has SILENCE_MS of quiet time
 * before the capture finalizes.
 */
@Injectable({ providedIn: 'root' })
export class VoiceCaptureService {
  readonly isSupported: boolean;
  private recognition: any = null;
  private silenceTimer: ReturnType<typeof setTimeout> | null = null;

  /** ms of silence after the last word before we finalize the transcript */
  private readonly SILENCE_MS = 2000;

  constructor() {
    this.isSupported = !!(
      (window as any).SpeechRecognition || (window as any).webkitSpeechRecognition
    );
  }

  /**
   * Starts continuous listening and resolves with the full transcript once the
   * user has been silent for SILENCE_MS milliseconds.
   *
   * Rejects with an Error whose `.message` is the SpeechRecognitionErrorCode:
   *   'not-allowed' — mic permission denied
   *   'no-speech'   — browser timed out without any speech
   *   'aborted'     — stop() was called before any speech arrived
   */
  captureOnce(lang: string): Promise<string> {
    return new Promise<string>((resolve, reject) => {
      const SpeechRecognition =
        (window as any).SpeechRecognition || (window as any).webkitSpeechRecognition;

      if (!SpeechRecognition) {
        reject(new Error('not-supported'));
        return;
      }

      this.stop(); // abort any prior session

      const rec = new SpeechRecognition();
      this.recognition = rec;

      rec.lang            = lang === 'ar' ? 'ar-EG' : 'en-US';
      rec.continuous      = true;   // keep mic open across pauses
      rec.interimResults  = true;   // fire onresult on partial words too
      rec.maxAlternatives = 1;

      let settled       = false;
      let finalText     = '';
      let interimText   = '';

      const finish = (transcript: string) => {
        if (settled) return;
        settled = true;
        this.clearSilenceTimer();
        try { rec.stop(); } catch { /* already stopped */ }
        this.recognition = null;
        resolve(transcript.trim());
      };

      const resetSilenceTimer = () => {
        this.clearSilenceTimer();
        this.silenceTimer = setTimeout(() => {
          // Combine whatever we have — prefer finalized text
          finish(finalText || interimText);
        }, this.SILENCE_MS);
      };

      rec.onresult = (event: any) => {
        interimText = '';
        for (let i = event.resultIndex; i < event.results.length; i++) {
          const result = event.results[i];
          if (result.isFinal) {
            finalText   += result[0].transcript;
            interimText  = '';
          } else {
            interimText += result[0].transcript;
          }
        }

        if (isDevMode()) {
          console.debug('[VoiceCapture] interim:', interimText, '| final:', finalText);
        }

        // Reset the silence countdown whenever speech is detected.
        resetSilenceTimer();
      };

      rec.onspeechstart = () => {
        // Cancel any early-timeout that may have been set before speech began.
        this.clearSilenceTimer();
      };

      rec.onspeechend = () => {
        // Speech paused — start counting down to finalize.
        resetSilenceTimer();
      };

      rec.onerror = (event: any) => {
        if (settled) return;
        settled = true;
        this.clearSilenceTimer();
        this.recognition = null;
        if (isDevMode()) console.debug('[VoiceCapture] error:', event.error);

        const code: string = event.error ?? 'unknown';
        if (code === 'no-speech' && (finalText || interimText)) {
          // Some browsers fire no-speech even after partial results — use what we have.
          resolve((finalText || interimText).trim());
        } else {
          reject(new Error(code));
        }
      };

      rec.onend = () => {
        // Browser closed the session (e.g. max duration hit).
        // If we still have text, resolve with it; otherwise let onerror handle it.
        if (!settled) {
          finish(finalText || interimText);
        }
      };

      try {
        rec.start();
      } catch (err) {
        settled = true;
        this.recognition = null;
        reject(new Error('not-allowed'));
      }
    });
  }

  /** Abort the current capture session immediately. */
  stop(): void {
    this.clearSilenceTimer();
    if (this.recognition) {
      try { this.recognition.abort(); } catch { /* ignore */ }
      this.recognition = null;
    }
  }

  private clearSilenceTimer(): void {
    if (this.silenceTimer !== null) {
      clearTimeout(this.silenceTimer);
      this.silenceTimer = null;
    }
  }
}
