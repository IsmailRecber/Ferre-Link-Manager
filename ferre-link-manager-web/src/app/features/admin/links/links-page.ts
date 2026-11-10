import { HttpErrorResponse } from '@angular/common/http';
import { Component, OnInit, computed, inject, signal } from '@angular/core';
import { MatButtonModule } from '@angular/material/button';
import { MatDialog } from '@angular/material/dialog';
import { MatFormFieldModule } from '@angular/material/form-field';
import { MatInputModule } from '@angular/material/input';
import { MatProgressBarModule } from '@angular/material/progress-bar';
import { MatSelectModule } from '@angular/material/select';
import { ActivatedRoute } from '@angular/router';
import { firstValueFrom } from 'rxjs';
import { getErrorCodes, toMessages } from '../../../core/api/api-errors';
import { ConfirmService } from '../../../shared/confirm/confirm.service';
import { CategoriesService, Category } from '../categories/categories.service';
import { LinkFormData, LinkFormDialog } from './link-form-dialog';
import { AdminLink, LinkFormValue, LinksAdminService, roleLabel } from './links-admin.service';

interface PageMessage {
  kind: 'success' | 'error';
  text: string;
}

interface LinkGroup {
  category: Category;
  /** The links shown now (after the search). */
  links: AdminLink[];
  /** All links of the category, whatever the search says. */
  total: number;
}

type FailureContext = 'form' | 'action';

@Component({
  selector: 'app-links-page',
  imports: [
    MatButtonModule,
    MatFormFieldModule,
    MatInputModule,
    MatProgressBarModule,
    MatSelectModule,
  ],
  templateUrl: './links-page.html',
})
export class LinksPage implements OnInit {
  private readonly linksService = inject(LinksAdminService);
  private readonly categoriesService = inject(CategoriesService);
  private readonly confirmService = inject(ConfirmService);
  private readonly dialog = inject(MatDialog);
  private readonly route = inject(ActivatedRoute);

  protected readonly links = signal<AdminLink[]>([]);
  protected readonly categories = signal<Category[]>([]);
  protected readonly loading = signal(false);
  protected readonly working = signal(false);
  protected readonly message = signal<PageMessage | null>(null);

  // Filters. An empty category id means "all categories".
  protected readonly filterCategoryId = signal('');
  protected readonly search = signal('');

  protected readonly activeCategories = computed(() => this.categories().filter((c) => c.isActive));

  protected readonly hasFilter = computed(
    () => this.filterCategoryId() !== '' || this.search().trim() !== '',
  );

  private readonly countsByCategory = computed(() => {
    const counts = new Map<string, number>();

    for (const link of this.links()) {
      counts.set(link.categoryId, (counts.get(link.categoryId) ?? 0) + 1);
    }

    return counts;
  });

  /** Every category is a group, even an empty one. A search keeps only the groups that have a match. */
  protected readonly groups = computed<LinkGroup[]>(() => {
    const term = this.search().trim().toLocaleLowerCase('tr');
    const selected = this.filterCategoryId();
    const all = this.links();
    const result: LinkGroup[] = [];

    for (const category of this.categories()) {
      if (selected && category.id !== selected) {
        continue;
      }

      const inCategory = all.filter((link) => link.categoryId === category.id);

      const links = term
        ? inCategory.filter(
            (link) =>
              link.title.toLocaleLowerCase('tr').includes(term) ||
              link.url.toLocaleLowerCase('tr').includes(term),
          )
        : inCategory;

      if (term && links.length === 0) {
        continue;
      }

      result.push({ category, links, total: inCategory.length });
    }

    return result;
  });

  async ngOnInit(): Promise<void> {
    // The "see links" button on the categories page opens this page with a category already chosen.
    const preset = this.route.snapshot.queryParamMap.get('category');

    if (preset) {
      this.filterCategoryId.set(preset);
    }

    await this.load();
  }

  protected roleLabel(role: string): string {
    return roleLabel(role);
  }

  protected countOf(categoryId: string): number {
    return this.countsByCategory().get(categoryId) ?? 0;
  }

  protected onSearch(event: Event): void {
    this.search.set((event.target as HTMLInputElement).value);
  }

  protected clearFilters(): void {
    this.filterCategoryId.set('');
    this.search.set('');
  }

  protected async load(): Promise<void> {
    this.loading.set(true);
    this.message.set(null);

    try {
      const [links, categories] = await Promise.all([
        this.linksService.getAll(),
        this.categoriesService.getAll(),
      ]);

      this.applyData(links, categories);
    } catch (error) {
      this.message.set({ kind: 'error', text: toMessages(getErrorCodes(error))[0] });
    } finally {
      this.loading.set(false);
    }
  }

  /** Opens the new-link form. A category can be given in advance (the "add to this category" button). */
  protected async create(presetCategoryId?: string): Promise<void> {
    const preferred = presetCategoryId ?? this.filterCategoryId();
    const categoryId = this.activeCategories().some((c) => c.id === preferred) ? preferred : '';

    const saved = await this.openForm({
      title: 'Yeni link',
      categories: this.activeCategories(),
      initial: { title: '', url: '', categoryId, roles: ['User'] },
      confirm: {
        title: 'Link oluştur',
        message: (value) => `"${value.title}" bağlantısı oluşturulsun mu?`,
        confirmText: 'Oluştur',
      },
      save: (value) => this.linksService.create(value),
      describeError: (error) => this.describe(error, 'form'),
    });

    if (saved) {
      await this.finishChange('Link oluşturuldu.');
    }
  }

