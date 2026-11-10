import { HttpClient, provideHttpClient, withInterceptors } from '@angular/common/http';
import { HttpTestingController, provideHttpClientTesting } from '@angular/common/http/testing';
import { TestBed } from '@angular/core/testing';
import { firstValueFrom } from 'rxjs';
import { afterEach, beforeEach, describe, expect, it } from 'vitest';
import { antiforgeryInterceptor } from './antiforgery.interceptor';

const NO_CONTENT = { status: 204, statusText: 'No Content' };

const setTokenCookie = (value: string): void => {
  document.cookie = `XSRF-TOKEN=${value}; path=/`;
};

describe('antiforgeryInterceptor', () => {
  let client: HttpClient;
  let http: HttpTestingController;

  beforeEach(() => {
    TestBed.configureTestingModule({
      providers: [
        provideHttpClient(withInterceptors([antiforgeryInterceptor])),
        provideHttpClientTesting(),
      ],
    });

    client = TestBed.inject(HttpClient);
    http = TestBed.inject(HttpTestingController);
  });

  afterEach(() => {
    http.verify();
    document.cookie = 'XSRF-TOKEN=; expires=Thu, 01 Jan 1970 00:00:00 GMT; path=/';
  });

  it('asks for a fresh token before a POST and sends the NEW token in the header', async () => {
    setTokenCookie('old-token');

    const done = firstValueFrom(client.post('/api/categories', { name: 'Finance' }));

    // The API answers the token request and, like the real one, renews the cookie.
    const tokenRequest = http.expectOne('/api/antiforgery');
    setTokenCookie('new-token');
    tokenRequest.flush(null, NO_CONTENT);

    const post = http.expectOne('/api/categories');
    expect(post.request.headers.get('X-XSRF-TOKEN')).toBe('new-token');
    post.flush({});

    await done;
  });

  it('does not ask for a token before a GET', async () => {
    const done = firstValueFrom(client.get('/api/links/my'));

    http.expectNone('/api/antiforgery');
    http.expectOne('/api/links/my').flush([]);

    await done;
  });

  it('leaves addresses outside the API alone', async () => {
    const done = firstValueFrom(client.post('/something-else', {}));

    http.expectNone('/api/antiforgery');
    http.expectOne('/something-else').flush({});

    await done;
  });
});