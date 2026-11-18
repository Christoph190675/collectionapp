import { HttpErrorResponse } from '@angular/common/http';
import { ComponentFixture, TestBed } from '@angular/core/testing';
import { Router, provideRouter } from '@angular/router';
import { Observable, Subject, of, throwError } from 'rxjs';

import { LoginRequest, UserDto } from '../../services/auth.models';
import { AuthService } from '../../services/auth.service';
import { UNREACHABLE_MESSAGE } from '../auth-errors';
import { HOME_URL, LoginPage, SESSION_EXPIRED_MESSAGE } from './login.page';

describe('LoginPage', () => {
  const user: UserDto = { id: 1, lastName: 'Pinto', firstName: 'Christoph', email: 'test@example.fr', claims: ['User'] };
  const credentials: LoginRequest = { email: 'test@example.fr', password: 'Collec2026' };

  let fixture: ComponentFixture<LoginPage>;
  let page: LoginPage;
  let login: ReturnType<typeof vi.fn<(request: LoginRequest) => Observable<UserDto>>>;
  let navigate: ReturnType<typeof vi.spyOn>;

  const text = (testId: string): string =>
    fixture.nativeElement.querySelector(`[data-testid="${testId}"]`)?.textContent?.trim() ?? '';

  function submit(values: Partial<LoginRequest> = {}): void {
    page.form.setValue({ ...credentials, ...values });
    page.submit();
    fixture.detectChanges();
  }

  function apiError(status: number, message: string, code: string): HttpErrorResponse {
    return new HttpErrorResponse({ status, error: { code, message, status } });
  }

  beforeEach(async () => {
    login = vi.fn<(request: LoginRequest) => Observable<UserDto>>(() => of(user));
    await TestBed.configureTestingModule({
      imports: [LoginPage],
      providers: [provideRouter([]), { provide: AuthService, useValue: { login } }],
    }).compileComponents();

    navigate = vi.spyOn(TestBed.inject(Router), 'navigateByUrl').mockResolvedValue(true);
    fixture = TestBed.createComponent(LoginPage);
    page = fixture.componentInstance;
    fixture.detectChanges();
  });

  it("US2-AC1 : connexion réussie → redirection vers l'accueil", () => {
    submit();

    expect(login).toHaveBeenCalledWith(credentials);
    expect(navigate).toHaveBeenCalledWith(HOME_URL);
  });

  it('US2-AC2/AC3 : 401 → message générique', () => {
    login.mockReturnValue(throwError(() => apiError(401, 'E-mail ou mot de passe incorrect', 'UnauthorizedException')));

    submit({ password: 'Wrong2026' });

    expect(text('global-error')).toBe('E-mail ou mot de passe incorrect');
    expect(page.form.controls.password.value).toBe('');
    expect(navigate).not.toHaveBeenCalled();
  });

  it('US2-AC4 : 423 → message compte temporairement bloqué', () => {
    login.mockReturnValue(
      throwError(() =>
        apiError(423, 'Compte temporairement bloqué. Réessayez dans 15 minutes.', 'LockedException'),
      ),
    );

    submit();

    expect(text('global-error')).toBe('Compte temporairement bloqué. Réessayez dans 15 minutes.');
  });

  it('EdgeCase : API injoignable → message clair', () => {
    login.mockReturnValue(throwError(() => new HttpErrorResponse({ status: 0 })));

    submit();

    expect(text('global-error')).toBe(UNREACHABLE_MESSAGE);
    expect(page.form.controls.email.value).toBe(credentials.email);
  });

  it('champs vides → aucune requête', () => {
    submit({ email: '', password: '' });

    expect(login).not.toHaveBeenCalled();
    expect(text('error-email')).toBe('Ce champ est obligatoire');
  });

  it('EdgeCase : double appui → une seule requête', () => {
    login.mockReturnValue(new Subject<UserDto>());
    page.form.setValue(credentials);

    page.submit();
    page.submit();

    expect(login).toHaveBeenCalledTimes(1);
  });

  it('US4-AC3 : ?expired=1 → message « Votre session a expiré »', () => {
    fixture.componentRef.setInput('expired', '1');
    fixture.detectChanges();

    expect(text('session-expired')).toBe(SESSION_EXPIRED_MESSAGE);
  });
});