  protected async edit(link: AdminLink): Promise<void> {
    // Only active categories can be chosen, plus the one the link is in now (even if it is inactive).
    const choices = this.categories().filter((c) => c.isActive || c.id === link.categoryId);

    const saved = await this.openForm({
      title: 'Linki düzenle',
      categories: choices,
      initial: { title: link.title, url: link.url, categoryId: link.categoryId, roles: [...link.roles] },
      confirm: {
        title: 'Linki güncelle',
        message: (value) => `"${value.title}" bağlantısı güncellensin mi?`,
        confirmText: 'Güncelle',
      },
      save: (value) => this.linksService.update(link.id, value),
      describeError: (error) => this.describe(error, 'form'),
    });

    if (saved) {
      await this.finishChange('Link güncellendi.');
    }
  }

  protected async toggleActive(link: AdminLink): Promise<void> {
    const makeActive = !link.isActive;

    const confirmed = await this.confirmService.confirm({
      title: makeActive ? 'Linki aktif yap' : 'Linki pasif yap',
      message: makeActive
        ? `"${link.title}" bağlantısı yeniden aktif yapılacak ve yetkili kullanıcılara görünecek.`
        : `"${link.title}" bağlantısı pasif yapılacak ve kullanıcılara görünmeyecek.`,
      confirmText: makeActive ? 'Aktif yap' : 'Pasif yap',
    });

    if (confirmed) {
      await this.run(
        () => this.linksService.setActive(link.id, makeActive),
        makeActive ? 'Link aktif yapıldı.' : 'Link pasif yapıldı.',
      );
    }
  }

  protected async remove(link: AdminLink): Promise<void> {
    const confirmed = await this.confirmService.confirm({
      title: 'Linki sil',
      message: `"${link.title}" bağlantısı silinecek. Bu işlem geri alınamaz.`,
      confirmText: 'Sil',
    });

    if (confirmed) {
      await this.run(() => this.linksService.remove(link.id), 'Link silindi.');
    }
  }

  /** Opens the link form. Resolves to true when the change was saved inside the window. */
  private async openForm(data: LinkFormData): Promise<boolean> {
    const reference = this.dialog.open<LinkFormDialog, LinkFormData, boolean>(LinkFormDialog, {
      data,
      width: '560px',
      maxWidth: '94vw',
    });

    return (await firstValueFrom(reference.afterClosed())) === true;
  }

  private applyData(links: AdminLink[], categories: Category[]): void {
    this.links.set(links);
    this.categories.set(categories);

    // The chosen category may have been deleted meanwhile.
    const selected = this.filterCategoryId();

    if (selected && !categories.some((category) => category.id === selected)) {
      this.filterCategoryId.set('');
    }
  }

  /** After a change that was saved in a window: shows the success message and re-reads the data. */
  private async finishChange(successText: string): Promise<void> {
    this.message.set({ kind: 'success', text: successText });
    this.working.set(true);

    try {
      await this.reloadQuietly();
    } finally {
      this.working.set(false);
    }
  }

  /** Runs one action, shows the result, and always re-reads the data so the screen stays current. */
  private async run(action: () => Promise<void>, successText: string): Promise<void> {
    this.working.set(true);
    this.message.set(null);

    try {
      await action();
      this.message.set({ kind: 'success', text: successText });
    } catch (error) {
      this.message.set({ kind: 'error', text: this.describe(error, 'action') });
    }

    try {
      await this.reloadQuietly();
    } finally {
      this.working.set(false);
    }
  }

  private async reloadQuietly(): Promise<void> {
    try {
      const [links, categories] = await Promise.all([
        this.linksService.getAll(),
        this.categoriesService.getAll(),
      ]);

      this.applyData(links, categories);
    } catch {
      // The old data stays on screen. The message already tells the user what happened.
    }
  }

  private describe(error: unknown, context: FailureContext): string {
    if (error instanceof HttpErrorResponse) {
      const codes = getErrorCodes(error);

      if (codes.includes('InvalidAntiforgeryToken')) {
        return toMessages(['InvalidAntiforgeryToken'])[0];
      }

      if (codes.includes('InvalidRole')) {
        return 'Geçersiz rol seçimi. Yalnızca Yönetici ve Kullanıcı rolleri seçilebilir.';
      }

      switch (error.status) {
        case 0:
          return toMessages(['NetworkError'])[0];
        case 400:
          return 'Girilen bilgiler geçersiz. Başlık, http veya https ile başlayan bir adres ve en az bir rol gerekli.';
        case 403:
          return 'Bu işlem için yetkiniz yok.';
        case 404:
          return context === 'form'
            ? 'Link veya kategori bulunamadı. Başka biri silmiş olabilir. Pencereyi kapatıp listeyi yenileyin.'
            : 'Link bulunamadı. Başka biri silmiş olabilir, liste yenilendi.';
        case 409:
          return 'Seçilen kategori pasif. Aktif bir kategori seçin.';
      }
    }

    return toMessages(getErrorCodes(error))[0];
  }
}