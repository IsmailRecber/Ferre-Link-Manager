import { inject } from '@angular/core';
import { CanActivateFn, Router } from '@angular/router';
import { AuthService } from './auth.service';

/** Only signed-in users may enter. Everybody else is sent to the login page. */
export const authGuard: CanActivateFn = async () => {
  const auth = inject(AuthService);
  const router = inject(Router);

  try {
    return (await auth.ensureUser()) ? true : router.createUrlTree(['/login']);
  } catch {
    return router.createUrlTree(['/login']);
  }
};

/** Only admins may enter. This is a convenience: the API checks the role as well. */
export const adminGuard: CanActivateFn = async () => {
  const auth = inject(AuthService);
  const router = inject(Router);

  try {
    await auth.ensureUser();
  } catch {
    return router.createUrlTree(['/login']);
  }

  return auth.isAdmin() ? true : router.createUrlTree(['/']);
};

/** Login and register pages are not shown to users who are already signed in. */
export const guestGuard: CanActivateFn = async () => {
  const auth = inject(AuthService);
  const router = inject(Router);

  try {
    return (await auth.ensureUser()) ? router.createUrlTree(['/']) : true;
  } catch {
    return true;
  }
};