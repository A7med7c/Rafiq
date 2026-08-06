/**
 * @file speech.service.ts
 * @description Azure Speech Service wrapper handling STT (Speech-to-Text) and TTS (Text-to-Speech).
 * Responsibility: Securely fetch temporary Azure Speech tokens from backend endpoint POST /api/speech/token
 * and manage SpeechRecognizer / SpeechSynthesizer sessions.
 */

import { Injectable, inject, signal } from '@angular/core';
import { HttpClient } from '@angular/common/http';
import { Observable, from, throwError, EMPTY, of } from 'rxjs';
import { switchMap, catchError, tap } from 'rxjs/operators';
// @ts-ignore
import * as SpeechSDKImport from 'microsoft-cognitiveservices-speech-sdk';

import { environment } from '../../../Environments/Environment';
console.log('SpeechSDKImport =', SpeechSDKImport);
export interface SpeechTokenResponse {
  token: string;
  region: string;
}

const TAG = '[SpeechService]';

@Injectable({
  providedIn: 'root',
})
export class SpeechService {
  private readonly http = inject(HttpClient);
  readonly isMuted = signal<boolean>(false);

  toggleMute(): boolean {
    const nextState = !this.isMuted();
    this.isMuted.set(nextState);
    if (nextState) {
      this.stopSpeaking();
    }
    return nextState;
  }
  private currentRecognizer: any = null;
  private currentSynthesizer: any = null;

  /** The SpeakerAudioDestination currently playing audio. Unlike synthesizer.close(),
   *  player.pause() stops audible playback IMMEDIATELY. */
  private currentPlayer: any = null;

  /** HTMLAudioElement playing ElevenLabs MP3 audio (when ElevenLabs is configured). */
  private currentHtmlAudio: HTMLAudioElement | null = null;

  /** null = untried, true = working, false = not configured (skip for this session). */
  private _elevenLabsAvailable: boolean | null = null;

  /** Resolves the in-flight speak() observable when playback is force-stopped,
   *  so awaiting callers never hang on a promise that will never settle. */
  private notifyPlaybackStopped: (() => void) | null = null;

  // Monotonically increasing counter. Each speak() call captures the value at
  // the moment it starts. Any async step that sees a mismatch knows a newer
  // call has superseded it and bails out with EMPTY.
  private _speechGen = 0;

  // ── Token cache (Azure speech tokens are valid for 10 minutes) ──────────
  private _tokenCache: { token: string; region: string; fetchedAt: number } | null = null;
  private static readonly TOKEN_TTL_MS = 8 * 60 * 1000;

  /** Fixed neural voices per language — ar-EG voices are native Egyptian Arabic. */
  private static readonly VOICE_BY_LANGUAGE: Record<string, string> = {
    'ar-EG': 'ar-EG-SalmaNeural',    // Female Egyptian voice — noticeably more natural than ar-EG-ShakirNeural
    'en-US': 'en-US-JennyNeural',
  };

  /**
   * Slightly slower delivery makes colloquial Egyptian Arabic markedly clearer —
   * the default rate runs words together. Tune per language if needed.
   */
  private static readonly RATE_BY_LANGUAGE: Record<string, string> = {
    'ar-EG': '+10%',
    'en-US': '+5%',
  };

  // ── SDK Import Resolution Helper ─────────────────────────────────────────

