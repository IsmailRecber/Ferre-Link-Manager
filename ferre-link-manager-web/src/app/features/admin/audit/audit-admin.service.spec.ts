import { provideHttpClient } from '@angular/common/http';
import { HttpTestingController, provideHttpClientTesting } from '@angular/common/http/testing';
import { TestBed } from '@angular/core/testing';
import { afterEach, beforeEach, describe, expect, it } from 'vitest';
import {
  AuditAdminService,
  actionLabel,
  endOfLocalDayIso,
  startOfLocalDayIso,
} from './audit-admin.service';

const EMPTY_PAGE = { items: [], totalCount: 0, page: 1, pageSize: 20 };

describe('AuditAdminService', () => {
  let service: AuditAdminService;
  let http: HttpTestingController;

  beforeEach(() => {
    TestBed.configureTestingModule({
      providers: [provideHttpClient(), provideHttpClientTesting()],
    });

    service = TestBed.inject(AuditAdminService);
    http = TestBed.inject(HttpTestingController);
  });

  afterEach(() => http.verify());

  it('sends only the page and the page size when there is no filter', async () => {
    const promise = service.getPage(2, 20, { action: null, fromUtc: null, toUtc: null });

    const request = http.expectOne((req) => req.url === '/api/audit-logs');
    expect(request.request.params.get('page')).toBe('2');
    expect(request.request.params.get('pageSize')).toBe('20');
    expect(request.request.params.has('action')).toBe(false);
    expect(request.request.params.has('from')).toBe(false);
    expect(request.request.params.has('to')).toBe(false);
    request.flush(EMPTY_PAGE);

    await promise;
  });

  it('sends the action and the date range when they are set', async () => {
    const promise = service.getPage(1, 20, {
      action: 'Link.Deleted',
      fromUtc: '2026-11-09T21:00:00.000Z',
      toUtc: '2026-11-10T20:59:59.999Z',
    });

    const request = http.expectOne((req) => req.url === '/api/audit-logs');
    expect(request.request.params.get('action')).toBe('Link.Deleted');
    expect(request.request.params.get('from')).toBe('2026-11-09T21:00:00.000Z');
    expect(request.request.params.get('to')).toBe('2026-11-10T20:59:59.999Z');
    request.flush(EMPTY_PAGE);

    await promise;
  });
});

describe('actionLabel', () => {
  it('turns a known action into Turkish', () => {
    expect(actionLabel('Link.Deleted')).toBe('Link silindi');
  });

  it('shows an unknown action as it is', () => {
    expect(actionLabel('Something.New')).toBe('Something.New');
  });
});

describe('day bounds', () => {
  it('returns null for an empty or invalid date', () => {
    expect(startOfLocalDayIso('')).toBeNull();
    expect(endOfLocalDayIso('not-a-date')).toBeNull();
    expect(startOfLocalDayIso('2026-13-45')).toBeNull();
  });

  it('covers one local day, from its first to its last moment, as UTC timestamps', () => {
    const start = startOfLocalDayIso('2026-11-10')!;
    const end = endOfLocalDayIso('2026-11-10')!;

    expect(start.endsWith('Z')).toBe(true);
    expect(end.endsWith('Z')).toBe(true);

    const hours = (new Date(end).getTime() - new Date(start).getTime()) / 3_600_000;

    expect(hours).toBeGreaterThan(22.9);
    expect(hours).toBeLessThan(25.1);
  });
});