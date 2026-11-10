import { TestBed } from '@angular/core/testing';
import {
  ActivatedRouteSnapshot,
  CanActivateFn,
  Router,
  RouterStateSnapshot,
  UrlTree,
  provideRouter,
} from '@angular/router';
import { beforeEach, describe, expect, it } from 'vitest';
import { adminGuard, authGuard, guestGuard } from './auth.guards';
import { AuthService } from './auth.service';

const state = { signedIn: false, admin: false, failing: false };

// A stand-in for AuthService: the guards only need these two members.
const authStub = {
  ensureUser: async () => {
    if (state.failing) {
      throw new Error('network problem');
    }

    return state.signedIn
      ? { userId: '1', email: 'user@example.com', roles: state.admin ? ['Admin'] : ['User'] }
      : null;
  },
  isAdmin: () => state.admin,
};

const run = async (guard: CanActivateFn): Promise<boolean | UrlTree> =>
  (await TestBed.runInInjectionContext(() =>
    guard({} as ActivatedRouteSnapshot, {} as RouterStateSnapshot),
  )) as boolean | UrlTree;

const describeResult = (result: boolean | UrlTree): boolean | string =>
  typeof result === 'boolean' ? result : TestBed.inject(Router).serializeUrl(result);

describe('route guards', () => {
  beforeEach(() => {
    state.signedIn = false;
    state.admin = false;
    state.failing = false;

    TestBed.configureTestingModule({
      providers: [provideRouter([]), { provide: AuthService, useValue: authStub }],
    });
  });

  describe('authGuard', () => {
    it('sends a visitor who is not signed in to the login page', async () => {
      expect(describeResult(await run(authGuard))).toBe('/login');
    });

    it('lets a signed-in user in', async () => {
      state.signedIn = true;

      expect(describeResult(await run(authGuard))).toBe(true);
    });

    it('sends the visitor to the login page when the session check fails', async () => {
      state.failing = true;

      expect(describeResult(await run(authGuard))).toBe('/login');
    });
  });

  describe('adminGuard', () => {
    it('sends a user who is not an admin to the home page', async () => {
      state.signedIn = true;

      expect(describeResult(await run(adminGuard))).toBe('/');
    });

    it('lets an admin in', async () => {
      state.signedIn = true;
      state.admin = true;

      expect(describeResult(await run(adminGuard))).toBe(true);
    });

    it('sends the visitor to the login page when the session check fails', async () => {
      state.failing = true;

      expect(describeResult(await run(adminGuard))).toBe('/login');
    });
  });

  describe('guestGuard', () => {
    it('sends a signed-in user away from the login page', async () => {
      state.signedIn = true;

      expect(describeResult(await run(guestGuard))).toBe('/');
    });

    it('lets a visitor see the login page', async () => {
      expect(describeResult(await run(guestGuard))).toBe(true);
    });

    it('lets the visitor see the login page when the session check fails', async () => {
      state.failing = true;

      expect(describeResult(await run(guestGuard))).toBe(true);
    });
  });
});