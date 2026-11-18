import { provideHttpClient } from '@angular/common/http';
import { HttpTestingController, provideHttpClientTesting } from '@angular/common/http/testing';
import { TestBed } from '@angular/core/testing';
import { Router, provideRouter } from '@angular/router';
import { firstValueFrom } from 'rxjs';

import { AuthSession, StoredTokens, UserDto } from './auth.models';
import { AUTH_URL, AuthService } from './auth.service';
import { TokenStorageService } from './token-storage.service';

describe('AuthService', () => {
  const user: UserDto = { id: 1, lastName: 'Pinto', firstName: 'Christoph', email: 'test@example.fr', claims: ['User'] };
  const session: AuthSession = {
    accessToken: 'access-1',
    accessTokenExpiresAt: '2026-11-18T10:15:00Z',
    refreshToken: 'refresh-1',
    refreshTokenExpiresAt: '2026-11-25T10:00:00Z',
    user,
  };

  let service: AuthService;
  let httpMock: HttpTestingController;
  let stored: StoredTokens | null;
  let navigate: ReturnType<typeof vi.spyOn>;

  /** Lets the storage promises resolve before checking the pending requests. */
  const flushMicrotasks = () => new Promise((resolve) => setTimeout(resolve));

  beforeEach(() => {
    stored = null;
    TestBed.configureTestingModule({
      providers: [
        provideHttpClient(),
        provideHttpClientTesting(),
        provideRouter([]),
        {
          provide: TokenStorageService,
          useValue: {
            get: vi.fn(async () => stored),
            save: vi.fn(async (tokens: StoredTokens) => void (stored = tokens)),
            clear: vi.fn(async () => void (stored = null)),
          },
        },
      ],
    });
    service = TestBed.inject(AuthService);
    httpMock = TestBed.inject(HttpTestingController);
    navigate = vi.spyOn(TestBed.inject(Router), 'navigateByUrl').mockResolvedValue(true);
  });

  afterEach(() => httpMock.verify());

  it('US2-AC1 : connexion réussie → tokens stockés + utilisateur courant', async () => {
    const result = firstValueFrom(service.login({ email: user.email, password: 'Collec2026' }));

    httpMock.expectOne(`${AUTH_URL}login`).flush(session);

    expect(await result).toEqual(user);
    expect(stored).toEqual({ accessToken: 'access-1', refreshToken: 'refresh-1' });
    expect(service.currentUser()).toEqual(user);
  });

  it('US2-AC7 : au démarrage, tokens présents + /me 200 → utilisateur connecté', async () => {
    stored = { accessToken: 'access-1', refreshToken: 'refresh-1' };

    const result = service.loadCurrentUser();
    await flushMicrotasks();
    httpMock.expectOne(`${AUTH_URL}me`).flush(user);

    expect(await result).toEqual(user);
    expect(service.currentUser()).toEqual(user);
  });

  it('US2-AC7 : aucun token → aucun appel, pas connecté', async () => {
    expect(await service.loadCurrentUser()).toBeNull();
    expect(service.currentUser()).toBeNull();
  });

  it('refresh → nouveaux tokens stockés (rotation)', async () => {
    stored = { accessToken: 'access-1', refreshToken: 'refresh-1' };
    const renewed: AuthSession = { ...session, accessToken: 'access-2', refreshToken: 'refresh-2' };

    const result = firstValueFrom(service.refresh());
    await flushMicrotasks();
    const request = httpMock.expectOne(`${AUTH_URL}refresh`);
    expect(request.request.body).toEqual({ refreshToken: 'refresh-1' });
    request.flush(renewed);

    expect((await result).accessToken).toBe('access-2');
    expect(stored).toEqual({ accessToken: 'access-2', refreshToken: 'refresh-2' });
  });

  it('refresh simultanés → un seul appel /refresh', async () => {
    stored = { accessToken: 'access-1', refreshToken: 'refresh-1' };

    const first = firstValueFrom(service.refresh());
    const second = firstValueFrom(service.refresh());
    await flushMicrotasks();
    httpMock.expectOne(`${AUTH_URL}refresh`).flush(session);

    expect(await first).toEqual(session);
    expect(await second).toEqual(session);
  });

  it('US3-AC1 : se déconnecter → POST logout, stockage vidé, redirection /login', async () => {
    stored = { accessToken: 'access-1', refreshToken: 'refresh-1' };
    service.currentUser.set(user);

    const done = service.logout();
    await flushMicrotasks();
    const request = httpMock.expectOne(`${AUTH_URL}logout`);
    expect(request.request.body).toEqual({ refreshToken: 'refresh-1' });
    request.flush(null, { status: 204, statusText: 'No Content' });
    await done;

    expect(stored).toBeNull();
    expect(service.currentUser()).toBeNull();
    expect(navigate).toHaveBeenCalledWith('/login');
  });

  it('US3-AC1 : logout en échec réseau → stockage vidé quand même', async () => {
    stored = { accessToken: 'access-1', refreshToken: 'refresh-1' };

    const done = service.logout();
    await flushMicrotasks();
    httpMock.expectOne(`${AUTH_URL}logout`).error(new ProgressEvent('error'));
    await done;

    expect(stored).toBeNull();
    expect(navigate).toHaveBeenCalledWith('/login');
  });
});
