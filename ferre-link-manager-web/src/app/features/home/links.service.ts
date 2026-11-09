import { HttpClient } from '@angular/common/http';
import { Injectable, inject } from '@angular/core';
import { firstValueFrom } from 'rxjs';

export interface LinkItem {
  id: string;
  title: string;
  categoryId: string;
  categoryName: string;
}

@Injectable({ providedIn: 'root' })
export class LinksService {
  private readonly http = inject(HttpClient);

  /** The links the signed-in user may open. The API never sends their addresses. */
  getMyLinks(): Promise<LinkItem[]> {
    return firstValueFrom(this.http.get<LinkItem[]>('/api/links/my'));
  }

  /**
   * Opens a link in a new tab. The browser asks the API, which reads the real address from the
   * database and redirects. The address is never known to this page.
   */
  open(id: string): void {
    window.open(`/api/links/${encodeURIComponent(id)}/go`, '_blank', 'noopener,noreferrer');
  }
}