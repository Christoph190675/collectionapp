import { HttpClient, HttpErrorResponse } from '@angular/common/http';
import { Injectable, inject } from '@angular/core';
import { Observable, TimeoutError, catchError, map, of, timeout } from 'rxjs';

import { environment } from '../../environments/environment';

export const HEALTH_TIMEOUT_MS = 5000;

export type HealthCheckError = 'unreachable' | 'timeout' | 'invalid-response';

export interface HealthCheckResult {
  api: 'ok' | 'ko';
  database: 'ok' | 'ko' | 'unknown';
  checkedAt: Date;
  error?: HealthCheckError;
}

/** Body returned by GET /api/health with status 200 or 503 (see contracts/health.openapi.yaml). */
interface HealthBody {
  api: 'ok';
  database: 'ok' | 'ko';
  checkedAt: string;
}

function isHealthBody(body: unknown): body is HealthBody {
  if (typeof body !== 'object' || body === null) {
    return false;
  }
  const candidate = body as Record<string, unknown>;
  return (
    candidate['api'] === 'ok' &&
    (candidate['database'] === 'ok' || candidate['database'] === 'ko') &&
    typeof candidate['checkedAt'] === 'string' &&
    !Number.isNaN(Date.parse(candidate['checkedAt']))
  );
}

function fromBody(body: HealthBody): HealthCheckResult {
  return { api: 'ok', database: body.database, checkedAt: new Date(body.checkedAt) };
}

function apiKo(error: HealthCheckError): HealthCheckResult {
  return { api: 'ko', database: 'unknown', checkedAt: new Date(), error };
}

function fromError(error: unknown): HealthCheckResult {
  if (error instanceof TimeoutError) {
    return apiKo('timeout');
  }
  if (error instanceof HttpErrorResponse) {
    // A 503 with a valid body means the API answers but the database is KO: never "unreachable"
    if (error.status === 503 && isHealthBody(error.error)) {
      return fromBody(error.error);
    }
    if (error.status === 0) {
      return apiKo('unreachable');
    }
  }
  return apiKo('invalid-response');
}

@Injectable({ providedIn: 'root' })
export class HealthService {
  private readonly http = inject(HttpClient);

  /** Never errors: every failure is translated into a result with api = 'ko'. */
  check(): Observable<HealthCheckResult> {
    return this.http.get<unknown>(`${environment.apiUrl}api/health`).pipe(
      timeout(HEALTH_TIMEOUT_MS),
      map((body) => (isHealthBody(body) ? fromBody(body) : apiKo('invalid-response'))),
      catchError((error: unknown) => of(fromError(error))),
    );
  }
}
