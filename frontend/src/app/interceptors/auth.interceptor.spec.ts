import { HttpClient, HttpErrorResponse, provideHttpClient, withInterceptors } from '@angular/common/http';
import { HttpTestingController, provideHttpClientTesting } from '@angular/common/http/testing';
import { TestBed } from '@angular/core/testing';
import { Router, provideRouter } from '@angular/router';
import { Observable, Subject, firstValueFrom, throwError } from 'rxjs';

import { environment } from '../../environments/environment';
import { AuthSession, StoredTokens, UserDto } from '../services/auth.models';
import { AuthService } from '../services/auth.service';
import { TokenStorageService } from '../services/token-storage.service';
import { authInterceptor } from './auth.interceptor';

describe('authInterceptor', () => {
  const api = environment.apiUrl;
  const user: UserDto = { id: 1, lastName: 'Pinto', firstName: 'Christoph', email: 'test@example.fr', claims: ['User'] };
  const renewed: AuthSession = {
    accessToken: 'access-2',
    accessTokenExpiresAt: '2026-11-18T10:30:00Z',
    refreshToken: 'refresh-2',
    refreshTokenExpiresAt: '2026-11-25T10:00:00Z',
    user,
  };

  let http: HttpClient;
  let httpMock: HttpTestingController;
  let stored: StoredTokens | null;
  let refreshSubject: Subject<AuthSession>;
  let refresh: ReturnType<typeof vi.fn<() => Observable<AuthSession>>>;
  let clearSession: ReturnType<typeof vi.fn>;
  let navigate: ReturnType<typeof vi.spyOn>;

  const flushMicrotasks = () => new Promise((resolve) => setTimeout(resolve));
  const unauthorized = { status: 401, statusText: 'Unauthorized' };

  beforeEach(() => {
    stored = { accessToken: 'access-1', refreshToken: 'refresh-1' };
    refreshSubject = new Subject<AuthSession>();
    refresh = vi.fn<() => Observable<AuthSession>>(() => refreshSubject);
    clearSession = vi.fn(async () => undefined);
    TestBed.configureTestingModule({
      providers: [
        provideHttpClient(withInterceptors([authInterceptor])),
        provideHttpClientTesting(),
        provideRouter([]),
        { provide: TokenStorageService, useValue: { get: vi.fn(async () => stored) } },
        { provide: AuthService, useValue: { refresh, clearSession } },
      ],
    });
    http = TestBed.inject(HttpClient);
    httpMock = TestBed.inject(HttpTestingController);
    navigate = vi.spyOn(TestBed.inject(Router), 'navigate').mockResolvedValue(true);
  });

  afterEach(() => httpMock.verify());

  it('ajoute Authorization: Bearer sur un appel réservé', async () => {
    const result = firstValueFrom(http.get(`${api}api/auth/me`));
    await flushMicrotasks();

    const request = httpMock.expectOne(`${api}api/auth/me`);
    expect(request.request.headers.get('Authorization')).toBe('Bearer access-1');
    request.flush(user);
    await result;
  });

  it.each(['api/auth/login', 'api/auth/register', 'api/auth/refresh', 'api/health'])(
    "pas d'en-tête Authorization sur %s",
    (path) => {
      http.get(`${api}${path}`).subscribe();

      expect(httpMock.expectOne(`${api}${path}`).request.headers.has('Authorization')).toBe(false);
    },
  );

  it('401 → un seul refresh puis requête rejouée', async () => {
    const result = firstValueFrom(http.get<UserDto>(`${api}api/auth/me`));
    await flushMicrotasks();
    httpMock.expectOne(`${api}api/auth/me`).flush(null, unauthorized);

    refreshSubject.next(renewed);
    refreshSubject.complete();
    const replay = httpMock.expectOne(`${api}api/auth/me`);
    expect(replay.request.headers.get('Authorization')).toBe('Bearer access-2');
    replay.flush(user);

    expect(await result).toEqual(user);
    expect(refresh).toHaveBeenCalledTimes(1);
  });

  it('US2-AC7 : réouverture après expiration de l’access token → /me 401 → refresh → /me rejoué → connecté', async () => {
    const result = firstValueFrom(http.get<UserDto>(`${api}api/auth/me`));
    await flushMicrotasks();
    httpMock.expectOne(`${api}api/auth/me`).flush(null, unauthorized);
    refreshSubject.next(renewed);
    refreshSubject.complete();
    httpMock.expectOne(`${api}api/auth/me`).flush(user);

    expect(await result).toEqual(user);
    expect(navigate).not.toHaveBeenCalled();
  });

  it('3 requêtes en 401 simultanées → les 3 rejouées avec le même refresh', async () => {
    // AuthService.refresh() shares the call in progress: the interceptor relies on it
    const results = [1, 2, 3].map((id) => firstValueFrom(http.get(`${api}api/items/${id}`)));
    await flushMicrotasks();
    for (const id of [1, 2, 3]) {
      httpMock.expectOne(`${api}api/items/${id}`).flush(null, unauthorized);
    }

    refreshSubject.next(renewed);
    refreshSubject.complete();
    for (const id of [1, 2, 3]) {
      httpMock.expectOne(`${api}api/items/${id}`).flush({ id });
    }

    expect(await Promise.all(results)).toEqual([{ id: 1 }, { id: 2 }, { id: 3 }]);
  });

  it('US4-AC3 : refresh en échec → stockage vidé + /login?expired=1', async () => {
    refresh.mockReturnValue(throwError(() => new HttpErrorResponse(unauthorized)));

    const result = firstValueFrom(http.get(`${api}api/auth/me`)).catch((error: unknown) => error);
    await flushMicrotasks();
    httpMock.expectOne(`${api}api/auth/me`).flush(null, unauthorized);

    expect(((await result) as HttpErrorResponse).status).toBe(401);
    await vi.waitFor(() => expect(navigate).toHaveBeenCalledWith(['/login'], { queryParams: { expired: 1 } }));
    expect(clearSession).toHaveBeenCalled();
  });

  it('autre erreur que 401 → transmise sans refresh', async () => {
    const result = firstValueFrom(http.get(`${api}api/auth/me`)).catch((error: unknown) => error);
    await flushMicrotasks();
    httpMock.expectOne(`${api}api/auth/me`).flush(null, { status: 500, statusText: 'Error' });

    expect(((await result) as HttpErrorResponse).status).toBe(500);
    expect(refresh).not.toHaveBeenCalled();
  });
});
