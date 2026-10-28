import { ComponentFixture, TestBed } from '@angular/core/testing';
import { Observable, Subject, of } from 'rxjs';

import { HealthCheckResult, HealthService } from '../services/health.service';
import { StatusPage } from './status.page';

describe('StatusPage', () => {
  const checkedAt = new Date('2026-10-28T09:15:42.000Z');
  const allOk: HealthCheckResult = { api: 'ok', database: 'ok', checkedAt };
  const databaseKo: HealthCheckResult = { api: 'ok', database: 'ko', checkedAt };
  const apiKo = (error: HealthCheckResult['error']): HealthCheckResult => ({
    api: 'ko',
    database: 'unknown',
    checkedAt,
    error,
  });

  let fixture: ComponentFixture<StatusPage>;
  let page: StatusPage;
  let check: ReturnType<typeof vi.fn<() => Observable<HealthCheckResult>>>;

  const element = (testId: string): HTMLElement | null =>
    fixture.nativeElement.querySelector(`[data-testid="${testId}"]`);
  const text = (testId: string): string => element(testId)?.textContent?.replace(/\s+/g, ' ').trim() ?? '';

  function enter(): void {
    page.ionViewWillEnter();
    fixture.detectChanges();
  }

  beforeEach(async () => {
    check = vi.fn<() => Observable<HealthCheckResult>>(() => of(allOk));
    await TestBed.configureTestingModule({
      imports: [StatusPage],
      providers: [{ provide: HealthService, useValue: { check } }],
    }).compileComponents();

    fixture = TestBed.createComponent(StatusPage);
    page = fixture.componentInstance;
    fixture.detectChanges();
  });

  it('US1-AC1 : affiche API = OK et Base de données = OK', () => {
    enter();

    expect(check).toHaveBeenCalledTimes(1);
    expect(text('api-status')).toContain('API');
    expect(text('api-status')).toContain('OK');
    expect(text('database-status')).toContain('Base de données');
    expect(text('database-status')).toContain('OK');
    expect(text('checked-at')).toMatch(/^Dernière vérification : \d{2}:\d{2}:\d{2}$/);
    expect(element('api-error')).toBeNull();
  });

  it('US2-AC1 : base KO affichée', () => {
    check.mockReturnValue(of(databaseKo));

    enter();

    expect(text('api-status')).toContain('OK');
    expect(text('database-status')).toContain('KO');
  });

  it('US2-AC2 : API injoignable affichée sans plantage', () => {
    check.mockReturnValue(of(apiKo('unreachable')));

    expect(() => enter()).not.toThrow();

    expect(text('api-status')).toContain('KO');
    expect(text('api-error')).toBe('API injoignable');
    expect(text('database-status')).toContain('Inconnu');
  });

  it('Edge : messages timeout et réponse invalide', () => {
    check.mockReturnValue(of(apiKo('timeout')));
    enter();
    expect(text('api-error')).toBe('Délai dépassé (5 s)');

    check.mockReturnValue(of(apiKo('invalid-response')));
    page.refresh();
    fixture.detectChanges();
    expect(text('api-error')).toBe("Réponse invalide de l'API");
  });

  it("US3-AC1 : la relance met à jour l'état KO → OK", () => {
    check.mockReturnValueOnce(of(databaseKo)).mockReturnValueOnce(of(allOk));
    enter();
    expect(text('database-status')).toContain('KO');

    element('refresh')!.click();
    fixture.detectChanges();

    expect(check).toHaveBeenCalledTimes(2);
    expect(text('database-status')).toContain('OK');
  });

  it('US3-AC2 : indicateur et bouton désactivé pendant la vérification', () => {
    const pending = new Subject<HealthCheckResult>();
    check.mockReturnValue(pending);
    enter();

    expect(element('spinner')).not.toBeNull();
    expect((element('refresh') as HTMLElement & { disabled: boolean }).disabled).toBe(true);

    element('refresh')!.click();
    expect(check).toHaveBeenCalledTimes(1);

    pending.next(allOk);
    pending.complete();
    fixture.detectChanges();

    expect(element('spinner')).toBeNull();
    expect((element('refresh') as HTMLElement & { disabled: boolean }).disabled).toBe(false);
    expect(text('database-status')).toContain('OK');
  });

  it("Edge : sortie d'onglet pendant la vérification", () => {
    const pending = new Subject<HealthCheckResult>();
    check.mockReturnValue(pending);
    enter();

    page.ionViewWillLeave();
    fixture.detectChanges();

    expect(pending.observed).toBe(false);
    expect(() => {
      pending.next(allOk);
      fixture.detectChanges();
    }).not.toThrow();
    expect(page.result()).toBeNull();
    expect(page.loading()).toBe(false);
  });
});
