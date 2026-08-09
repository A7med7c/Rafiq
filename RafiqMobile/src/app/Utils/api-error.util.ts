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

export function localizeKnownApiMessage(message: string, translations: any): string {
  if (!message || !translations) return message;
  const normalized = message.trim();
  const map: Record<string, string | undefined> = {
    'Invalid email/Password': translations.login?.invalidEmailPassword,
    'Login Successfully': translations.login?.loginSuccess,
    'Record Saved Successfully': translations.records?.recordSavedSuccessfully,
    'Record saved successfully.': translations.records?.recordSavedSuccessfully,
    'The Document Already Exist': translations.records?.documentAlreadyExist,
    'Appointments validation Failed': translations.appointments?.validationFailed,
    'Please fill in all required fields': translations.family?.requiredFields,
    'Please fill in all required fields.': translations.family?.requiredFields,
    'Invalid email address': translations.validation?.invalidEmailAddress,
    'Invalid email address.': translations.validation?.invalidEmailAddress,
    'Edit phone number validation failed': translations.family?.editPhoneValidationFailed,
    'Weight must be between 1 and 500.': translations.validation?.weightBetween,
    'Height must be between 30 and 300.': translations.validation?.heightBetween,
    'Medicine name is required.': translations.validation?.medicineNameRequired,
    'Validation.MedicineNameIsRequired': translations.validation?.medicineNameRequired,
    'Dosage is required.': translations.validation?.dosageRequired,
    'Validation.DosageIsRequired': translations.validation?.dosageRequired,
    'Frequency is required.': translations.validation?.frequencyRequired,
    'Validation.FrequencyIsRequired': translations.validation?.frequencyRequired,
    'Duration is required.': translations.validation?.durationRequired,
    'Validation.DurationIsRequired': translations.validation?.durationRequired,
    'The selected time has already passed.': translations.validation?.selectedTimePassed,
    'Contacts validation failed.': translations.validation?.contactsValidationFailed,
    'Contacts validation failed': translations.validation?.contactsValidationFailed,
    'Medication Added Successfully': translations.medications?.medicationAddedSuccessfully,
    'You cannot invite yourself.': translations.family?.cannotInviteYourself,
    'An account with this phone number already exists.' : translations.login?.phoneNumberAlreadyExist,
    'Google login successful.': translations.login?.googleLoginSuccess,
    'Invalid email / phone number or password.' : translations.login?.invalidEmailPassword,
    'No laboratory tests could be extracted from the uploaded image.': translations.documentAnalysis?.noLabTestsExtracted ?? translations.records?.noLabTestsExtracted,
    'No laboratory tests could be extracted from the uploaded image': translations.documentAnalysis?.noLabTestsExtracted ?? translations.records?.noLabTestsExtracted,
  };

  if (map[normalized]) return map[normalized]!;

  if (normalized.startsWith('ApplicationUser with identifier') && normalized.includes('was not found')) {
    return translations.family?.userNotFound ?? message;
  }

  const betweenMatch = normalized.match(/^['"]?([A-Za-z]+)['"]?\s+must be between/i);
  if (betweenMatch) {
    const field = betweenMatch[1].toLowerCase();
    const key = `${field}Between`;
    return (translations.validation && (translations.validation as any)[key]) ?? message;
  }

  return message;
}
