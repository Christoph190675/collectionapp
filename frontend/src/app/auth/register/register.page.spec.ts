import { HttpErrorResponse } from '@angular/common/http';
import { ComponentFixture, TestBed } from '@angular/core/testing';
import { Router, provideRouter } from '@angular/router';
import { ToastController } from '@ionic/angular';
import { Observable, Subject, of, throwError } from 'rxjs';

import { RegisterRequest, UserDto } from '../../services/auth.models';
import { AuthService } from '../../services/auth.service';
import { UNREACHABLE_MESSAGE } from '../auth-errors';
import { RegisterPage } from './register.page';

describe('RegisterPage', () => {
  const user: UserDto = { id: 1, lastName: 'Pinto', firstName: 'Christoph', email: 'test@example.fr', claims: ['User'] };
  const valid: RegisterRequest = {
    lastName: 'Pinto',
    firstName: 'Christoph',
    email: 'test@example.fr',
    password: 'Collec2026',
    passwordConfirmation: 'Collec2026',
  };

  let fixture: ComponentFixture<RegisterPage>;
  let page: RegisterPage;
  let register: ReturnType<typeof vi.fn<(request: RegisterRequest) => Observable<UserDto>>>;
  let present: ReturnType<typeof vi.fn>;
  let navigate: ReturnType<typeof vi.spyOn>;

  const text = (testId: string): string =>
    fixture.nativeElement.querySelector(`[data-testid="${testId}"]`)?.textContent?.trim() ?? '';

  function fill(values: Partial<RegisterRequest> = {}): void {
    page.form.setValue({ ...valid, ...values });
  }

  function submit(): void {
    page.submit();
    fixture.detectChanges();
  }

  function apiError(status: number, body: unknown): HttpErrorResponse {
    return new HttpErrorResponse({ status, error: body });
  }

  beforeEach(async () => {
    register = vi.fn<(request: RegisterRequest) => Observable<UserDto>>(() => of(user));
    present = vi.fn(async () => undefined);
    await TestBed.configureTestingModule({
      imports: [RegisterPage],
      providers: [
        provideRouter([]),
        { provide: AuthService, useValue: { register } },
        { provide: ToastController, useValue: { create: vi.fn(async () => ({ present })) } },
      ],
    }).compileComponents();

    navigate = vi.spyOn(TestBed.inject(Router), 'navigateByUrl').mockResolvedValue(true);
    fixture = TestBed.createComponent(RegisterPage);
    page = fixture.componentInstance;
    fixture.detectChanges();
  });

  it('US1-AC1 : inscription réussie → message + redirection /login', async () => {
    fill();

    submit();

    expect(register).toHaveBeenCalledWith(valid);
    await vi.waitFor(() => expect(navigate).toHaveBeenCalledWith('/login'));
    expect(present).toHaveBeenCalled();
  });

  it('US1-AC2 : 409 → message e-mail déjà utilisé', () => {
    register.mockReturnValue(
      throwError(() =>
        apiError(409, { code: 'AlreadyExistException', message: 'Cette adresse e-mail est déjà utilisée', status: 409 }),
      ),
    );
    fill();

    submit();

    expect(text('global-error')).toBe('Cette adresse e-mail est déjà utilisée');
    expect(navigate).not.toHaveBeenCalled();
  });

  it('US1-AC3 : 400 → erreurs affichées sous chaque champ', () => {
    register.mockReturnValue(
      throwError(() =>
        apiError(400, {
          code: 'ValidationException',
          message: 'Les données saisies sont invalides',
          status: 400,
          fields: [
            { field: 'email', message: "L'adresse e-mail n'est pas valide" },
            { field: 'password', message: 'Le mot de passe doit contenir une lettre et un chiffre' },
          ],
        }),
      ),
    );
    fill();

    submit();

    expect(text('error-email')).toBe("L'adresse e-mail n'est pas valide");
    expect(text('error-password')).toBe('Le mot de passe doit contenir une lettre et un chiffre');
    expect(text('global-error')).toBe('');
  });

  it('US1-AC3 : champ obligatoire vide → aucune requête, message sous le champ', () => {
    fill({ lastName: '' });

    submit();

    expect(register).not.toHaveBeenCalled();
    expect(text('error-lastName')).toBe('Ce champ est obligatoire');
  });

  it('US1-AC4 : confirmation différente → aucune requête', () => {
    fill({ passwordConfirmation: 'Collec2027' });

    submit();

    expect(register).not.toHaveBeenCalled();
    expect(text('error-passwordConfirmation')).toBe('La confirmation ne correspond pas au mot de passe');
  });

  it('EdgeCase : API injoignable → message, champs conservés sauf mot de passe', () => {
    register.mockReturnValue(throwError(() => apiError(0, null)));
    fill();

    submit();

    expect(text('global-error')).toBe(UNREACHABLE_MESSAGE);
    const values = page.form.getRawValue();
    expect(values.email).toBe(valid.email);
    expect(values.lastName).toBe(valid.lastName);
    expect(values.password).toBe('');
  });

  it('EdgeCase : double appui → une seule requête', () => {
    register.mockReturnValue(new Subject<UserDto>());
    fill();

    page.submit();
    page.submit();

    expect(register).toHaveBeenCalledTimes(1);
  });
});
