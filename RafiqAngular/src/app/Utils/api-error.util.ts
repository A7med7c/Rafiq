import { HttpErrorResponse } from '@angular/common/http';
import { ApiErrorBody } from '../Modles/api-response';

export function getApiErrorMessages(error: HttpErrorResponse , translations?: any): string[] {
  const body = error.error as ApiErrorBody | null;
if (body?.errors?.length) {
  return body.errors.map(message =>
    translations
      ? localizeKnownApiMessage(message, translations)
      : message
  );
}

if (body?.message) {
  return [
    translations
      ? localizeKnownApiMessage(body.message, translations)
      : body.message
  ];
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

export function localizeKnownApiMessage(message: string, translations: any): string {
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
    'An account with this phone number already exists.' : translations.login.phoneNumberAlreadyExist,
    'Google login successful.': translations.login?.googleLoginSuccess,
    'Invalid email / phone number or password.' : translations.login?.invalidEmailPassword,
  };

  if (map[normalized]) return map[normalized]!;

  // Handle variable backend messages like: "ApplicationUser with identifier 'x' was not found." -> map to a friendly message
  if (normalized.startsWith('ApplicationUser with identifier') && normalized.includes('was not found')) {
    return translations.family?.userNotFound ?? message;
  }

  // Handle variable height messages like: "Height must be between 30 and 300. You entered 400." -> map to friendly height message
  // Handle variable "<Field> must be between" messages by mapping to a
  // `validation.<field>Between` translation when available (e.g. "Weight must be between...").
  const betweenMatch = normalized.match(/^['"]?([A-Za-z]+)['"]?\s+must be between/i);
  if (betweenMatch) {
    const field = betweenMatch[1].toLowerCase();
    const key = `${field}Between`;
    return (translations.validation && (translations.validation as any)[key]) ?? message;
  }

  return message;
}
