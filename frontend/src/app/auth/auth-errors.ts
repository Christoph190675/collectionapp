import { HttpErrorResponse } from '@angular/common/http';

import { isApiError } from '../services/auth.models';

export const UNREACHABLE_MESSAGE = "Impossible de joindre le serveur. Vérifiez votre connexion et réessayez.";
const UNEXPECTED_MESSAGE = 'Une erreur inattendue est survenue. Réessayez.';

export interface FormErrors {
  /** Error shown above the form (wrong credentials, locked account, API unreachable…) */
  global: string | null;
  /** Errors returned by the API for a given field (camelCase name) */
  fields: Record<string, string>;
}

/** Translates an API failure into messages for the form (FR-017): the API messages are shown as is. */
export function toFormErrors(error: unknown): FormErrors {
  if (error instanceof HttpErrorResponse) {
    if (error.status === 0) {
      return { global: UNREACHABLE_MESSAGE, fields: {} };
    }
    if (isApiError(error.error)) {
      const fields: Record<string, string> = {};
      for (const field of error.error.fields ?? []) {
        fields[field.field] ??= field.message;
      }
      return { global: error.error.fields?.length ? null : error.error.message, fields };
    }
  }
  return { global: UNEXPECTED_MESSAGE, fields: {} };
}
