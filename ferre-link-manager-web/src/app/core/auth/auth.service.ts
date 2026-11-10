import { HttpClient, HttpErrorResponse } from '@angular/common/http';
import { Injectable, computed, inject, signal } from '@angular/core';
import { firstValueFrom } from 'rxjs';

export interface CurrentUser {
  userId: string;
  email: string | null;
  roles: string[];
}

export interface RegisterResponse {
  userId: string;
}

@Injectable({ providedIn: 'root' })
export class AuthService {
  private readonly http = inject(HttpClient);
  private readonly currentUser = signal<CurrentUser | null>(null);
  private loaded = false;

  readonly user = this.currentUser.asReadonly();
  readonly isAdmin = computed(() => this.currentUser()?.roles.includes('Admin') ?? false);

  /** Returns the signed-in user. The API is asked only once, the answer is kept in memory. */
  async ensureUser(): Promise<CurrentUser | null> {
    return this.loaded ? this.currentUser() : this.loadCurrentUser();
  }

  /** Asks the API who is signed in. Returns null when nobody is. */
  async loadCurrentUser(): Promise<CurrentUser | null> {
    try {
      const user = await firstValueFrom(this.http.get<CurrentUser>('/api/auth/me'));
      this.currentUser.set(user);
      this.loaded = true;
      return user;
    } catch (error) {
      if (error instanceof HttpErrorResponse && error.status === 401) {
        this.clearSession();
        return null;
      }

      throw error;
    }
  }

  async login(email: string, password: string): Promise<void> {
    await firstValueFrom(this.http.post('/api/auth/login', { email, password }));
    await this.loadCurrentUser();
  }

  async logout(): Promise<void> {
    await firstValueFrom(this.http.post('/api/auth/logout', {}));
    this.clearSession();
  }

  /** Forgets the signed-in user locally. The server session is not touched. */
  clearSession(): void {
    this.currentUser.set(null);
    this.loaded = true;
  }

  register(email: string, password: string): Promise<RegisterResponse> {
    return firstValueFrom(this.http.post<RegisterResponse>('/api/auth/register', { email, password }));
  }

  async confirmEmail(userId: string, token: string): Promise<void> {
    await firstValueFrom(this.http.post('/api/auth/confirm-email', { userId, token }));
  }

  async resendVerification(userId: string): Promise<void> {
    await firstValueFrom(this.http.post('/api/auth/resend-verification', { userId }));
  }

    async resendVerificationByEmail(email: string): Promise<void> {
    await firstValueFrom(this.http.post('/api/auth/resend-verification-by-email', { email }));
  }
}