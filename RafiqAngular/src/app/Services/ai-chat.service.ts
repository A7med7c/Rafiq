import { HttpClient } from '@angular/common/http';
import { Injectable, inject, signal } from '@angular/core';
import { Observable, catchError, map, of } from 'rxjs';
import { environment } from '../Environments/Environment';
import { ApiResponse } from '../Modles/api-response';
import {
  AiMessageResponseDto,
  ConversationHistoryDto,
  ConversationSummaryDto,
  CreateConversationRequest,
  SendMessageRequest,
} from '../Modles/ai-chat.models';

@Injectable({ providedIn: 'root' })
export class AiChatService {
  private readonly http = inject(HttpClient);
  private readonly base = `${environment.apiUrl}/chat`;

  readonly isPanelOpen = signal(false);
  // Incremented each time the robot is clicked — AiPanel watches this and
  // switches to voice mode. Using a counter (not boolean) ensures the effect
  // fires even when the panel is already open.
  readonly voiceModeRequest = signal(0);

  // ── Session-scoped image cache ───────────────────────────
  // Key: `${conversationId}:${sequenceNumber}` → data-URL
  // Backed by sessionStorage so images survive page reloads
  // within the same browser tab. Falls back to an in-memory
  // map when sessionStorage is unavailable or full.
  private static readonly _SS_KEY = 'rafiq_img_cache';
  private readonly _imgCache = new Map<string, string>();

  constructor() {
    // Warm the in-memory map from sessionStorage on startup.
    try {
      const stored = sessionStorage.getItem(AiChatService._SS_KEY);
      if (stored) {
        const parsed: Record<string, string> = JSON.parse(stored);
        for (const [k, v] of Object.entries(parsed)) {
          this._imgCache.set(k, v);
        }
      }
    } catch { /* sessionStorage unavailable */ }
  }

  cacheImage(conversationId: string, seq: number, dataUrl: string): void {
    const key = `${conversationId}:${seq}`;
    this._imgCache.set(key, dataUrl);
    try {
      const stored = sessionStorage.getItem(AiChatService._SS_KEY);
      const parsed: Record<string, string> = stored ? JSON.parse(stored) : {};
      parsed[key] = dataUrl;
      sessionStorage.setItem(AiChatService._SS_KEY, JSON.stringify(parsed));
    } catch { /* quota exceeded or unavailable — in-memory only */ }
  }

  getCachedImage(conversationId: string, seq: number): string | undefined {
    return this._imgCache.get(`${conversationId}:${seq}`);
  }

  openPanel(): void {
    this.isPanelOpen.set(true);
  }

  openPanelInVoiceMode(): void {
    this.isPanelOpen.set(true);
    this.voiceModeRequest.update(v => v + 1);
  }

  closePanel(): void {
    this.isPanelOpen.set(false);
  }

  togglePanel(): void {
    this.isPanelOpen.update(v => !v);
  }

  getConversations(): Observable<ConversationSummaryDto[]> {
    return this.http.get<ApiResponse<ConversationSummaryDto[]>>(`${this.base}/conversations`).pipe(
      map(r => r.data ?? []),
      catchError(() => of([] as ConversationSummaryDto[]))
    );
  }

  getConversationHistory(conversationId: string): Observable<ConversationHistoryDto | null> {
    return this.http.get<ApiResponse<ConversationHistoryDto>>(`${this.base}/conversations/${conversationId}`).pipe(
      map(r => r.data ?? null),
      catchError(() => of(null))
    );
  }

  createConversation(request: CreateConversationRequest): Observable<ApiResponse<string>> {
    return this.http.post<ApiResponse<string>>(`${this.base}/conversations`, request);
  }

  sendMessage(conversationId: string, request: SendMessageRequest): Observable<ApiResponse<AiMessageResponseDto>> {
    return this.http.post<ApiResponse<AiMessageResponseDto>>(
      `${this.base}/conversations/${conversationId}/messages`,
      request
    );
  }

  renameConversation(conversationId: string, title: string): Observable<ApiResponse<ConversationSummaryDto>> {
    return this.http.patch<ApiResponse<ConversationSummaryDto>>(`${this.base}/conversations/${conversationId}`, {
      title,
    });
  }

  archiveConversation(conversationId: string): Observable<ApiResponse<boolean>> {
    return this.http.delete<ApiResponse<boolean>>(`${this.base}/conversations/${conversationId}`);
  }

  generateConversationTitle(conversationId: string): Observable<ApiResponse<string>> {
    return this.http.post<ApiResponse<string>>(`${this.base}/conversations/${conversationId}/generate-title`, {});
  }

  reactToMessage(
    conversationId: string,
    messageId: string,
    reactionType: 'ThumbsUp' | 'ThumbsDown',
    remove: boolean,
    feedback?: string
  ): Observable<ApiResponse<boolean>> {
    return this.http.post<ApiResponse<boolean>>(
      `${this.base}/conversations/${conversationId}/messages/${messageId}/react`,
      { reactionType, remove, feedback }
    );
  }
}
