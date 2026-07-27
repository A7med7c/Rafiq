export interface ConversationSummaryDto {
  id: string;
  userHealthProfileId: string;
  title: string;
  lastMessageAt: string | null;
  createdAt: string;
}

export interface ConversationMessageDto {
  id: string;
  role: string; // "User" | "Assistant"
  content: string;
  sequenceNumber: number;
  createdAt: string;
  userReaction?: 'ThumbsUp' | 'ThumbsDown' | null;
  /** "Pending" | "Processing" | "Completed" | "Failed" — only set for Assistant messages */
  status?: string;
  errorMessage?: string;
}

export interface ConversationHistoryDto {
  id: string;
  title: string;
  lastMessageAt: string | null;
  messages: ConversationMessageDto[];
}

export interface AiMessageResponseDto {
  id: string;
  content: string;
}

export interface CreateConversationRequest {
  userHealthProfileId: string;
  title: string;
}

export interface SendMessageRequest {
  text: string;
  base64Image?: string | null;
  imageFormat?: string | null;
  language?: string;
  utcOffsetMinutes?: number;
}
