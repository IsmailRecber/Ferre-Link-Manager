import { HttpClient, HttpParams } from '@angular/common/http';
import { Injectable, inject } from '@angular/core';
import { firstValueFrom } from 'rxjs';

export interface AuditLogItem {
  id: string;
  action: string;
  entityType: string;
  entityId: string;
  entityName: string | null;
  entityUrl: string | null;
  userId: string | null;
  userEmail: string | null;
  occurredOnUtc: string;
}

export interface AuditLogPage {
  items: AuditLogItem[];
  totalCount: number;
  page: number;
  pageSize: number;
}

export interface AuditFilter {
  action: string | null;
  fromUtc: string | null;
  toUtc: string | null;
}

/** The actions the API knows, with the words the user sees. */
export const AUDIT_ACTIONS = [
  { value: 'Link.Created', label: 'Link oluşturuldu' },
  { value: 'Link.Updated', label: 'Link güncellendi' },
  { value: 'Link.Activated', label: 'Link aktif yapıldı' },
  { value: 'Link.Deactivated', label: 'Link pasif yapıldı' },
  { value: 'Link.Deleted', label: 'Link silindi' },
  { value: 'Category.Created', label: 'Kategori oluşturuldu' },
  { value: 'Category.Renamed', label: 'Kategori yeniden adlandırıldı' },
  { value: 'Category.Activated', label: 'Kategori aktif yapıldı' },
  { value: 'Category.Deactivated', label: 'Kategori pasif yapıldı' },
  { value: 'Category.Deleted', label: 'Kategori silindi' },
  { value: 'User.Activated', label: 'Kullanıcı aktif yapıldı' },
  { value: 'User.Deactivated', label: 'Kullanıcı pasif yapıldı' },
] as const;

export function actionLabel(action: string): string {
  return AUDIT_ACTIONS.find((option) => option.value === action)?.label ?? action;
}

function parseLocalDay(value: string, time: string): string | null {
  if (!/^\d{4}-\d{2}-\d{2}$/.test(value)) {
    return null;
  }

  const date = new Date(`${value}T${time}`);

  return Number.isNaN(date.getTime()) ? null : date.toISOString();
}

/** The first moment of a day (yyyy-mm-dd) in the user's time zone, as a UTC timestamp. */
export function startOfLocalDayIso(value: string): string | null {
  return parseLocalDay(value, '00:00:00.000');
}

/** The last moment of a day (yyyy-mm-dd) in the user's time zone, as a UTC timestamp. */
export function endOfLocalDayIso(value: string): string | null {
  return parseLocalDay(value, '23:59:59.999');
}

@Injectable({ providedIn: 'root' })
export class AuditAdminService {
  private readonly http = inject(HttpClient);

  getPage(page: number, pageSize: number, filter: AuditFilter): Promise<AuditLogPage> {
    let params = new HttpParams().set('page', page).set('pageSize', pageSize);

    if (filter.action) {
      params = params.set('action', filter.action);
    }

    if (filter.fromUtc) {
      params = params.set('from', filter.fromUtc);
    }

    if (filter.toUtc) {
      params = params.set('to', filter.toUtc);
    }

    return firstValueFrom(this.http.get<AuditLogPage>('/api/audit-logs', { params }));
  }
}