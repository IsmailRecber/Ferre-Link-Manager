import { HttpClient } from '@angular/common/http';
import { Injectable, inject } from '@angular/core';
import { firstValueFrom } from 'rxjs';

export interface AdminLink {
  id: string;
  title: string;
  url: string;
  categoryId: string;
  categoryName: string;
  isActive: boolean;
  roles: string[];
}

export interface LinkFormValue {
  title: string;
  url: string;
  categoryId: string;
  roles: string[];
}

/** The role names are the ones the API knows. The labels are what the user sees. */
export const ROLE_OPTIONS = [
  { value: 'Admin', label: 'Yönetici' },
  { value: 'User', label: 'Kullanıcı' },
] as const;

export function roleLabel(role: string): string {
  return ROLE_OPTIONS.find((option) => option.value === role)?.label ?? role;
}

@Injectable({ providedIn: 'root' })
export class LinksAdminService {
  private readonly http = inject(HttpClient);

  getAll(): Promise<AdminLink[]> {
    return firstValueFrom(this.http.get<AdminLink[]>('/api/links'));
  }

  async create(value: LinkFormValue): Promise<void> {
    await firstValueFrom(this.http.post('/api/links', value));
  }

  async update(id: string, value: LinkFormValue): Promise<void> {
    await firstValueFrom(this.http.put(`/api/links/${encodeURIComponent(id)}`, value));
  }

  async setActive(id: string, isActive: boolean): Promise<void> {
    await firstValueFrom(this.http.put(`/api/links/${encodeURIComponent(id)}/active`, { isActive }));
  }

  async remove(id: string): Promise<void> {
    await firstValueFrom(this.http.delete(`/api/links/${encodeURIComponent(id)}`));
  }
}