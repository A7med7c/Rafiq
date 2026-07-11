import { HttpClient } from '@angular/common/http';
import { Injectable, inject } from '@angular/core';
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
}
