import { TestBed } from '@angular/core/testing';
import { provideRouter } from '@angular/router';
import { AppComponent } from './app.component';
import { AuthService } from './services/auth.service';

describe('AppComponent', () => {
  it('should create the app', async () => {
    await TestBed.configureTestingModule({
      imports: [AppComponent],
      providers: [provideRouter([]), { provide: AuthService, useValue: { loadCurrentUser: vi.fn(async () => null) } }],
    }).compileComponents();

    const fixture = TestBed.createComponent(AppComponent);
    const app = fixture.componentInstance;
    expect(app).toBeTruthy();
  });

  it('US2-AC7 : au démarrage, la session stockée est rechargée', async () => {
    const loadCurrentUser = vi.fn(async () => null);
    await TestBed.configureTestingModule({
      imports: [AppComponent],
      providers: [provideRouter([]), { provide: AuthService, useValue: { loadCurrentUser } }],
    }).compileComponents();

    TestBed.createComponent(AppComponent);

    expect(loadCurrentUser).toHaveBeenCalledTimes(1);
  });
});
