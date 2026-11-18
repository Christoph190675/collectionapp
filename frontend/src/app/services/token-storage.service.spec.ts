import { TestBed } from '@angular/core/testing';
import { Preferences } from '@capacitor/preferences';

import { TokenStorageService } from './token-storage.service';

describe('TokenStorageService', () => {
  let service: TokenStorageService;

  beforeEach(async () => {
    await Preferences.clear();
    service = TestBed.inject(TokenStorageService);
  });

  it('save puis get → mêmes tokens', async () => {
    await service.save({ accessToken: 'a', refreshToken: 'r' });

    expect(await service.get()).toEqual({ accessToken: 'a', refreshToken: 'r' });
  });

  it('aucun token → null', async () => {
    expect(await service.get()).toBeNull();
  });

  it('clear → plus aucun token', async () => {
    await service.save({ accessToken: 'a', refreshToken: 'r' });

    await service.clear();

    expect(await service.get()).toBeNull();
  });
});
