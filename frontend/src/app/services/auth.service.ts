import { HttpClient } from '@angular/common/http';
import { Injectable, inject, signal } from '@angular/core';
import { Router } from '@angular/router';
import { Observable, defer, finalize, firstValueFrom, from, map, shareReplay, switchMap, throwError } from 'rxjs';

import { environment } from '../../environments/environment';
import { AuthSession, LoginRequest, RegisterRequest, UserDto } from './auth.models';
import { TokenStorageService } from './token-storage.service';

export const AUTH_URL = `${environment.apiUrl}api/auth/`;

/** Only entry point to /api/auth/* (constitution VI): components never call HttpClient. */
@Injectable({ providedIn: 'root' })
export class AuthService {
  private readonly http = inject(HttpClient);
  private readonly storage = inject(TokenStorageService);
  private readonly router = inject(Router);

  /** Refresh in progress, shared by every request failing with 401 at the same time (R10). */
  private refreshInFlight: Observable<AuthSession> | null = null;
  private loadingUser: Promise<UserDto | null> | null = null;

  readonly currentUser = signal<UserDto | null>(null);

  register(request: RegisterRequest): Observable<UserDto> {
    return this.http.post<UserDto>(`${AUTH_URL}register`, request);
  }

  login(request: LoginRequest): Observable<UserDto> {
    return this.http.post<AuthSession>(`${AUTH_URL}login`, request).pipe(
      switchMap((session) => from(this.storeSession(session))),
    );
  }

  /** Renews the session with the stored refresh token (rotation: both tokens change). */
  refresh(): Observable<AuthSession> {
    if (!this.refreshInFlight) {
      this.refreshInFlight = defer(() => from(this.storage.get())).pipe(
        switchMap((tokens) =>
          tokens
            ? this.http.post<AuthSession>(`${AUTH_URL}refresh`, { refreshToken: tokens.refreshToken })
            : throwError(() => new Error('No refresh token')),
        ),
        switchMap((session) => from(this.storeSession(session)).pipe(map(() => session))),
        finalize(() => (this.refreshInFlight = null)),
        shareReplay(1),
      );
    }
    return this.refreshInFlight;
  }

  /** Revokes the session of this device; the local session is cleared even if the call fails. */
  async logout(): Promise<void> {
    const tokens = await this.storage.get();
    try {
      if (tokens) {
        await firstValueFrom(this.http.post<void>(`${AUTH_URL}logout`, { refreshToken: tokens.refreshToken }));
      }
    } catch {
      // Offline or session already expired: nothing more to revoke from here
    } finally {
      await this.clearSession();
      await this.router.navigateByUrl('/login');
    }
  }

  /** At startup: restores the user if the stored session is still valid (US2-AC7). */
  loadCurrentUser(): Promise<UserDto | null> {
    // Called at startup and by the guard of the first screen: a single /me request
    this.loadingUser ??= this.fetchCurrentUser().finally(() => (this.loadingUser = null));
    return this.loadingUser;
  }

  private async fetchCurrentUser(): Promise<UserDto | null> {
    if (!(await this.storage.get())) {
      this.currentUser.set(null);
      return null;
    }
    try {
      const user = await firstValueFrom(this.http.get<UserDto>(`${AUTH_URL}me`));
      this.currentUser.set(user);
      return user;
    } catch {
      this.currentUser.set(null);
      return null;
    }
  }

  async clearSession(): Promise<void> {
    await this.storage.clear();
    this.currentUser.set(null);
  }

  private async storeSession(session: AuthSession): Promise<UserDto> {
    await this.storage.save({ accessToken: session.accessToken, refreshToken: session.refreshToken });
    this.currentUser.set(session.user);
    return session.user;
  }
}
