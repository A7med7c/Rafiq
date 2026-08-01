import { HttpErrorResponse } from '@angular/common/http';
import { ApiErrorBody } from '../Modles/api-response';

export function getApiErrorMessages(error: HttpErrorResponse): string[] {
  const body = error.error as ApiErrorBody | null;

  if (body?.errors?.length) {
    return body.errors;
  }

  if (body?.message) {
    return [body.message];
  }

  switch (error.status) {
    case 400:
      return ['Invalid request. Please check your input.'];
    case 401:
      return ['Authentication required. Please sign in again.'];
    case 403:
      return ['You do not have permission to perform this action.'];
    case 500:
      return ['An unexpected server error occurred. Please try again later.'];
    default:
      return ['An unexpected error occurred. Please try again.'];
  }
}

export function getApiErrorMessage(error: HttpErrorResponse): string {
  return getApiErrorMessages(error).join(' ');
}
