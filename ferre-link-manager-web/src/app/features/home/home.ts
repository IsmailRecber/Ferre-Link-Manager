import { Component, OnInit, computed, inject, signal } from '@angular/core';
import { MatButtonModule } from '@angular/material/button';
import { MatFormFieldModule } from '@angular/material/form-field';
import { MatInputModule } from '@angular/material/input';
import { MatProgressBarModule } from '@angular/material/progress-bar';
import { Router } from '@angular/router';
import { getErrorCodes, toMessages } from '../../core/api/api-errors';
import { AuthService } from '../../core/auth/auth.service';
import { ConfirmService } from '../../shared/confirm/confirm.service';
import { LinkItem, LinksService } from './links.service';

interface LinkGroup {
  id: string;
  name: string;
  links: LinkItem[];
}

@Component({
  selector: 'app-home',
  imports: [MatButtonModule, MatFormFieldModule, MatInputModule, MatProgressBarModule],
  templateUrl: './home.html',
  styleUrl: './home.scss',
})
export class Home implements OnInit {
  private readonly linksService = inject(LinksService);
  private readonly confirmService = inject(ConfirmService);
  private readonly auth = inject(AuthService);
  private readonly router = inject(Router);

  protected readonly links = signal<LinkItem[]>([]);
  protected readonly loading = signal(false);
  protected readonly error = signal<string | null>(null);
  protected readonly query = signal('');

  protected readonly groups = computed<LinkGroup[]>(() => {
    const term = this.query().trim().toLocaleLowerCase('tr');

    const visible = term
      ? this.links().filter(
          (link) =>
            link.title.toLocaleLowerCase('tr').includes(term) ||
            link.categoryName.toLocaleLowerCase('tr').includes(term),
        )
      : this.links();

    // The API already sorts by category and title, so the order is kept as it comes.
    const groups = new Map<string, LinkGroup>();

    for (const link of visible) {
      let group = groups.get(link.categoryId);

      if (!group) {
        group = { id: link.categoryId, name: link.categoryName, links: [] };
        groups.set(link.categoryId, group);
      }

      group.links.push(link);
    }

    return [...groups.values()];
  });

  protected readonly visibleCount = computed(() =>
    this.groups().reduce((total, group) => total + group.links.length, 0),
  );

  async ngOnInit(): Promise<void> {
    await this.load();
  }

  protected async load(): Promise<void> {
    this.loading.set(true);
    this.error.set(null);

    try {
      this.links.set(await this.linksService.getMyLinks());
    } catch (error) {
      this.error.set(toMessages(getErrorCodes(error))[0]);
    } finally {
      this.loading.set(false);
    }
  }

  protected onSearch(event: Event): void {
    this.query.set((event.target as HTMLInputElement).value);
  }

  protected async open(link: LinkItem): Promise<void> {
    // The session may have ended in another tab. Check it first, before asking anything.
    try {
      if (!(await this.auth.loadCurrentUser())) {
        await this.router.navigateByUrl('/login');
        return;
      }
    } catch (error) {
      this.error.set(toMessages(getErrorCodes(error))[0]);
      return;
    }

    const confirmed = await this.confirmService.confirm({
      title: 'Bağlantıyı aç',
      message: `"${link.title}" bağlantısı yeni sekmede açılacak. Devam etmek istiyor musunuz?`,
      confirmText: 'Aç',
    });

    if (confirmed) {
      this.linksService.open(link.id);
    }
  }

  protected initial(link: LinkItem): string {
    return link.title.trim().charAt(0).toLocaleUpperCase('tr');
  }
}