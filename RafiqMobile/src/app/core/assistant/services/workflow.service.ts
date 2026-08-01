/**
 * @file workflow.service.ts
 * @description Manages multi-step agent workflows composed of sequential or parallel AssistantCommands.
 * Responsibility: Orchestrate command sequences, track workflow progress, and emit step-level events.
 * This is the execution engine for AI-driven workflows (e.g., "book an appointment" = multiple commands).
 */

import { Injectable, inject } from '@angular/core';
import { Observable, Subject, from, concatMap, tap, catchError, of, finalize } from 'rxjs';
import { AssistantCommand, CommandResult } from '../models/assistant-command';
import { ToolContext } from '../models/assistant-tool';
import { CommandService } from './command.service';

export interface WorkflowStep {
  command: AssistantCommand;
  description?: string;
}

export interface WorkflowResult {
  workflowId: string;
  totalSteps: number;
  completedSteps: number;
  results: CommandResult[];
  success: boolean;
}

export interface WorkflowProgressEvent {
  workflowId: string;
  stepIndex: number;
  totalSteps: number;
  result: CommandResult;
}

@Injectable({
  providedIn: 'root',
})
export class WorkflowService {
  private readonly commandService = inject(CommandService);

  /** Emits progress events for each step of any running workflow. */
  readonly stepCompleted$ = new Subject<WorkflowProgressEvent>();

  /**
   * Executes a list of workflow steps sequentially.
   * Each step runs after the previous one completes, regardless of success/failure.
   * Returns an Observable emitting the aggregated WorkflowResult when all steps finish.
   */
  executeSequential(
    workflowId: string,
    steps: WorkflowStep[],
    context?: ToolContext
  ): Observable<WorkflowResult> {
    const results: CommandResult[] = [];

    return from(steps).pipe(
      concatMap((step, index) =>
        this.commandService.execute(step.command, context).pipe(
          tap(result => {
            results.push(result);
            this.stepCompleted$.next({
              workflowId,
              stepIndex: index,
              totalSteps: steps.length,
              result,
            });
          }),
          catchError(err => {
            const errorResult: CommandResult = {
              commandId: step.command.id,
              success: false,
              error: err?.message || String(err),
              executionTimeMs: 0,
            };
            results.push(errorResult);
            return of(errorResult);
          })
        )
      ),
      finalize(() => {
        // All steps complete — nothing to clean up at this time
      }),
      // After all concatMap emissions, reduce to a single WorkflowResult
      // via a final mapping in the subscribe or orchestrator
      concatMap(() => of(null)),
    ).pipe(
      finalize(() => {}),
      // Return final aggregate after steps complete via caller subscription
      // Orchestrator collects WorkflowResult through the last emission
      concatMap(() =>
        of<WorkflowResult>({
          workflowId,
          totalSteps: steps.length,
          completedSteps: results.length,
          results,
          success: results.every(r => r.success),
        })
      )
    );
  }
}
