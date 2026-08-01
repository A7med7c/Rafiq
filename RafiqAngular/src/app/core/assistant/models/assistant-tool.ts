/**
 * @file assistant-tool.ts
 * @description Defines the tool interface and contracts for the Rafiq AI Agent architecture.
 * Responsibility: Provide the base tool interface that all AI Agent tools (navigate, highlight, dialog, etc.) implement.
 */

import { Observable } from 'rxjs';

export interface ToolContext {
  activeProfileId?: string | null;
  activeConversationId?: string | null;
  currentRoute?: string;
  language?: string;
}

export interface ToolExecutionResult<T = any> {
  success: boolean;
  message?: string;
  data?: T;
}

export interface AssistantTool<TParams = any, TResult = any> {
  readonly name: string;
  readonly description: string;
  readonly parametersSchema?: Record<string, any>;

  execute(params: TParams, context?: ToolContext): Observable<ToolExecutionResult<TResult>> | Promise<ToolExecutionResult<TResult>>;
}
