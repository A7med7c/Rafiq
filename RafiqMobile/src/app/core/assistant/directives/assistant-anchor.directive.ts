/**
 * @file assistant-anchor.directive.ts
 * @description Directive for marking UI elements as assistant anchors.
 * Responsibility: Automatically registers host DOM element with AssistantAnchorRegistryService on creation
 * and unregisters on destruction.
 * Usage: <button [assistantAnchor]="'appointments'"> or <div assistantAnchor="medications">
 */

import {
  Directive,
  ElementRef,
  Input,
  OnDestroy,
  OnInit,
  OnChanges,
  SimpleChanges,
  inject,
} from '@angular/core';
import { AssistantAnchorRegistryService } from '../services/assistant-anchor-registry.service';

@Directive({
  selector: '[assistantAnchor]',
  standalone: true,
})
export class AssistantAnchorDirective implements OnInit, OnChanges, OnDestroy {
  @Input('assistantAnchor') anchorName: string = '';

  private readonly elementRef = inject(ElementRef);
  private readonly registry = inject(AssistantAnchorRegistryService);
  private registeredName: string = '';

  ngOnInit(): void {
    this.register();
  }

  ngOnChanges(changes: SimpleChanges): void {
    if (changes['anchorName'] && !changes['anchorName'].firstChange) {
      this.unregister();
      this.register();
    }
  }

  ngOnDestroy(): void {
    this.unregister();
  }

  private register(): void {
    if (this.anchorName) {
      this.registeredName = this.anchorName;
      this.registry.registerAnchor(this.registeredName, this.elementRef.nativeElement);
    }
  }

  private unregister(): void {
    if (this.registeredName) {
      this.registry.unregisterAnchor(this.registeredName, this.elementRef.nativeElement);
      this.registeredName = '';
    }
  }
}
