/**
 * @file assistant-state.ts
 * @description Core state models, state types, and status metadata for the Rafiq AI Agent architecture.
 * Responsibility: Provide strongly-typed state contracts for state management across orchestrator, UI, avatar engine, and services.
 */

export type AvatarState =
  | 'idle'
  | 'wave'
  | 'listening'
  | 'thinking'
  | 'speaking'
  | 'pointLeft'
  | 'pointRight'
  | 'celebrate'
  | 'error';

export type AssistantState = 'idle' | 'listening' | 'speaking' | 'processing' | 'error';

export interface AssistantStatus {
  state: AssistantState;
  label: string;
  badgeClass: string;
  dotClass: string;
  description: string;
}

export interface AssistantStateContext {
  state: AssistantState;
  avatarState: AvatarState;
  title: string;
  subtitle: string;
  isOpen: boolean;
  activeProfileId?: string | null;
  activeConversationId?: string | null;
  errorMessage?: string | null;
  lastRecognizedText?: string | null;
}
