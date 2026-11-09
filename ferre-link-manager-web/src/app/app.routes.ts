import { Routes } from '@angular/router';
import { adminGuard, authGuard, guestGuard } from './core/auth/auth.guards';

export const routes: Routes = [
  {
    path: 'login',
    canActivate: [guestGuard],
    loadComponent: () => import('./features/auth/login/login').then((m) => m.Login),
  },
  {
    path: 'register',
    canActivate: [guestGuard],
    loadComponent: () => import('./features/auth/register/register').then((m) => m.Register),
  },
  {
    path: 'verify-email',
    loadComponent: () =>
      import('./features/auth/verify-email/verify-email').then((m) => m.VerifyEmail),
  },
  {
    // Everything below needs a signed-in user and is shown inside the shell (top bar).
    path: '',
    canActivate: [authGuard],
    loadComponent: () => import('./layout/shell/shell').then((m) => m.Shell),
    children: [
      {
        path: '',
        pathMatch: 'full',
        loadComponent: () => import('./features/home/home').then((m) => m.Home),
      },
      {
        path: 'admin',
        canActivate: [adminGuard],
        loadComponent: () => import('./features/admin/admin-home').then((m) => m.AdminHome),
        children: [
          { path: '', pathMatch: 'full', redirectTo: 'categories' },
          {
            path: 'categories',
            loadComponent: () =>
              import('./features/admin/categories/categories-page').then((m) => m.CategoriesPage),
          },
          {
            path: 'links',
            loadComponent: () =>
              import('./features/admin/links/links-page').then((m) => m.LinksPage),
          },
          {
            path: 'users',
            loadComponent: () =>
              import('./features/admin/users/users-page').then((m) => m.UsersPage),
          },
        ],
      },
    ],
  },
  { path: '**', redirectTo: '' },
];