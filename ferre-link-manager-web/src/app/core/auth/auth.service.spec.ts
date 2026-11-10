import { provideHttpClient } from '@angular/common/http';
import { HttpTestingController, provideHttpClientTesting } from '@angular/common/http/testing';
import { TestBed } from '@angular/core/testing';
import { afterEach, beforeEach, describe, expect, it, vi } from 'vitest';
import { AuthService } from './auth.service';

const NO_CONTENT = { status: 204, statusText: 'No Content' };
const UNAUTHORIZED = { status: 401, statusText: 'Unauthorized' };

describe('AuthService', () => {
  let service: AuthService;
  let http: HttpTestingController;

  beforeEach(() => {
    TestBed.configureTestingModule({
      providers: [provideHttpClient(), provideHttpClientTesting()],
    });

    service = TestBed.inject(AuthService);
    http = TestBed.inject(HttpTestingController);
  });

  afterEach(() => http.verify());

  it('stores the signed-in user and knows when it is an admin', async () => {
    const promise = service.loadCurrentUser();
    http.expectOne('/api/auth/me').flush({ userId: '1', email: 'admin@example.com', roles: ['Admin'] });

    const user = await promise;

    expect(user?.email).toBe('admin@example.com');
    expect(service.user()?.email).toBe('admin@example.com');
    expect(service.isAdmin()).toBe(true);
  });

  it('is not an admin when the user only has the User role', async () => {
    const promise = service.loadCurrentUser();
    http.expectOne('/api/auth/me').flush({ userId: '2', email: 'user@example.com', roles: ['User'] });
    await promise;

    expect(service.isAdmin()).toBe(false);
  });

  it('treats a 401 answer as "nobody is signed in"', async () => {
    const promise = service.loadCurrentUser();
    http.expectOne('/api/auth/me').flush(null, UNAUTHORIZED);

    expect(await promise).toBeNull();
    expect(service.user()).toBeNull();
  });

  it('passes other errors on to the caller', async () => {
    const promise = service.loadCurrentUser();
    http.expectOne('/api/auth/me').flush(null, { status: 500, statusText: 'Server Error' });

    await expect(promise).rejects.toBeTruthy();
  });

  it('asks the API only once and then answers from memory', async () => {
    const first = service.ensureUser();
    http.expectOne('/api/auth/me').flush({ userId: '1', email: 'user@example.com', roles: ['User'] });
    await first;

    const second = await service.ensureUser();

    http.expectNone('/api/auth/me');
    expect(second?.email).toBe('user@example.com');
  });

  it('forgets the user after clearSession and does not ask the API again', async () => {
    const first = service.ensureUser();
    http.expectOne('/api/auth/me').flush({ userId: '1', email: 'user@example.com', roles: ['User'] });
    await first;

    service.clearSession();

    expect(service.user()).toBeNull();
    expect(await service.ensureUser()).toBeNull();
    http.expectNone('/api/auth/me');
  });

  it('signs in and then loads the user', async () => {
    const promise = service.login('user@example.com', 'Abc123');

    const loginRequest = http.expectOne('/api/auth/login');
    expect(loginRequest.request.method).toBe('POST');
    expect(loginRequest.request.body).toEqual({ email: 'user@example.com', password: 'Abc123' });
    loginRequest.flush(null, NO_CONTENT);

    // The second request is sent right after the first one finished.
    const meRequest = await vi.waitFor(() => http.expectOne('/api/auth/me'));
    meRequest.flush({ userId: '1', email: 'user@example.com', roles: ['User'] });

    await promise;

    expect(service.user()?.email).toBe('user@example.com');
  });

  it('clears the local user after a logout', async () => {
    const load = service.loadCurrentUser();
    http.expectOne('/api/auth/me').flush({ userId: '1', email: 'user@example.com', roles: ['User'] });
    await load;

    const promise = service.logout();
    const request = http.expectOne('/api/auth/logout');
    expect(request.request.method).toBe('POST');
    request.flush(null, NO_CONTENT);
    await promise;

    expect(service.user()).toBeNull();
  });
});