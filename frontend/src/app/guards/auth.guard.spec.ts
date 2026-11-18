import { signal } from '@angular/core';
import { TestBed } from '@angular/core/testing';
import { Router, UrlTree, provideRouter } from '@angular/router';

import { routes } from '../tabs/tabs.routes';
import { UserDto } from '../services/auth.models';
import { AuthService } from '../services/auth.service';
import { authGuard } from './auth.guard';

describe('authGuard', () => {
  const user: UserDto = { id: 1, lastName: 'Pinto', firstName: 'Christoph', email: 'test@example.fr', claims: ['User'] };

  let currentUser: ReturnType<typeof signal<UserDto | null>>;
  let loadCurrentUser: ReturnType<typeof vi.fn<() => Promise<UserDto | null>>>;

  function run(): Promise<boolean | UrlTree> {
    return TestBed.runInInjectionContext(() => authGuard({} as never, {} as never)) as Promise<boolean | UrlTree>;
  }

  beforeEach(() => {
    currentUser = signal<UserDto | null>(null);
    loadCurrentUser = vi.fn<() => Promise<UserDto | null>>(async () => null);
    TestBed.configureTestingModule({
      providers: [provideRouter([]), { provide: AuthService, useValue: { currentUser, loadCurrentUser } }],
    });
  });

  it('US4-AC1 : non connecté → redirection /login', async () => {
    const result = await run();

    expect(TestBed.inject(Router).serializeUrl(result as UrlTree)).toBe('/login');
  });

  it('connecté → accès autorisé', async () => {
    currentUser.set(user);

    expect(await run()).toBe(true);
    expect(loadCurrentUser).not.toHaveBeenCalled();
  });

  it('session stockée encore valide → accès autorisé', async () => {
    loadCurrentUser.mockResolvedValue(user);

    expect(await run()).toBe(true);
  });

  it('US4-AC2 : la route status est accessible sans connexion, les onglets réservés sont gardés', () => {
    const children = routes[0].children ?? [];
    const guarded = (path: string) => children.find((route) => route.path === path)?.canActivate ?? [];

    expect(guarded('status')).toEqual([]);
    for (const tab of ['tab1', 'tab2', 'tab3']) {
      expect(guarded(tab)).toEqual([authGuard]);
    }
  });
});
