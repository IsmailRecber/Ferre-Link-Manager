import { Routes } from '@angular/router';
import { adminGuard, authGuard, guestGuard } from './core/auth/auth.guards';

const SUFFIX = ' - Ferre Link Manager';

export const routes: Routes = [
  {
    path: 'login',
    title: 'Giriş yap' + SUFFIX,
    canActivate: [guestGuard],
    loadComponent: () => import('./features/auth/login/login').then((m) => m.Login),
  },
  {
    path: 'register',
    title: 'Hesap oluştur' + SUFFIX,
    canActivate: [guestGuard],
    loadComponent: () => import('./features/auth/register/register').then((m) => m.Register),
  },
  {
    path: 'verify-email',
    title: 'E-posta doğrulama' + SUFFIX,
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
        title: 'Linklerim' + SUFFIX,
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
            title: 'Kategoriler - Yönetim' + SUFFIX,
            loadComponent: () =>
              import('./features/admin/categories/categories-page').then((m) => m.CategoriesPage),
          },
          {
            path: 'links',
            title: 'Linkler - Yönetim' + SUFFIX,
            loadComponent: () =>
              import('./features/admin/links/links-page').then((m) => m.LinksPage),
          },
          {
            path: 'users',
            title: 'Kullanıcılar - Yönetim' + SUFFIX,
            loadComponent: () =>
              import('./features/admin/users/users-page').then((m) => m.UsersPage),
          },
          {
            path: 'audit',
            title: 'Denetim kayıtları - Yönetim' + SUFFIX,
            loadComponent: () =>
              import('./features/admin/audit/audit-page').then((m) => m.AuditPage),
          },
        ],
      },
    ],
  },
  {
    path: '**',
    title: 'Sayfa bulunamadı' + SUFFIX,
    loadComponent: () => import('./features/not-found/not-found').then((m) => m.NotFound),
  },
];