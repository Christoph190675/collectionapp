import { inject } from '@angular/core';
import { CanActivateFn, Router } from '@angular/router';

import { AuthService } from '../services/auth.service';

/** Reserved screens: a visitor who is not logged in goes to the login screen (FR-016). */
export const authGuard: CanActivateFn = async () => {
  const authService = inject(AuthService);
  const router = inject(Router);

  if (authService.currentUser()) {
    return true;
  }
  return (await authService.loadCurrentUser()) ? true : router.createUrlTree(['/login']);
};
