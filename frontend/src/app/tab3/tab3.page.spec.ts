import { signal } from '@angular/core';
import { ComponentFixture, TestBed } from '@angular/core/testing';

import { UserDto } from '../services/auth.models';
import { AuthService } from '../services/auth.service';
import { Tab3Page } from './tab3.page';

describe('Tab3Page', () => {
  const user: UserDto = { id: 1, lastName: 'Pinto', firstName: 'Christoph', email: 'test@example.fr', claims: ['User'] };

  let fixture: ComponentFixture<Tab3Page>;
  let currentUser: ReturnType<typeof signal<UserDto | null>>;
  let logout: ReturnType<typeof vi.fn>;

  const button = (): HTMLElement | null => fixture.nativeElement.querySelector('[data-testid="logout"]');

  beforeEach(async () => {
    currentUser = signal<UserDto | null>(user);
    logout = vi.fn(async () => undefined);
    await TestBed.configureTestingModule({
      imports: [Tab3Page],
      providers: [{ provide: AuthService, useValue: { currentUser, logout } }],
    }).compileComponents();

    fixture = TestBed.createComponent(Tab3Page);
    fixture.detectChanges();
  });

  it('US3-AC1 : « Se déconnecter » → logout du service', async () => {
    button()?.click();
    await fixture.whenStable();

    expect(logout).toHaveBeenCalledTimes(1);
  });

  it('non connecté → pas de bouton de déconnexion', () => {
    currentUser.set(null);
    fixture.detectChanges();

    expect(button()).toBeNull();
  });
});
