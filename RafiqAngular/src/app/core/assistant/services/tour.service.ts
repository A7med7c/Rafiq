/**
 * @file tour.service.ts
 * @description Facade service for guided tours.
 * Responsibility: Connects legacy TourDefinition calls to the new TourEngineService.
 */

import { Injectable, inject } from '@angular/core';
import { TourEngineService } from './tour-engine.service';
import { TourScenario, TourStepScenario } from '../models/tour-scenario';

export interface TourStep {
  element?: string;
  popover?: {
    title?: string;
    description?: string;
    side?: 'left' | 'right' | 'top' | 'bottom';
    align?: 'start' | 'center' | 'end';
  };
}

export interface TourDefinition {
  id: string;
  steps: TourStep[];
  showProgress?: boolean;
  allowClose?: boolean;
}

@Injectable({
  providedIn: 'root',
})
export class TourService {
  private readonly engine = inject(TourEngineService);

  /** Registers a tour scenario or legacy TourDefinition into the TourEngineService. */
  registerTour(definition: TourDefinition | TourScenario): void {
    if ('steps' in definition) {
      const scenario: TourScenario = {
        id: definition.id,
        title: definition.id,
        steps: definition.steps.map((s: any) => ({
          anchor: s.element || s.anchor,
          speech: s.popover?.description || s.speech,
          highlight: true,
          placement: s.popover?.side || s.placement || 'left',
        })),
      };
      this.engine.registerScenario(scenario);
    }
  }

  /** Starts the tour identified by tourId. */
  startTour(tourId: string): void {
    this.engine.startTour(tourId);
  }

  /** Returns whether a tour with the given ID has been registered. */
  hasTour(tourId: string): boolean {
    return !!this.engine.getScenario(tourId);
  }

  /** Returns all registered tour IDs. */
  getRegisteredTourIds(): string[] {
    return this.engine.getRegisteredScenarios().map(s => s.id);
  }
}
