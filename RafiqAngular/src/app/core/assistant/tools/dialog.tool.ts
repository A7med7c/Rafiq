/**
 * @file dialog.tool.ts
 * @description AI Agent Tool for triggering UI dialogs, alerts, or toast notifications.
 * Responsibility: Allow the AI Agent to present dialog messages or toast notifications to the user.
 */

import { Injectable, inject } from '@angular/core';
import { Observable, of } from 'rxjs';
import { AssistantTool, ToolContext, ToolExecutionResult } from '../models/assistant-tool';
import { NotificationService } from '../../../Services/notification.service';

export interface DialogToolParams {
  title: string;
  message: string;
  type?: 'info' | 'success' | 'warning' | 'error';
}

@Injectable({
  providedIn: 'root',
})
export class DialogTool implements AssistantTool<DialogToolParams, boolean> {
  readonly name = 'dialog';
  readonly description = 'Displays a notification or dialog to the user.';

  private readonly notifService = inject(NotificationService, { optional: true });

  execute(params: DialogToolParams, _context?: ToolContext): Observable<ToolExecutionResult<boolean>> {
    if (!params || !params.message) {
      return of({ success: false, message: 'Dialog message parameter is required.' });
    }

    const title = params.title || 'Rafiq Assistant';
    const type = params.type ?? 'info';
    // NotificationService.showToast accepts 'info' | 'success' | 'error'; map 'warning' → 'info'
    const toastType: 'info' | 'success' | 'error' =
      type === 'warning' ? 'info' : (type as 'info' | 'success' | 'error');

    if (this.notifService) {
      this.notifService.showToast(title, params.message, toastType);
    } else {
      console.log(`[DialogTool] ${title}: ${params.message}`);
    }

    return of({
      success: true,
      message: `Displayed ${type} dialog`,
      data: true,
    });
  }
}
