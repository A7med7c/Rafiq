/**
 * @file assistant-anchor-registry.service.ts
 * @description Central registry service for UI elements declaring assistant anchors.
 * Responsibility: Allows any component to register [assistantAnchor]="name" elements dynamically,
 * enabling the AI agent and position service to target elements by name without direct DOM queries.
 */

import { Injectable, signal } from '@angular/core';

@Injectable({
  providedIn: 'root',
})
export class AssistantAnchorRegistryService {
  /** Registry mapping anchor names to DOM elements */
  private readonly anchorMap = new Map<string, HTMLElement>();

  /** Signal containing all registered anchor names for reactive inspection */
  private readonly _anchorNames = signal<string[]>([]);
  readonly anchorNames = this._anchorNames.asReadonly();

  /**
   * Registers a DOM element with a given anchor name.
   */
  registerAnchor(name: string, element: HTMLElement): void {
    if (!name || !element) return;
    this.anchorMap.set(name, element);
    this.updateAnchorNamesSignal();
  }

  /**
   * Unregisters an anchor by name.
   */
  unregisterAnchor(name: string, element?: HTMLElement): void {
    if (!name) return;
    const existing = this.anchorMap.get(name);
    // Only remove if it matches the current element (handles re-renders)
    if (!element || existing === element) {
      this.anchorMap.delete(name);
      this.updateAnchorNamesSignal();
    }
  }

  /**
   * Retrieves the DOM element registered for a given anchor name.
   */
  getAnchorElement(name: string): HTMLElement | null {
    return this.anchorMap.get(name) ?? null;
  }

  /**
   * Checks if an anchor with the given name is currently registered.
   */
  hasAnchor(name: string): boolean {
    return this.anchorMap.has(name);
  }

  /**
   * Returns all currently registered anchor names.
   */
  getRegisteredAnchorNames(): string[] {
    return Array.from(this.anchorMap.keys());
  }

  private updateAnchorNamesSignal(): void {
    queueMicrotask(() => {
      this._anchorNames.set(Array.from(this.anchorMap.keys()));
    });
  }
}
