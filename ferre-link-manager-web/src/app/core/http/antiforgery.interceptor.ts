import { HttpInterceptorFn, HttpRequest, HttpResponse } from '@angular/common/http';
import { filter, switchMap, take } from 'rxjs';

const SAFE_METHODS = ['GET', 'HEAD', 'OPTIONS'];

/**
 * Before every state-changing API request, asks the API for a fresh antiforgery token.
 * The token belongs to the current user, so it must be refreshed after login and logout.
 * Angular's built-in XSRF support then copies the XSRF-TOKEN cookie into the X-XSRF-TOKEN header.
 */
export const antiforgeryInterceptor: HttpInterceptorFn = (req, next) => {
  const needsToken = req.url.startsWith('/api/') && !SAFE_METHODS.includes(req.method);

  if (!needsToken) {
    return next(req);
  }

  return next(new HttpRequest('GET', '/api/antiforgery')).pipe(
    filter((event) => event instanceof HttpResponse),
    take(1),
    switchMap(() => next(req)),
  );
};