  /**
   * Robustly resolves Azure Speech SDK components across esbuild / Webpack / CommonJS bundle targets.
   */
  private getSDK(): {
    SpeechConfig: any;
    AudioConfig: any;
    SpeechRecognizer: any;
    SpeechSynthesizer: any;
    SpeakerAudioDestination: any;
    CancellationDetails: any;
    ResultReason: any;
    CancellationReason: any;
    rawSDK: any;
  } {
    const rawImport: any = SpeechSDKImport;

    // 1. Resolve target SDK object across browser window / globalThis / module exports
    let targetSDK: any = null;

    if (typeof window !== 'undefined') {
      const win = window as any;
      if (win.SpeechSDK && win.SpeechSDK.SpeechConfig) targetSDK = win.SpeechSDK;
      else if (win.SDK && win.SDK.SpeechConfig) targetSDK = win.SDK;
    }

    if (!targetSDK && typeof globalThis !== 'undefined') {
      const g = globalThis as any;
      if (g.SpeechSDK && g.SpeechSDK.SpeechConfig) targetSDK = g.SpeechSDK;
    }

    if (!targetSDK) {
      if (rawImport?.SpeechConfig) targetSDK = rawImport;
      else if (rawImport?.default?.SpeechConfig) targetSDK = rawImport.default;
      else targetSDK = rawImport?.default || rawImport;
    }

    // Print the imported module object to the console
    console.log('[SpeechSDK]', targetSDK);

    const SpeechConfig = targetSDK?.SpeechConfig || rawImport?.SpeechConfig || rawImport?.default?.SpeechConfig;
    const AudioConfig = targetSDK?.AudioConfig || rawImport?.AudioConfig || rawImport?.default?.AudioConfig;
    const SpeechRecognizer = targetSDK?.SpeechRecognizer || rawImport?.SpeechRecognizer || rawImport?.default?.SpeechRecognizer;
    const SpeechSynthesizer = targetSDK?.SpeechSynthesizer || rawImport?.SpeechSynthesizer || rawImport?.default?.SpeechSynthesizer;
    const SpeakerAudioDestination = targetSDK?.SpeakerAudioDestination || rawImport?.SpeakerAudioDestination || rawImport?.default?.SpeakerAudioDestination;
    const CancellationDetails = targetSDK?.CancellationDetails || rawImport?.CancellationDetails || rawImport?.default?.CancellationDetails;
    const ResultReason = targetSDK?.ResultReason || rawImport?.ResultReason || rawImport?.default?.ResultReason;
    const CancellationReason = targetSDK?.CancellationReason || rawImport?.CancellationReason || rawImport?.default?.CancellationReason;

    return {
      SpeechConfig,
      AudioConfig,
      SpeechRecognizer,
      SpeechSynthesizer,
      SpeakerAudioDestination,
      CancellationDetails,
      ResultReason,
      CancellationReason,
      rawSDK: targetSDK,
    };
  }

  // ── SSML Builder ─────────────────────────────────────────────────────────

