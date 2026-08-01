/**
 * @file highlight.tool.ts
 * @description AI Agent Tool for visually highlighting UI elements or sections on the page.
 * Responsibility: Allow the AI Agent to draw attention to specific UI elements via glowing borders or spotlights.
 * Supports anchor names from AssistantAnchorRegistryService as well as standard CSS selectors.
 */

import { Injectable, inject } from '@angular/core';
import { Observable, of } from 'rxjs';
import { AssistantTool, ToolContext, ToolExecutionResult } from '../models/assistant-tool';
import { AssistantAnchorRegistryService } from '../services/assistant-anchor-registry.service';

export interface HighlightToolParams {
  selector: string;
  durationMs?: number;
}

@Injectable({
  providedIn: 'root',
})
export class HighlightTool implements AssistantTool<HighlightToolParams, boolean> {
  readonly name = 'highlight';
  readonly description = 'Visually highlights a specified UI element or anchor on the DOM.';

  private readonly anchorRegistry = inject(AssistantAnchorRegistryService);

  execute(params: HighlightToolParams, _context?: ToolContext): Observable<ToolExecutionResult<boolean>> {
    if (!params || !params.selector) {
      return of({ success: false, message: 'Selector or anchor name parameter is required.' });
    }

    let element: Element | null = this.anchorRegistry.getAnchorElement(params.selector);

    if (!element && typeof document !== 'undefined') {
      const cleaned = params.selector.startsWith('#') || params.selector.startsWith('.')
        ? params.selector
        : `#${params.selector}`;
      element = document.querySelector(cleaned) || document.getElementById(params.selector);
    }

    if (!element) {
      return of({ success: false, message: `Element or anchor "${params.selector}" not found.` });
    }

    element.classList.add('rafiq-agent-highlight');

    const duration = params.durationMs ?? 3000;
    setTimeout(() => {
      element?.classList.remove('rafiq-agent-highlight');
    }, duration);

    return of({
      success: true,
      message: `Highlighted "${params.selector}" for ${duration}ms`,
      data: true,
    });
  }
}
