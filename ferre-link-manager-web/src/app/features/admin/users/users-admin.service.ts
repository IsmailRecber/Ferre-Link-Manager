import { HttpClient } from '@angular/common/http';
import { Injectable, inject } from '@angular/core';
import { firstValueFrom } from 'rxjs';

export interface AdminUser {
  id: string;
  email: string;
  isActive: boolean;
  emailConfirmed: boolean;
  roles: string[];
}

@Injectable({ providedIn: 'root' })
export class UsersAdminService {
  private readonly http = inject(HttpClient);

  getAll(): Promise<AdminUser[]> {
    return firstValueFrom(this.http.get<AdminUser[]>('/api/users'));
  }

  async setActive(id: string, isActive: boolean): Promise<void> {
    await firstValueFrom(this.http.put(`/api/users/${encodeURIComponent(id)}/active`, { isActive }));
  }
}