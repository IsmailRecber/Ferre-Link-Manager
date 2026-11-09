import { HttpErrorResponse } from '@angular/common/http';
import { Component, OnInit, computed, inject, signal } from '@angular/core';
import { MatButtonModule } from '@angular/material/button';
import { MatFormFieldModule } from '@angular/material/form-field';
import { MatInputModule } from '@angular/material/input';
import { MatProgressBarModule } from '@angular/material/progress-bar';
import { getErrorCodes, toMessages } from '../../../core/api/api-errors';
import { AuthService } from '../../../core/auth/auth.service';
import { ConfirmService } from '../../../shared/confirm/confirm.service';
import { roleLabel } from '../links/links-admin.service';
import { AdminUser, UsersAdminService } from './users-admin.service';

interface PageMessage {
  kind: 'success' | 'error';
  text: string;
}

@Component({
  selector: 'app-users-page',
  imports: [MatButtonModule, MatFormFieldModule, MatInputModule, MatProgressBarModule],
  templateUrl: './users-page.html',
})
export class UsersPage implements OnInit {
  private readonly service = inject(UsersAdminService);
  private readonly confirmService = inject(ConfirmService);
  private readonly auth = inject(AuthService);

  protected readonly users = signal<AdminUser[]>([]);
  protected readonly loading = signal(false);
  protected readonly working = signal(false);
  protected readonly message = signal<PageMessage | null>(null);
  protected readonly query = signal('');

  private readonly myId = computed(() => this.auth.user()?.userId.toLowerCase() ?? null);

  protected readonly filtered = computed(() => {
    const term = this.query().trim().toLocaleLowerCase('tr');

    return term
      ? this.users().filter((user) => user.email.toLocaleLowerCase('tr').includes(term))
      : this.users();
  });

  async ngOnInit(): Promise<void> {
    await this.load();
  }

  protected roleLabel(role: string): string {
    return roleLabel(role);
  }

  protected isSelf(user: AdminUser): boolean {
    return user.id.toLowerCase() === this.myId();
  }

  protected onSearch(event: Event): void {
    this.query.set((event.target as HTMLInputElement).value);
  }

  protected async load(): Promise<void> {
    this.loading.set(true);
    this.message.set(null);

    try {
      this.users.set(await this.service.getAll());
    } catch (error) {
      this.message.set({ kind: 'error', text: toMessages(getErrorCodes(error))[0] });
    } finally {
      this.loading.set(false);
    }
  }

  protected async toggleActive(user: AdminUser): Promise<void> {
    if (this.isSelf(user)) {
      return;
    }

    const makeActive = !user.isActive;

    const confirmed = await this.confirmService.confirm({
      title: makeActive ? 'Kullanıcıyı aktif yap' : 'Kullanıcıyı pasif yap',
      message: makeActive
        ? `"${user.email}" hesabı yeniden aktif yapılacak ve kullanıcı tekrar giriş yapabilecek.`
        : `"${user.email}" hesabı pasif yapılacak. Kullanıcı giriş yapamaz ve açık oturumu kısa süre içinde (yaklaşık 1 dakika) sona erer.`,
      confirmText: makeActive ? 'Aktif yap' : 'Pasif yap',
    });

    if (!confirmed) {
      return;
    }

    this.working.set(true);
    this.message.set(null);

    try {
      await this.service.setActive(user.id, makeActive);
      this.message.set({
        kind: 'success',
        text: makeActive ? 'Kullanıcı aktif yapıldı.' : 'Kullanıcı pasif yapıldı.',
      });
    } catch (error) {
      this.message.set({ kind: 'error', text: this.describe(error) });
    }

    // Always re-read the list, so the screen shows what is really stored.
    try {
      this.users.set(await this.service.getAll());
    } catch {
      // The old list stays on screen. The message already tells the user what happened.
    } finally {
      this.working.set(false);
    }
  }

  private describe(error: unknown): string {
    if (error instanceof HttpErrorResponse) {
      if (getErrorCodes(error).includes('InvalidAntiforgeryToken')) {
        return toMessages(['InvalidAntiforgeryToken'])[0];
      }

      switch (error.status) {
        case 0:
          return toMessages(['NetworkError'])[0];
        case 403:
          return 'Bu işlem için yetkiniz yok.';
        case 404:
          return 'Kullanıcı bulunamadı. Liste yenilendi.';
        case 409:
          return 'Kendi hesabınızı pasif yapamazsınız.';
      }
    }

    return toMessages(getErrorCodes(error))[0];
  }
}