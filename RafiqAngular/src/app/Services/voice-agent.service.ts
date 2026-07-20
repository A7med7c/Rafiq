import { Injectable, inject } from '@angular/core';
import { HttpClient } from '@angular/common/http';
import { Observable } from 'rxjs';
import { environment } from '../Environments/Environment';
import { ApiResponse } from '../Modles/api-response';

export interface VoiceAgentResponseDto {
  sessionId: string;
  response:  string;
  navigateTo: string | null;
  needsMoreInfo: boolean;
}

@Injectable({ providedIn: 'root' })
export class VoiceAgentService {
  private readonly http = inject(HttpClient);
  private readonly base = `${environment.apiUrl}/voice-agent`;

  /** Creates a new voice session. Returns the session UUID. */
  createSession(profileId: string, language: string): Observable<ApiResponse<string>> {
    return this.http.post<ApiResponse<string>>(
      `${this.base}/sessions`,
      { profileId, language }
    );
  }

  /**
   * Streaming mode — returns 202 immediately.
   * The AI response arrives via SignalR (VoiceAgentResponse event).
   */
  sendMessageStream(sessionId: string, text: string, language: string): Observable<{ sessionId: string }> {
    return this.http.post<{ sessionId: string }>(
      `${this.base}/sessions/${sessionId}/messages/stream`,
      { text, language }
    );
  }

  /**
   * Synchronous mode — waits for the full response and returns it in the HTTP body.
   * Used by non-voice clients (Postman, mobile, tests). Also emits SignalR events.
   */
  sendMessage(sessionId: string, text: string, language: string): Observable<ApiResponse<VoiceAgentResponseDto>> {
    return this.http.post<ApiResponse<VoiceAgentResponseDto>>(
      `${this.base}/sessions/${sessionId}/messages`,
      { text, language }
    );
  }
}
