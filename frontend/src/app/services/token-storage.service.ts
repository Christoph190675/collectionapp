import { Injectable } from '@angular/core';
import { Preferences } from '@capacitor/preferences';

import { StoredTokens } from './auth.models';

const ACCESS_TOKEN_KEY = 'accessToken';
const REFRESH_TOKEN_KEY = 'refreshToken';

/** Only place that reads or writes the session tokens (browser storage or native preferences). */
@Injectable({ providedIn: 'root' })
export class TokenStorageService {
  async get(): Promise<StoredTokens | null> {
    const [access, refresh] = await Promise.all([
      Preferences.get({ key: ACCESS_TOKEN_KEY }),
      Preferences.get({ key: REFRESH_TOKEN_KEY }),
    ]);
    return access.value && refresh.value ? { accessToken: access.value, refreshToken: refresh.value } : null;
  }

  async save(tokens: StoredTokens): Promise<void> {
    await Promise.all([
      Preferences.set({ key: ACCESS_TOKEN_KEY, value: tokens.accessToken }),
      Preferences.set({ key: REFRESH_TOKEN_KEY, value: tokens.refreshToken }),
    ]);
  }

  async clear(): Promise<void> {
    await Promise.all([Preferences.remove({ key: ACCESS_TOKEN_KEY }), Preferences.remove({ key: REFRESH_TOKEN_KEY })]);
  }
}
