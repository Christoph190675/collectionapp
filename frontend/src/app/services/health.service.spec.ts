import { provideHttpClient } from '@angular/common/http';
import { HttpTestingController, provideHttpClientTesting } from '@angular/common/http/testing';
import { TestBed } from '@angular/core/testing';

import { environment } from '../../environments/environment';
import { HEALTH_TIMEOUT_MS, HealthCheckResult, HealthService } from './health.service';

describe('HealthService', () => {
  const url = `${environment.apiUrl}api/health`;
  const now = new Date('2026-10-28T10:00:00.000Z');
  let service: HealthService;
  let httpMock: HttpTestingController;

  function check(): HealthCheckResult[] {
    const results: HealthCheckResult[] = [];
    service.check().subscribe({
      next: (result) => results.push(result),
      error: () => {
        throw new Error('check() must never error');
      },
    });
    return results;
  }

  beforeEach(() => {
    vi.useFakeTimers();
    vi.setSystemTime(now);
    TestBed.configureTestingModule({
      providers: [provideHttpClient(), provideHttpClientTesting()],
    });
    service = TestBed.inject(HealthService);
    httpMock = TestBed.inject(HttpTestingController);
  });

  afterEach(() => {
    httpMock.verify();
    vi.useRealTimers();
  });

  it('US1-AC1 : 200 valide → API ok, base ok', () => {
    const results = check();

    const req = httpMock.expectOne(url);
    expect(req.request.method).toBe('GET');
    req.flush({ api: 'ok', database: 'ok', checkedAt: '2026-10-28T09:15:42.123Z' });

    expect(results).toEqual([
      { api: 'ok', database: 'ok', checkedAt: new Date('2026-10-28T09:15:42.123Z') },
    ]);
  });

  it('US2-AC1 : 503 avec corps valide → API ok, base ko', () => {
    const results = check();

    httpMock
      .expectOne(url)
      .flush(
        { api: 'ok', database: 'ko', checkedAt: '2026-10-28T09:15:42.123Z' },
        { status: 503, statusText: 'Service Unavailable' },
      );

    expect(results).toEqual([
      { api: 'ok', database: 'ko', checkedAt: new Date('2026-10-28T09:15:42.123Z') },
    ]);
  });

  it('US2-AC2 : erreur réseau (status 0) → API ko, base unknown, error unreachable', () => {
    const results = check();

    httpMock.expectOne(url).error(new ProgressEvent('error'));

    expect(results).toEqual([{ api: 'ko', database: 'unknown', checkedAt: now, error: 'unreachable' }]);
  });

  it('Edge : pas de réponse en 5 s → error timeout', () => {
    const results = check();
    const req = httpMock.expectOne(url);

    vi.advanceTimersByTime(HEALTH_TIMEOUT_MS - 1);
    expect(results).toEqual([]);

    vi.advanceTimersByTime(1);
    expect(results).toEqual([{ api: 'ko', database: 'unknown', checkedAt: expect.any(Date), error: 'timeout' }]);
    expect(req.cancelled).toBe(true);
  });

  it('Edge : 500 → error invalid-response', () => {
    const results = check();

    httpMock.expectOne(url).flush('boom', { status: 500, statusText: 'Internal Server Error' });

    expect(results).toEqual([{ api: 'ko', database: 'unknown', checkedAt: now, error: 'invalid-response' }]);
  });

  it('Edge : 200 avec corps invalide → error invalid-response', () => {
    const results = check();

    httpMock.expectOne(url).flush({ api: 'ok', database: 'maybe', checkedAt: 'not-a-date' });

    expect(results).toEqual([{ api: 'ko', database: 'unknown', checkedAt: now, error: 'invalid-response' }]);
  });

  it('Edge : 503 sans corps conforme → error invalid-response', () => {
    const results = check();

    httpMock.expectOne(url).flush(null, { status: 503, statusText: 'Service Unavailable' });

    expect(results).toEqual([{ api: 'ko', database: 'unknown', checkedAt: now, error: 'invalid-response' }]);
  });
});
