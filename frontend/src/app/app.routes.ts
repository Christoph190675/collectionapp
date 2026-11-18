import { Routes } from '@angular/router';

export const routes: Routes = [
  // Public screens (FR-015); the tabs apply their own guard
  {
    path: 'register',
    loadComponent: () => import('./auth/register/register.page').then((m) => m.RegisterPage),
  },
  {
    path: '',
    loadChildren: () => import('./tabs/tabs.routes').then((m) => m.routes),
  },
];
