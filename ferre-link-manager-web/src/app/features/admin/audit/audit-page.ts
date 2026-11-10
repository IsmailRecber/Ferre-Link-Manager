import { Component, OnInit, computed, inject, signal } from '@angular/core';
import { MatButtonModule } from '@angular/material/button';
import { MatFormFieldModule } from '@angular/material/form-field';
import { MatInputModule } from '@angular/material/input';
import { MatProgressBarModule } from '@angular/material/progress-bar';
import { MatSelectModule } from '@angular/material/select';
import { getErrorCodes, toMessages } from '../../../core/api/api-errors';
import {
  AUDIT_ACTIONS,
  AuditAdminService,
  AuditLogItem,
  actionLabel,
  endOfLocalDayIso,
  startOfLocalDayIso,
} from './audit-admin.service';

interface AppliedFilter {
  action: string;
  fromDate: string;
  toDate: string;
}

const PAGE_SIZE = 20;

const ENTITY_LABELS: Record<string, string> = {
  Link: 'Link',
  Category: 'Kategori',
  User: 'Kullanıcı',
};

// The user's own time zone. The API sends UTC and the browser converts it.
const timeFormatter = new Intl.DateTimeFormat('tr-TR', {
  day: '2-digit',
  month: '2-digit',
  year: 'numeric',
  hour: '2-digit',
  minute: '2-digit',
  second: '2-digit',
});

@Component({
  selector: 'app-audit-page',
  imports: [
    MatButtonModule,
    MatFormFieldModule,
    MatInputModule,
    MatProgressBarModule,
    MatSelectModule,
  ],
  templateUrl: './audit-page.html',
})
export class AuditPage implements OnInit {
  private readonly service = inject(AuditAdminService);

  protected readonly actionOptions = AUDIT_ACTIONS;

  protected readonly items = signal<AuditLogItem[]>([]);
  protected readonly totalCount = signal(0);
  protected readonly page = signal(1);
  protected readonly loading = signal(false);
  protected readonly error = signal<string | null>(null);

  // What the user typed. It is applied only when the Filter button is pressed.
  protected readonly action = signal('');
  protected readonly fromDate = signal('');
  protected readonly toDate = signal('');
  protected readonly formError = signal<string | null>(null);

  private readonly applied = signal<AppliedFilter>({ action: '', fromDate: '', toDate: '' });

  protected readonly hasFilter = computed(() => {
    const filter = this.applied();

    return Boolean(filter.action || filter.fromDate || filter.toDate);
  });

  protected readonly totalPages = computed(() => Math.max(1, Math.ceil(this.totalCount() / PAGE_SIZE)));

  // Only the answer to the latest request may change the screen.
  private requestId = 0;

  async ngOnInit(): Promise<void> {
    await this.load(1);
  }

  protected async load(page: number): Promise<void> {
    const requestId = ++this.requestId;

    this.loading.set(true);
    this.error.set(null);

    try {
      const filter = this.applied();

      const result = await this.service.getPage(page, PAGE_SIZE, {
        action: filter.action || null,
        fromUtc: startOfLocalDayIso(filter.fromDate),
        toUtc: endOfLocalDayIso(filter.toDate),
      });

      if (requestId !== this.requestId) {
        return;
      }

      this.items.set(result.items);
      this.totalCount.set(result.totalCount);
      this.page.set(result.page);
    } catch (error) {
      if (requestId === this.requestId) {
        this.error.set(toMessages(getErrorCodes(error))[0]);
      }
    } finally {
      if (requestId === this.requestId) {
        this.loading.set(false);
      }
    }
  }

  protected async applyFilters(event: Event): Promise<void> {
    event.preventDefault();

    const from = this.fromDate();
    const to = this.toDate();

    // yyyy-mm-dd dates can be compared as text.
    if (from && to && from > to) {
      this.formError.set('Başlangıç tarihi bitiş tarihinden sonra olamaz.');
      return;
    }

    this.formError.set(null);
    this.applied.set({ action: this.action(), fromDate: from, toDate: to });

    await this.load(1);
  }

  protected async clearFilters(): Promise<void> {
    this.action.set('');
    this.fromDate.set('');
    this.toDate.set('');
    this.formError.set(null);
    this.applied.set({ action: '', fromDate: '', toDate: '' });

    await this.load(1);
  }

  protected async goTo(page: number): Promise<void> {
    if (this.loading() || page < 1 || page > this.totalPages()) {
      return;
    }

    await this.load(page);
  }

  protected valueOf(event: Event): string {
    return (event.target as HTMLInputElement).value;
  }

  protected actionLabel(action: string): string {
    return actionLabel(action);
  }

  protected formatTime(iso: string): string {
    const date = new Date(iso);

    return Number.isNaN(date.getTime()) ? iso : timeFormatter.format(date);
  }

  protected entityTypeLabel(type: string): string {
    return ENTITY_LABELS[type] ?? type;
  }

  protected targetName(item: AuditLogItem): string {
    if (item.entityName) {
      return item.entityName;
    }

    return item.entityType === 'User' ? 'Bilinmeyen kullanıcı' : item.entityId.slice(0, 8);
  }

  protected actorName(item: AuditLogItem): string {
    if (item.userEmail) {
      return item.userEmail;
    }

    return item.userId ? 'Bilinmeyen kullanıcı' : 'Sistem';
  }
}