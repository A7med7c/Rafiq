/**
 * @file api.tool.ts
 * @description AI Agent Tool for executing backend API calls on behalf of the AI Agent.
 * Responsibility: Provide generic HTTP requests execution capability for agent tools.
 */

import { Injectable, inject } from '@angular/core';
import { HttpClient } from '@angular/common/http';
import { Observable, of, catchError, map } from 'rxjs';
import { AssistantTool, ToolContext, ToolExecutionResult } from '../models/assistant-tool';
import { environment } from '../../../Environments/Environment';

export interface ApiToolParams {
  endpoint: string;
  method?: 'GET' | 'POST' | 'PUT' | 'DELETE';
  body?: any;
}

@Injectable({
  providedIn: 'root',
})
export class ApiTool implements AssistantTool<ApiToolParams, any> {
  readonly name = 'api';
  readonly description = 'Executes a REST API call to the backend server.';

  private readonly http = inject(HttpClient);

  execute(params: ApiToolParams, _context?: ToolContext): Observable<ToolExecutionResult<any>> {
    if (!params || !params.endpoint) {
      return of({ success: false, message: 'API endpoint parameter is required.' });
    }

    const method = params.method ?? 'GET';
    const url = params.endpoint.startsWith('http')
      ? params.endpoint
      : `${environment.apiUrl}/${params.endpoint.replace(/^\//, '')}`;

    let req$: Observable<any>;
    if (method === 'POST') {
      req$ = this.http.post(url, params.body ?? {});
    } else if (method === 'PUT') {
      req$ = this.http.put(url, params.body ?? {});
    } else if (method === 'DELETE') {
      req$ = this.http.delete(url);
    } else {
      req$ = this.http.get(url);
    }

    return req$.pipe(
      map(res => ({
        success: true,
        data: res,
      })),
      catchError(err =>
        of({
          success: false,
          message: `API request failed: ${err.message || err}`,
        })
      )
    );
  }
}
