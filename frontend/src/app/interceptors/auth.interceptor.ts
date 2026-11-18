import { HttpErrorResponse, HttpInterceptorFn, HttpRequest } from '@angular/common/http';
import { inject } from '@angular/core';
import { Router } from '@angular/router';
import { catchError, from, switchMap, throwError } from 'rxjs';

import { environment } from '../../environments/environment';
import { AuthService } from '../services/auth.service';
import { TokenStorageService } from '../services/token-storage.service';

/** API calls that never carry nor renew the session. */
const ANONYMOUS_PATHS = ['api/auth/login', 'api/auth/register', 'api/auth/refresh', 'api/health'];

function isAnonymous(request: HttpRequest<unknown>): boolean {
  if (!request.url.startsWith(environment.apiUrl)) {
    return true;
  }
  const path = request.url.substring(environment.apiUrl.length).split('?')[0];
  return ANONYMOUS_PATHS.includes(path);
}

function withToken(request: HttpRequest<unknown>, accessToken: string | undefined): HttpRequest<unknown> {
  return accessToken ? request.clone({ setHeaders: { Authorization: `Bearer ${accessToken}` } }) : request;
}

/**
 * Sends the access token and, on a 401, renews the session once then replays the request (research R10).
 * If the session cannot be renewed, goes back to the login screen with "session expired" (US4-AC3).
 */
export const authInterceptor: HttpInterceptorFn = (request, next) => {
  if (isAnonymous(request)) {
    return next(request);
  }
  const storage = inject(TokenStorageService);
  const authService = inject(AuthService);
  const router = inject(Router);

  return from(storage.get()).pipe(
    switchMap((tokens) => next(withToken(request, tokens?.accessToken))),
    catchError((error: unknown) => {
      if (!(error instanceof HttpErrorResponse) || error.status !== 401) {
        return throwError(() => error);
      }
      return authService.refresh().pipe(
        catchError(() => {
          void authService
            .clearSession()
            .then(() => router.navigate(['/login'], { queryParams: { expired: 1 } }));
          return throwError(() => error);
        }),
        // Replayed only once: a new 401 is returned to the caller
        switchMap((session) => next(withToken(request, session.accessToken))),
      );
    }),
  );
};