  /** Escapes the five XML predefined entities so user/scenario text is SSML-safe. */
  private escapeXml(text: string): string {
    return text
      .replace(/&/g, '&amp;')
      .replace(/</g, '&lt;')
      .replace(/>/g, '&gt;')
      .replace(/"/g, '&quot;')
      .replace(/'/g, '&apos;');
  }

  /**
   * Wraps plain text in SSML with prosody + sentence-boundary pauses.
   *
   * Two things dramatically improve Egyptian-Arabic delivery:
   *  1. A slightly slower `rate` so colloquial words don't slur together.
   *  2. Explicit `<break>` pauses after sentence terminators (. ! ? … ، ؛ :)
   *     so the voice breathes between clauses instead of racing through them.
   */
  private buildSsml(text: string, language: string, voiceName: string): string {
    const rate = SpeechService.RATE_BY_LANGUAGE[language] ?? '0%';

    // Insert a short pause after each sentence/clause terminator. The marker is
    // added on the raw text, then each segment is XML-escaped individually.
    const withBreaks = this.escapeXml(text)
      // Strong terminators → short breath
      .replace(/([.!?…]+)\s+/g, '$1<break time="180ms"/> ')
      // Arabic comma / semicolon / colon → brief breath
      .replace(/([،؛:])\s+/g, '$1<break time="100ms"/> ');

    return (
      `<speak version="1.0" xmlns="http://www.w3.org/2001/10/synthesis" ` +
      `xmlns:mstts="https://www.w3.org/2001/mstts" xml:lang="${language}">` +
      `<voice name="${voiceName}">` +
      `<prosody rate="${rate}">${withBreaks}</prosody>` +
      `</voice></speak>`
    );
  }

  // ── Token Fetch ──────────────────────────────────────────────────────────

  /**
   * Fetches a fresh Azure Speech token + region from backend REST API endpoint.
   */
  private getSpeechToken(): Observable<SpeechTokenResponse> {
    // Serve from cache while the token is still valid — avoids a full HTTP
    // round-trip before every tour step, which caused audible gaps.
    const cached = this._tokenCache;
    if (cached && Date.now() - cached.fetchedAt < SpeechService.TOKEN_TTL_MS) {
      return of({ token: cached.token, region: cached.region });
    }

    console.log(`${TAG} [2] Requesting speech token from ${environment.apiUrl}/speech/token …`);

    return this.http.post<SpeechTokenResponse>(`${environment.apiUrl}/speech/token`, {}).pipe(
      tap(res => {
        this._tokenCache = { token: res.token, region: res.region, fetchedAt: Date.now() };
      }),
      catchError(error => {
        console.error(`${TAG} [2✗] Token request FAILED.`, {
          status: error.status,
          statusText: error.statusText,
          body: error.error,
          message: error.message,
        });
        return throwError(
          () =>
            new Error(
              `Failed to obtain Azure Speech token: ${error.error?.message || error.message || 'Unknown network error'}`
            )
        );
      })
    );
  }

  // ── Microphone Permission Pre-check ──────────────────────────────────────

  /**
   * Requests microphone access via getUserMedia before handing off to Azure SDK.
   * Releases the stream immediately; only used to verify browser permissions.
   */
  private checkMicrophonePermission(): Observable<void> {
    return from(
      (async () => {
        if (!navigator?.mediaDevices?.getUserMedia) {
          throw new Error('navigator.mediaDevices.getUserMedia is not available in this environment.');
        }

        console.log(`${TAG} [mic-check] Calling navigator.mediaDevices.getUserMedia({ audio: true }) …`);
        let stream: MediaStream;
        try {
          stream = await navigator.mediaDevices.getUserMedia({ audio: true });
        } catch (err: any) {
          const name = err?.name || 'Unknown';
          const message = err?.message || String(err);

          if (name === 'NotAllowedError' || name === 'PermissionDeniedError') {
            console.error(
              `${TAG} [mic-check ✗] MICROPHONE PERMISSION DENIED.`,
              `Browser error: ${name} — ${message}`,
              '\nAsk the user to allow microphone access in the browser address-bar lock icon.'
            );
          } else if (name === 'NotFoundError' || name === 'DevicesNotFoundError') {
            console.error(
              `${TAG} [mic-check ✗] NO MICROPHONE FOUND.`,
              `Browser error: ${name} — ${message}`
            );
          } else {
            console.error(
              `${TAG} [mic-check ✗] getUserMedia failed.`,
              `Error: ${name} — ${message}`
            );
          }
          throw err;
        }

        // Release the test stream — we only needed the permission check
        stream.getTracks().forEach(t => t.stop());
        console.log(`${TAG} [mic-check ✓] Microphone permission granted. Stream released.`);
      })()
    );
  }

  // ── Speech-to-Text ───────────────────────────────────────────────────────

  /**
   * Recognizes speech from default microphone input and returns an Observable emitting recognized text.
   * Automatically verifies mic permission and requests a fresh Azure token prior to starting recognition.
   */
  startListening(language: string = 'ar-EG'): Observable<string> {
    this.stopListening();

    return this.checkMicrophonePermission().pipe(
      switchMap(() => this.getSpeechToken()),
      switchMap(tokenData => {
        console.log(`${TAG} [3] Token received. region="${tokenData.region}", token length=${tokenData.token?.length}`);

        return new Observable<string>(subscriber => {
          let recognizer: any = null;

          try {
            const SDK = this.getSDK();

            // Print the value of SpeechSDK to console as requested
            console.log(`${TAG} [SpeechSDK Object Print]`, SDK.rawSDK);
            console.log(`${TAG} [SpeechSDK Component Status]`, {
              SpeechConfig: !!SDK.SpeechConfig,
              AudioConfig: !!SDK.AudioConfig,
              SpeechRecognizer: !!SDK.SpeechRecognizer,
            });

            if (!SDK.SpeechConfig || !SDK.AudioConfig || !SDK.SpeechRecognizer) {
              const sdkErr = new Error(
                `Azure Speech SDK components are undefined! SpeechConfig: ${!!SDK.SpeechConfig}, AudioConfig: ${!!SDK.AudioConfig}, SpeechRecognizer: ${!!SDK.SpeechRecognizer}`
              );
              console.error(`${TAG} [4✗] SDK verification failed.`, sdkErr);
              throw sdkErr;
            }

            // Step 4: Create SpeechConfig
            console.log(`${TAG} [4] Creating SpeechConfig. language="${language}"`);
            const speechConfig = SDK.SpeechConfig.fromAuthorizationToken(tokenData.token, tokenData.region);
            speechConfig.speechRecognitionLanguage = language;

            // Step 5: Create AudioConfig & SpeechRecognizer
            console.log(`${TAG} [5] Creating SpeechRecognizer …`);
            const audioConfig = SDK.AudioConfig.fromDefaultMicrophoneInput();
            recognizer = new SDK.SpeechRecognizer(speechConfig, audioConfig);
            this.currentRecognizer = recognizer;
            console.log(`${TAG} [6] SpeechRecognizer created successfully.`, recognizer);

            // ── Event hooks ──────────────────────────────────────────────

            // Step 8: Session started
            recognizer.sessionStarted = (_s: any, e: any) => {
              console.log(`${TAG} [8] sessionStarted. SessionId="${e?.sessionId}"`);
            };

            // Step 9: Speech start detected
            recognizer.speechStartDetected = (_s: any, e: any) => {
              console.log(`${TAG} [9] speechStartDetected. Offset=${e?.offset}`);
            };

            // Step 10: Partial hypothesis (recognizing)
            recognizer.recognizing = (_s: any, e: any) => {
              console.log(`${TAG} [10] recognizing… text="${e?.result?.text}"`);
            };

            // Step 11: Final hypothesis (recognized)
            recognizer.recognized = (_s: any, e: any) => {
              const reason = e?.result?.reason;
              const text = e?.result?.text;
              const reasonLabel = this.reasonLabel(SDK, reason);
              console.log(`${TAG} [11] recognized. reason=${reasonLabel} (${reason}), text="${text}"`);
            };

            // Step 12: Speech end detected
            recognizer.speechEndDetected = (_s: any, e: any) => {
              console.log(`${TAG} [12] speechEndDetected. Offset=${e?.offset}`);
            };

            // Step 13: Session stopped
            recognizer.sessionStopped = (_s: any, e: any) => {
              console.log(`${TAG} [13] sessionStopped. SessionId="${e?.sessionId}"`);
            };

            // Step 14: Canceled with full details
            recognizer.canceled = (_s: any, e: any) => {
              const reason = e?.reason;
              const errorCode = e?.errorCode;
              const errorDetails = e?.errorDetails;
              console.error(
                `${TAG} [14] canceled!`,
                `\n  reason       : ${reason}`,
                `\n  errorCode    : ${errorCode}`,
                `\n  errorDetails : ${errorDetails}`
              );

              if (reason === SDK.CancellationReason?.Error || reason === 0) {
                subscriber.error(new Error(`Speech recognition canceled: ${errorDetails || errorCode || reason}`));
                this.cleanupRecognizer(recognizer);
              }
            };

            // Step 7: Start recognition
            console.log(`${TAG} [7] Calling recognizeOnceAsync() …`);
            recognizer.recognizeOnceAsync(
              (result: any) => {
                const reason = result?.reason;
                const text = result?.text;
                console.log(
                  `${TAG} [11-final] recognizeOnceAsync result. reason=${this.reasonLabel(SDK, reason)} (${reason}), text="${text}"`
                );

                if (reason === SDK.ResultReason.RecognizedSpeech) {
                  console.log(`${TAG} [✓] Recognition SUCCESS. Emitting: "${text}"`);
                  subscriber.next(text);
                  subscriber.complete();
                } else if (reason === SDK.ResultReason.NoMatch) {
                  console.warn(`${TAG} [✗] NoMatch — nothing recognized.`);
                  subscriber.error(new Error('No speech could be recognized.'));
                } else if (reason === SDK.ResultReason.Canceled) {
                  const cancellation = SDK.CancellationDetails.fromResult(result);
                  console.error(
                    `${TAG} [14-result] Canceled from result.`,
                    `\n  reason       : ${cancellation.reason}`,
                    `\n  errorCode    : ${cancellation.errorCode}`,
                    `\n  errorDetails : ${cancellation.errorDetails}`
                  );
                  subscriber.error(
                    new Error(`Speech recognition canceled: ${cancellation.errorDetails || cancellation.reason}`)
                  );
                } else {
                  console.warn(`${TAG} [?] Unexpected result reason: ${reason}`);
                  subscriber.error(new Error(`Speech recognition completed with reason: ${reason}`));
                }

                this.cleanupRecognizer(recognizer);
              },
              (error: any) => {
                const errorMsg = typeof error === 'string' ? error : (error?.message || 'Unknown recognition error');
                console.error(`${TAG} [15✗] recognizeOnceAsync ERROR callback.`, { error, errorMsg });
                subscriber.error(new Error(errorMsg));
                this.cleanupRecognizer(recognizer);
              }
            );

          } catch (err: any) {
            console.error(`${TAG} [15✗] Synchronous exception thrown during recognizer setup.`, err);
            subscriber.error(err instanceof Error ? err : new Error(String(err)));
            if (recognizer) {
              this.cleanupRecognizer(recognizer);
            }
          }

          return () => {
            this.stopListening();
          };
        });
      }),
      catchError(err => {
        console.error(`${TAG} [pipeline✗] Fatal error in startListening pipeline.`, err);
        return throwError(() => err);
      })
    );
  }

  /**
   * Cancels any active recognition session and disposes recognizer resources.
   */
  stopListening(): void {
    if (this.currentRecognizer) {
      console.log(`${TAG} [stop] stopListening() — closing active recognizer.`);
      const recognizer = this.currentRecognizer;
      this.currentRecognizer = null;
      try {
        recognizer.close();
      } catch {
        // Suppress close errors
      }
    }
  }

  // ── Text-to-Speech ───────────────────────────────────────────────────────

  /**
   * Synthesizes text to speech and plays it through the speakers.
   *
   * Engine priority:
   *  1. ElevenLabs (via backend proxy) — highest Arabic quality, used when configured.
   *  2. Azure neural TTS (ar-EG-SalmaNeural via SSML) — automatic fallback.
   *
   * A generation counter guarantees only ONE synthesis is ever audible at a time,
   * regardless of which engine produced it.
   */
  speak(text: string, language: string = 'ar-EG'): Observable<void> {
    if (this.isMuted()) {
      this.stopSpeaking();
      return of(undefined);
    }

    // Kill any in-progress synthesizer immediately and claim a new generation.
    // Any async step still in flight from a previous speak() call will see
    // myGen !== this._speechGen and short-circuit with EMPTY, so there is
    // never more than one synthesizer active at a time.
    this.stopSpeaking();
    const myGen = ++this._speechGen;

    console.log(`${TAG} [tts-1] speak() gen=${myGen} language="${language}" text.length=${text?.length}`);

    return this.speakWithElevenLabs(text, myGen).pipe(
      catchError(err => {
        if (myGen !== this._speechGen) return EMPTY;
        console.warn(`${TAG} [tts-11l✗] ElevenLabs request failed, falling back to Azure TTS:`, err);
        return this.speakWithAzure(text, language, myGen);
      })
    );
  }

  /**
   * Plays TTS audio fetched from the backend ElevenLabs proxy endpoint.
   * Completion fires when PLAYBACK ends (HTMLAudioElement 'ended' event).
   */
  private speakWithElevenLabs(text: string, myGen: number): Observable<void> {
    return this.http.post(`${environment.apiUrl}/speech/elevenlabs-tts`, { text }, { responseType: 'blob' }).pipe(
      switchMap(blob => {
        this._elevenLabsAvailable = true;

        if (myGen !== this._speechGen) {
          console.log(`${TAG} [tts-11l-skip] gen=${myGen} superseded by gen=${this._speechGen}, discarding audio.`);
          return EMPTY;
        }

        return new Observable<void>(subscriber => {
          const url = URL.createObjectURL(blob);
          const audio = new Audio(url);
          audio.playbackRate = 1.15;
          this.currentHtmlAudio = audio;
          let isCancelled = false;

          // If stopSpeaking() force-kills playback, settle this observable so
          // awaiting callers (voice panel) don't hang forever.
          this.notifyPlaybackStopped = () => {
            if (isCancelled) return;
            subscriber.next();
            subscriber.complete();
          };

          audio.onended = () => {
            if (isCancelled) return;
            if (myGen === this._speechGen) {
              this.notifyPlaybackStopped = null;
            }
            console.log(`${TAG} [tts-11l-end] gen=${myGen} ElevenLabs playback finished.`);
            URL.revokeObjectURL(url);
            subscriber.next();
            subscriber.complete();
          };

          audio.onerror = () => {
            if (isCancelled) return;
            URL.revokeObjectURL(url);
            subscriber.error(new Error('ElevenLabs audio playback failed.'));
          };

          audio.play().catch(err => {
            if (isCancelled) return;

            const isAutoplayBlocked = err?.name === 'NotAllowedError' || String(err).includes('interact');
            if (isAutoplayBlocked) {
              console.warn(`${TAG} [tts-11l] Autoplay blocked by browser policy. Waiting for user interaction to play ElevenLabs audio.`);

              const resumeAudio = () => {
                window.removeEventListener('click', resumeAudio);
                window.removeEventListener('keydown', resumeAudio);
                window.removeEventListener('touchstart', resumeAudio);
                if (!isCancelled && this.currentHtmlAudio === audio) {
                  audio.play().catch(e => {
                    if (!isCancelled) subscriber.error(e instanceof Error ? e : new Error(String(e)));
                  });
                }
              };

              window.addEventListener('click', resumeAudio, { once: true });
              window.addEventListener('keydown', resumeAudio, { once: true });
              window.addEventListener('touchstart', resumeAudio, { once: true });
              return;
            }

            subscriber.error(err instanceof Error ? err : new Error(String(err)));
          });

          return () => {
            isCancelled = true;
            if (this.currentHtmlAudio === audio) {
              this.currentHtmlAudio = null;
            }
            try { audio.pause(); } catch { /* suppress */ }
            URL.revokeObjectURL(url);
          };
        });
      })
    );
  }

  /**
   * Azure neural TTS path (SSML + SpeakerAudioDestination).
   * Requests a (cached) Azure token prior to playback.
   */
  private speakWithAzure(text: string, language: string, myGen: number): Observable<void> {
    return this.getSpeechToken().pipe(
      switchMap(tokenData => {
        // If a newer speak() call arrived while we were fetching the token, abort.
        if (myGen !== this._speechGen) {
          console.log(`${TAG} [tts-skip] gen=${myGen} superseded by gen=${this._speechGen}, aborting.`);
          return EMPTY;
        }

        console.log(`${TAG} [tts-3] TTS token received. region="${tokenData.region}"`);

        return new Observable<void>(subscriber => {
          let synthesizer: any = null;
          let player: any = null;
          // Prevents stale SDK callbacks from emitting after the subscription
          // has been torn down (e.g. tour advancing to next step).
          let isCancelled = false;

          try {
            const SDK = this.getSDK();

            if (!SDK.SpeechConfig || !SDK.AudioConfig || !SDK.SpeechSynthesizer) {
              throw new Error(
                `Azure Speech SDK components for TTS are undefined! SpeechConfig: ${!!SDK.SpeechConfig}, AudioConfig: ${!!SDK.AudioConfig}, SpeechSynthesizer: ${!!SDK.SpeechSynthesizer}`
              );
            }

            const speechConfig = SDK.SpeechConfig.fromAuthorizationToken(tokenData.token, tokenData.region);
            speechConfig.speechSynthesisLanguage = language;

            // Pin a specific neural voice so Arabic is always native Egyptian
            // (ar-EG-ShakirNeural) rather than whatever regional default applies.
            const voiceName = SpeechService.VOICE_BY_LANGUAGE[language];
            if (voiceName) {
              speechConfig.speechSynthesisVoiceName = voiceName;
            }

            // MP3 streams play most reliably through SpeakerAudioDestination
            // (Media Source Extensions) on Chrome/Edge/Safari.
            const outputFormat = SDK.rawSDK?.SpeechSynthesisOutputFormat?.Audio24Khz48KBitRateMonoMp3;
            if (outputFormat !== undefined) {
              speechConfig.speechSynthesisOutputFormat = outputFormat;
            }

            // Guard a second time: a newer speak() call may have arrived while
            // the SDK was initialising (between the switchMap check and here).
            if (myGen !== this._speechGen) {
              console.log(`${TAG} [tts-skip2] gen=${myGen} superseded by gen=${this._speechGen} before synthesizer creation.`);
              subscriber.complete();
              return;
            }

            // Route audio through a SpeakerAudioDestination player instead of
            // fromDefaultSpeakerOutput(). This is the crucial difference:
            //  1. player.onAudioEnd fires when PLAYBACK actually finishes.
            //     (speakTextAsync's callback fires when the audio DATA finishes
            //     downloading — often many seconds before playback ends, which
            //     made the tour advance early and overlap two voices.)
            //  2. player.pause() stops audible output IMMEDIATELY, which
            //     synthesizer.close() never guaranteed.
            if (SDK.SpeakerAudioDestination) {
              player = new SDK.SpeakerAudioDestination();
              this.currentPlayer = player;

              player.onAudioEnd = () => {
                if (isCancelled) return;
                if (myGen === this._speechGen) {
                  this.notifyPlaybackStopped = null;
                }
                console.log(`${TAG} [tts-audio-end] gen=${myGen} playback finished.`);
                subscriber.next();
                subscriber.complete();
              };

              // If stopSpeaking() force-kills playback, settle this observable
              // so awaiting callers (voice panel) don't hang forever.
              this.notifyPlaybackStopped = () => {
                if (isCancelled) return;
                subscriber.next();
                subscriber.complete();
              };
            }

            const audioConfig = player
              ? SDK.AudioConfig.fromSpeakerOutput(player)
              : SDK.AudioConfig.fromDefaultSpeakerOutput();
            synthesizer = new SDK.SpeechSynthesizer(speechConfig, audioConfig);
            this.currentSynthesizer = synthesizer;
            console.log(`${TAG} [tts-6] gen=${myGen} SpeechSynthesizer created. voice="${voiceName || language}"`);

            // Prefer SSML (prosody + sentence pauses) when we have a pinned voice
            // and the SDK supports it; otherwise fall back to plain-text synthesis.
            const useSsml = !!voiceName && typeof synthesizer.speakSsmlAsync === 'function';
            const onResult = (result: any) => {
                if (isCancelled) {
                  this.cleanupSynthesizer(synthesizer);
                  return;
                }

                const reason = result?.reason;
                console.log(`${TAG} [tts-result] gen=${myGen} reason=${this.reasonLabel(SDK, reason)} (synthesis done, playback may continue)`);

                if (reason === SDK.ResultReason.SynthesizingAudioCompleted) {
                  // Synthesis (data transfer) is complete. Release the network
                  // handle, but do NOT complete the observable yet — the player
                  // keeps playing and onAudioEnd signals true completion.
                  this.cleanupSynthesizer(synthesizer);
                  if (!player) {
                    // Fallback path without SpeakerAudioDestination: old behavior.
                    subscriber.next();
                    subscriber.complete();
                  }
                } else if (reason === SDK.ResultReason.Canceled) {
                  const cancellation = SDK.CancellationDetails.fromResult(result);
                  subscriber.error(
                    new Error(`Speech synthesis canceled: ${cancellation.errorDetails || cancellation.reason}`)
                  );
                  this.cleanupSynthesizer(synthesizer);
                  this.cleanupPlayer(player);
                } else {
                  subscriber.error(new Error(`Speech synthesis finished with reason: ${reason}`));
                  this.cleanupSynthesizer(synthesizer);
                  this.cleanupPlayer(player);
                }
              };

            const onError = (error: any) => {
                if (isCancelled) {
                  this.cleanupSynthesizer(synthesizer);
                  return;
                }
                const errorMsg = typeof error === 'string' ? error : (error?.message || 'Unknown synthesis error');
                console.error(`${TAG} [tts-15✗] gen=${myGen} synthesis ERROR.`, { errorMsg });
                subscriber.error(new Error(errorMsg));
                this.cleanupSynthesizer(synthesizer);
                this.cleanupPlayer(player);
              };

            if (useSsml) {
              const ssml = this.buildSsml(text, language, voiceName);
              console.log(`${TAG} [tts-7] gen=${myGen} speaking via SSML.`);
              synthesizer.speakSsmlAsync(ssml, onResult, onError);
            } else {
              synthesizer.speakTextAsync(text, onResult, onError);
            }
          } catch (err: any) {
            console.error(`${TAG} [tts-15✗] Synchronous exception during synthesizer setup.`, err);
            subscriber.error(err instanceof Error ? err : new Error(String(err)));
            if (synthesizer) {
              this.cleanupSynthesizer(synthesizer);
            }
            this.cleanupPlayer(player);
          }

          return () => {
            isCancelled = true;
            // Only tear down the shared audio pipeline if this generation still
            // owns it — otherwise we would kill a NEWER speak()'s playback.
            if (myGen === this._speechGen) {
              this.stopSpeaking();
            } else {
              this.cleanupSynthesizer(synthesizer);
              this.cleanupPlayer(player);
            }
          };
        });
      }),
      catchError(err => {
        console.error(`${TAG} [tts-pipeline✗] Fatal error in speak() pipeline.`, err);
        return throwError(() => err);
      })
    );
  }

  /**
   * Cancels active speech synthesis and disposes synthesizer resources.
   */
  stopSpeaking(): void {
    // Kill browser-native TTS (VoiceSynthesisService / Web Speech API) so the
    // two audio engines never play simultaneously. Chrome has a known bug where
    // cancel() alone doesn't immediately stop the current utterance — calling
    // pause() first forces an immediate cutoff.
    if (typeof window !== 'undefined' && window.speechSynthesis) {
      window.speechSynthesis.pause();
      window.speechSynthesis.cancel();
    }

    // Detach the completion notifier BEFORE touching the player, then invoke it
    // at the end so any awaiting caller settles instead of hanging.
    const notify = this.notifyPlaybackStopped;
    this.notifyPlaybackStopped = null;

    // Halt any ElevenLabs MP3 playback instantly.
    if (this.currentHtmlAudio) {
      console.log(`${TAG} [tts-stop] stopSpeaking() — pausing active ElevenLabs audio.`);
      const audio = this.currentHtmlAudio;
      this.currentHtmlAudio = null;
      try { audio.pause(); } catch { /* suppress */ }
      try { audio.src = ''; } catch { /* suppress */ }
    }

    // player.pause() halts audible output instantly — this is the only reliable
    // way to silence Azure audio that is already in the output buffer.
    if (this.currentPlayer) {
      console.log(`${TAG} [tts-stop] stopSpeaking() — pausing and closing active player.`);
      const player = this.currentPlayer;
      this.currentPlayer = null;
      try { player.pause(); } catch { /* suppress */ }
      try { player.close(); } catch { /* suppress */ }
    }

    if (this.currentSynthesizer) {
      console.log(`${TAG} [tts-stop] stopSpeaking() — closing active synthesizer.`);
      const synthesizer = this.currentSynthesizer;
      this.currentSynthesizer = null;
      try {
        synthesizer.close();
      } catch {
        // Suppress close errors
      }
    }

    if (notify) {
      try { notify(); } catch { /* suppress */ }
    }
  }

  // ── Private Helpers ──────────────────────────────────────────────────────

  private cleanupRecognizer(recognizer: any): void {
    if (!recognizer) return;
    if (this.currentRecognizer === recognizer) {
      this.currentRecognizer = null;
    }
    try {
      recognizer.close();
    } catch {
      // Suppress close errors
    }
  }

  private cleanupSynthesizer(synthesizer: any): void {
    if (!synthesizer) return;
    if (this.currentSynthesizer === synthesizer) {
      this.currentSynthesizer = null;
    }
    try {
      synthesizer.close();
    } catch {
      // Suppress close errors
    }
  }

  private cleanupPlayer(player: any): void {
    if (!player) return;
    if (this.currentPlayer === player) {
      this.currentPlayer = null;
    }
    try { player.pause(); } catch { /* suppress */ }
    try { player.close(); } catch { /* suppress */ }
  }

  /** Returns a human-readable label for an SDK ResultReason numeric value. */
  private reasonLabel(SDK: any, reason: number): string {
    if (SDK.ResultReason) {
      for (const [key, val] of Object.entries(SDK.ResultReason)) {
        if (val === reason) return key;
      }
    }
    return `Reason(${reason})`;
  }
}
