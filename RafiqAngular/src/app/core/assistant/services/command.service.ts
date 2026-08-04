/**
 * @file command.service.ts
 * @description Manages the registration, parsing, and execution of AssistantCommands by dispatching them
 * to the appropriate registered AssistantTool.
 * Responsibility: Acts as the tool registry, command parser, and command dispatcher within the agent architecture.
 */

import { Injectable, inject } from '@angular/core';
import { Observable, of } from 'rxjs';
import { AssistantCommand, CommandResult } from '../models/assistant-command';
import { AssistantTool, ToolContext } from '../models/assistant-tool';
import { NavigateTool } from '../tools/navigate.tool';
import { HighlightTool } from '../tools/highlight.tool';
import { DialogTool } from '../tools/dialog.tool';
import { ApiTool } from '../tools/api.tool';
import { TourTool } from '../tools/tour.tool';

@Injectable({
  providedIn: 'root',
})
export class CommandService {
  /** Registry mapping tool names to tool implementations. */
  private readonly toolRegistry = new Map<string, AssistantTool>();

  constructor() {
    // Register all available tools at startup
    this.registerTool(inject(NavigateTool));
    this.registerTool(inject(HighlightTool));
    this.registerTool(inject(DialogTool));
    this.registerTool(inject(ApiTool));
    this.registerTool(inject(TourTool));
  }

  /** Registers a tool implementation into the tool registry. */
  registerTool(tool: AssistantTool): void {
    this.toolRegistry.set(tool.name, tool);
  }

  /**
   * Processes raw speech/user input text: parses intent, maps to command, logs steps, and executes tool.
   */
  processUserText(text: string, context?: ToolContext): Observable<CommandResult> {
    console.log('[Assistant] heard:', text);

    const command = this.parseCommand(text, context);

    if (!command) {
      console.log('No command matched.');
      return of({
        commandId: `cmd-none-${Date.now()}`,
        success: false,
        error: 'No command matched.',
        executionTimeMs: 0,
      });
    }

    console.log('[Assistant] matched command:', command.type, command.payload);
    console.log('[Assistant] executing tool:', command.toolName);

    return this.execute(command, context);
  }

  private tourIdForRoute(route?: string): string {
    const r = route ?? (typeof window !== 'undefined' ? window.location.pathname : '');
    if (r.includes('/medical-records'))  return 'medical-records-tour';
    if (r.includes('/appointments'))     return 'appointments-tour';
    if (r.includes('/medications'))      return 'medications-tour';
    if (r.includes('/family-profiles'))  return 'family-profiles-tour';
    if (r.includes('/my-profile'))       return 'my-profile-tour';
    return 'dashboard-tour';
  }

  /**
   * Parses text input into an AssistantCommand object.
   */
  parseCommand(text: string, context?: ToolContext): AssistantCommand | null {
    if (!text || typeof text !== 'string') {
      return null;
    }

    // Strip punctuation and normalize case
    const normalized = text
      .toLowerCase()
      .trim()
      .replace(/[.,\/#!$%\^&\*;:{}=\-_`~()]/g, '');

    console.log('[Assistant] parsed:', normalized);

    // Route navigation matching rules
    const routeRules: Array<{ keywords: string[]; url: string }> = [
      {
        keywords: ['appointment', 'appointments', 'المواعيد', 'موعد', 'الموعد'],
        url: '/appointments',
      },
      {
        keywords: ['medication', 'medications', 'medicine', 'medicines', 'الأدوية', 'أدوية', 'دواء'],
        url: '/medications',
      },
      {
        keywords: ['dashboard', 'home', 'الرئيسية', 'لوحة التحكم'],
        url: '/dashboard',
      },
      {
        keywords: ['record', 'records', 'medical record', 'medical records', 'السجلات', 'السجلات الطبية'],
        url: '/medical-records',
      },
      {
        keywords: ['family', 'family profile', 'family profiles', 'العائلة', 'عائلة'],
        url: '/family-profiles',
      },
      {
        keywords: ['profile', 'my profile', 'البروفايل', 'الملف الشخصي'],
        url: '/my-profile',
      },
    ];

    for (const rule of routeRules) {
      if (rule.keywords.some(kw => normalized.includes(kw))) {
        return {
          id: `cmd-${Date.now()}`,
          type: 'navigate',
          toolName: 'navigate',
          payload: { url: rule.url },
          timestamp: Date.now(),
        };
      }
    }

    // Tour initiation matching rules — pick the tour for the current page
    const tourKeywords = ['tour', 'walkthrough', 'جولة', 'الرباط'];
    if (tourKeywords.some(kw => normalized.includes(kw))) {
      return {
        id: `cmd-${Date.now()}`,
        type: 'startTour',
        toolName: 'tour',
        payload: { tourId: this.tourIdForRoute(context?.currentRoute) },
        timestamp: Date.now(),
      };
    }

    return null;
  }

  /**
   * Dispatches a command to its matching tool and returns an Observable of CommandResult.
   * Emits an error result (not an Observable error) if no tool is found.
   */
  execute<TResult = any>(command: AssistantCommand, context?: ToolContext): Observable<CommandResult<TResult>> {
    const startTime = Date.now();
    const tool = this.toolRegistry.get(command.toolName);

    if (!tool) {
      return of({
        commandId: command.id,
        success: false,
        error: `No tool registered for name: "${command.toolName}"`,
        executionTimeMs: Date.now() - startTime,
      });
    }

    const result$ = tool.execute(command.payload, context);

    // Handle both Observable and Promise returns from tools
    const obs$: Observable<any> = result$ instanceof Promise
      ? new Observable(s => { result$.then(r => { s.next(r); s.complete(); }).catch(e => s.error(e)); })
      : result$;

    return new Observable<CommandResult<TResult>>(subscriber => {
      obs$.subscribe({
        next: toolResult => {
          subscriber.next({
            commandId: command.id,
            success: toolResult.success,
            data: toolResult.data,
            error: toolResult.message,
            executionTimeMs: Date.now() - startTime,
          });
          subscriber.complete();
        },
        error: err => {
          subscriber.next({
            commandId: command.id,
            success: false,
            error: err?.message || String(err),
            executionTimeMs: Date.now() - startTime,
          });
          subscriber.complete();
        },
      });
    });
  }

  /** Returns the list of all registered tool names. */
  getRegisteredToolNames(): string[] {
    return Array.from(this.toolRegistry.keys());
  }
}
