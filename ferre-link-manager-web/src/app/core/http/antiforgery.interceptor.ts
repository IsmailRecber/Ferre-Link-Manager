import {
  HttpInterceptorFn,
  HttpRequest,
  HttpResponse,
  HttpXsrfTokenExtractor,
} from '@angular/common/http';
import { inject } from '@angular/core';
import { filter, switchMap, take } from 'rxjs';

const SAFE_METHODS = ['GET', 'HEAD', 'OPTIONS'];
const HEADER_NAME = 'X-XSRF-TOKEN';

/**
 * Before every state-changing API request, asks the API for a fresh antiforgery token and then
 * puts the NEW token into the request header. The token belongs to the current user, so it must
 * be refreshed after login and logout. The header is set explicitly here because Angular's own
 * XSRF support may have copied an old cookie value into the request before this interceptor ran.
 */
export const antiforgeryInterceptor: HttpInterceptorFn = (req, next) => {
  const tokenExtractor = inject(HttpXsrfTokenExtractor);
  const needsToken = req.url.startsWith('/api/') && !SAFE_METHODS.includes(req.method);

  if (!needsToken) {
    return next(req);
  }

  return next(new HttpRequest('GET', '/api/antiforgery')).pipe(
    filter((event) => event instanceof HttpResponse),
    take(1),
    switchMap(() => {
      const token = tokenExtractor.getToken();

      return next(token ? req.clone({ setHeaders: { [HEADER_NAME]: token } }) : req);
    }),
  );
};