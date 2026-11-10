import { HttpClient } from '@angular/common/http';
import { Injectable, inject } from '@angular/core';
import { firstValueFrom } from 'rxjs';

export interface Category {
  id: string;
  name: string;
  isActive: boolean;
  /** Links in this category, including inactive ones. A category with links cannot be deleted. */
  linkCount: number;
}

@Injectable({ providedIn: 'root' })
export class CategoriesService {
  private readonly http = inject(HttpClient);

  getAll(): Promise<Category[]> {
    return firstValueFrom(this.http.get<Category[]>('/api/categories'));
  }

  async create(name: string): Promise<void> {
    await firstValueFrom(this.http.post('/api/categories', { name }));
  }

  async update(id: string, name: string, isActive: boolean): Promise<void> {
    await firstValueFrom(this.http.put(`/api/categories/${encodeURIComponent(id)}`, { name, isActive }));
  }

  async remove(id: string): Promise<void> {
    await firstValueFrom(this.http.delete(`/api/categories/${encodeURIComponent(id)}`));
  }
}