import { HttpErrorResponse } from '@angular/common/http';
import { Component, OnInit, inject, signal } from '@angular/core';
import { MatButtonModule } from '@angular/material/button';
import { MatDialog } from '@angular/material/dialog';
import { MatProgressBarModule } from '@angular/material/progress-bar';
import { firstValueFrom } from 'rxjs';
import { getErrorCodes, toMessages } from '../../../core/api/api-errors';
import { ConfirmService } from '../../../shared/confirm/confirm.service';
import { CategoriesService, Category } from './categories.service';
import { CategoryFormData, CategoryFormDialog } from './category-form-dialog';

interface PageMessage {
  kind: 'success' | 'error';
  text: string;
}

type FailureContext = 'save' | 'delete';

@Component({
  selector: 'app-categories-page',
  imports: [MatButtonModule, MatProgressBarModule],
  templateUrl: './categories-page.html',
})
export class CategoriesPage implements OnInit {
  private readonly service = inject(CategoriesService);
  private readonly confirmService = inject(ConfirmService);
  private readonly dialog = inject(MatDialog);

  protected readonly categories = signal<Category[]>([]);
  protected readonly loading = signal(false);
  protected readonly working = signal(false);
  protected readonly message = signal<PageMessage | null>(null);

  async ngOnInit(): Promise<void> {
    await this.load();
  }

  protected async load(): Promise<void> {
    this.loading.set(true);
    this.message.set(null);

    try {
      this.categories.set(await this.service.getAll());
    } catch (error) {
      this.message.set({ kind: 'error', text: toMessages(getErrorCodes(error))[0] });
    } finally {
      this.loading.set(false);
    }
  }

  protected async create(): Promise<void> {
    const saved = await this.openForm({
      title: 'Yeni kategori',
      name: '',
      confirm: {
        title: 'Kategori oluştur',
        message: (name) => `"${name}" adlı kategori oluşturulsun mu?`,
        confirmText: 'Oluştur',
      },
      save: (name) => this.service.create(name),
      describeError: (error) => this.describe(error, 'save'),
    });

    if (saved) {
      await this.finishChange('Kategori oluşturuldu.');
    }
  }

  protected async rename(category: Category): Promise<void> {
    const saved = await this.openForm({
      title: 'Kategoriyi yeniden adlandır',
      name: category.name,
      confirm: {
        title: 'Kategoriyi güncelle',
        message: (name) => `"${category.name}" kategorisinin adı "${name}" olarak değiştirilsin mi?`,
        confirmText: 'Güncelle',
      },
      save: (name) => this.service.update(category.id, name, category.isActive),
      describeError: (error) => this.describe(error, 'save'),
    });

    if (saved) {
      await this.finishChange('Kategori güncellendi.');
    }
  }

  protected async toggleActive(category: Category): Promise<void> {
    const makeActive = !category.isActive;

    const confirmed = await this.confirmService.confirm({
      title: makeActive ? 'Kategoriyi aktif yap' : 'Kategoriyi pasif yap',
      message: makeActive
        ? `"${category.name}" kategorisi yeniden aktif yapılacak.`
        : `"${category.name}" kategorisi pasif yapılacak. Bu kategorideki bağlantılar kullanıcılara görünmez ve yeni bağlantı eklenemez.`,
      confirmText: makeActive ? 'Aktif yap' : 'Pasif yap',
    });

    if (confirmed) {
      await this.run(
        () => this.service.update(category.id, category.name, makeActive),
        makeActive ? 'Kategori aktif yapıldı.' : 'Kategori pasif yapıldı.',
        'save',
      );
    }
  }

  protected async remove(category: Category): Promise<void> {
    const confirmed = await this.confirmService.confirm({
      title: 'Kategoriyi sil',
      message: `"${category.name}" kategorisi silinecek. Kategoriye bağlı bağlantı varsa silinemez.`,
      confirmText: 'Sil',
    });

    if (confirmed) {
      await this.run(() => this.service.remove(category.id), 'Kategori silindi.', 'delete');
    }
  }

  /** Opens the name form. Resolves to true when the change was saved inside the window. */
  private async openForm(data: CategoryFormData): Promise<boolean> {
    const reference = this.dialog.open<CategoryFormDialog, CategoryFormData, boolean>(
      CategoryFormDialog,
      { data, width: '420px', maxWidth: '92vw' },
    );

    return (await firstValueFrom(reference.afterClosed())) === true;
  }

  /** After a change that was saved in a window: shows the success message and re-reads the list. */
  private async finishChange(successText: string): Promise<void> {
    this.message.set({ kind: 'success', text: successText });
    this.working.set(true);

    try {
      await this.reloadQuietly();
    } finally {
      this.working.set(false);
    }
  }

  /** Runs one action, shows the result, and always re-reads the list so the screen stays current. */
  private async run(
    action: () => Promise<void>,
    successText: string,
    context: FailureContext,
  ): Promise<void> {
    this.working.set(true);
    this.message.set(null);

    try {
      await action();
      this.message.set({ kind: 'success', text: successText });
    } catch (error) {
      this.message.set({ kind: 'error', text: this.describe(error, context) });
    }

    try {
      await this.reloadQuietly();
    } finally {
      this.working.set(false);
    }
  }

  private async reloadQuietly(): Promise<void> {
    try {
      this.categories.set(await this.service.getAll());
    } catch {
      // The old list stays on screen. The message already tells the user what happened.
    }
  }

  private describe(error: unknown, context: FailureContext): string {
    if (error instanceof HttpErrorResponse) {
      if (getErrorCodes(error).includes('InvalidAntiforgeryToken')) {
        return toMessages(['InvalidAntiforgeryToken'])[0];
      }

      switch (error.status) {
        case 0:
          return toMessages(['NetworkError'])[0];
        case 400:
          return 'Girilen bilgiler geçersiz. Kategori adı boş olamaz ve en fazla 100 karakter olabilir.';
        case 403:
          return 'Bu işlem için yetkiniz yok.';
        case 404:
          return 'Kategori bulunamadı. Başka biri silmiş olabilir. Pencereyi kapatıp listeyi yenileyin.';
        case 409:
          return context === 'delete'
            ? 'Bu kategoriye bağlı bağlantılar var, silinemez. Önce bağlantıları silin veya başka bir kategoriye taşıyın.'
            : 'Bu adla bir kategori zaten var.';
      }
    }

    return toMessages(getErrorCodes(error))[0];
  }
}