/**
 * @file tour.tool.ts
 * @description AI Agent Tool for initiating guided tours and onboarding walkthroughs.
 * Responsibility: Allow the AI Agent to trigger interactive tours for specific features or pages.
 */

import { Injectable, inject } from '@angular/core';
import { Observable, of } from 'rxjs';
import { AssistantTool, ToolContext, ToolExecutionResult } from '../models/assistant-tool';
import { TourEngineService } from '../services/tour-engine.service';

export interface TourToolParams {
  tourId: string;
  autoStart?: boolean;
}

@Injectable({
  providedIn: 'root',
})
export class TourTool implements AssistantTool<TourToolParams, boolean> {
  readonly name = 'tour';
  readonly description = 'Starts an interactive feature walkthrough tour.';

  private readonly tourEngine = inject(TourEngineService);

  execute(params: TourToolParams, _context?: ToolContext): Observable<ToolExecutionResult<boolean>> {
    if (!params || !params.tourId) {
      return of({ success: false, message: 'Tour ID parameter is required.' });
    }

    const started = this.tourEngine.startTour(params.tourId);

    if (!started) {
      return of({
        success: false,
        message: `Tour "${params.tourId}" is not registered. Available tours: ${this.tourEngine.getRegisteredScenarios().map(s => s.id).join(', ')}`,
      });
    }

    return of({
      success: true,
      message: `Started tour "${params.tourId}"`,
      data: true,
    });
  }
}
