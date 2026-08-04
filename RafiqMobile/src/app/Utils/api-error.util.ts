import { HttpErrorResponse } from '@angular/common/http';
import { ApiErrorBody } from '../Modles/api-response';

export function getApiErrorMessages(error: HttpErrorResponse, t?: any): string[] {
  const body = error.error as ApiErrorBody | null;

  let rawErrors: string[] = [];

  if (body?.errors?.length) {
    rawErrors = body.errors;
  } else if (body?.message) {
    rawErrors = [body.message];
  } else {
    switch (error.status) {
      case 400:
        rawErrors = ['Invalid request. Please check your input.'];
        break;
      case 401:
        rawErrors = ['Authentication required. Please sign in again.'];
        break;
      case 403:
        rawErrors = ['You do not have permission to perform this action.'];
        break;
      case 500:
        rawErrors = ['An unexpected server error occurred. Please try again later.'];
        break;
      default:
        rawErrors = ['An unexpected error occurred. Please try again.'];
        break;
    }
  }

  // Attempt to translate
  if (t && t.Validation) {
    return rawErrors.map(err => {
      if (err.startsWith('Validation.')) {
        const key = err.substring('Validation.'.length);
        if (t.Validation[key]) {
          return t.Validation[key];
        }
      }
      return err;
    });
  }

  return rawErrors;
}

export function getApiErrorMessage(error: HttpErrorResponse, t?: any): string {
  return getApiErrorMessages(error, t).join(' ');
}
