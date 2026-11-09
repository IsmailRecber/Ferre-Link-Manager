import { HttpErrorResponse, HttpInterceptorFn } from '@angular/common/http';
import { inject } from '@angular/core';
import { Router } from '@angular/router';
import { catchError, throwError } from 'rxjs';
import { AuthService } from '../auth/auth.service';

// These calls answer 401 as a normal result (nobody signed in / wrong password).
const EXPECTED_401 = ['/api/auth/login', '/api/auth/me'];

/** When the session ends (for example it expired), clears the local state and goes to the login page. */
export const unauthorizedInterceptor: HttpInterceptorFn = (req, next) => {
  const auth = inject(AuthService);
  const router = inject(Router);

  return next(req).pipe(
    catchError((error: unknown) => {
      if (
        error instanceof HttpErrorResponse &&
        error.status === 401 &&
        req.url.startsWith('/api/') &&
        !EXPECTED_401.some((url) => req.url.startsWith(url))
      ) {
        auth.clearSession();
        void router.navigateByUrl('/login');
      }

      return throwError(() => error);
    }),
  );
};