/**
 * @file navigate.tool.ts
 * @description AI Agent Tool for executing programmatic client-side route navigation.
 * Responsibility: Allow the AI Agent to navigate to different views/pages in the Angular app.
 */

import { Injectable, inject } from '@angular/core';
import { Router } from '@angular/router';
import { Observable, of } from 'rxjs';
import { AssistantTool, ToolContext, ToolExecutionResult } from '../models/assistant-tool';

export interface NavigateToolParams {
  url: string;
}

@Injectable({
  providedIn: 'root',
})
export class NavigateTool implements AssistantTool<NavigateToolParams, boolean> {
  readonly name = 'navigate';
  readonly description = 'Navigates the user interface to a specified route URL.';

  private readonly router = inject(Router);

  execute(params: NavigateToolParams, _context?: ToolContext): Observable<ToolExecutionResult<boolean>> {
    if (!params || !params.url) {
      return of({
        success: false,
        message: 'Navigation URL parameter is required.',
      });
    }

    console.log('[Assistant] navigation:', params.url);

    const navigated = this.router.navigateByUrl(params.url);
    return of({
      success: true,
      message: `Navigated to ${params.url}`,
      data: true,
    });
  }
}
