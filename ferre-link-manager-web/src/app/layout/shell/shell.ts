import { Component, DestroyRef, computed, inject } from '@angular/core';
import { MatButtonModule } from '@angular/material/button';
import { MatMenuModule } from '@angular/material/menu';
import { MatToolbarModule } from '@angular/material/toolbar';
import { Router, RouterLink, RouterLinkActive, RouterOutlet } from '@angular/router';
import { AuthService } from '../../core/auth/auth.service';
import { ConfirmService } from '../../shared/confirm/confirm.service';

@Component({
  selector: 'app-shell',
  imports: [
    RouterOutlet,
    RouterLink,
    RouterLinkActive,
    MatToolbarModule,
    MatButtonModule,
    MatMenuModule,
  ],
  templateUrl: './shell.html',
  styleUrl: './shell.scss',
})
export class Shell {
  protected readonly auth = inject(AuthService);
  private readonly router = inject(Router);
  private readonly confirmService = inject(ConfirmService);

  protected readonly initial = computed(() =>
    (this.auth.user()?.email ?? '?').charAt(0).toUpperCase(),
  );

  constructor() {
    // When the user comes back to this tab, check that the session is still alive
    // (it may have ended in another tab).
    const onVisibilityChange = (): void => {
      if (document.visibilityState === 'visible') {
        void this.checkSession();
      }
    };

    document.addEventListener('visibilitychange', onVisibilityChange);
    inject(DestroyRef).onDestroy(() =>
      document.removeEventListener('visibilitychange', onVisibilityChange),
    );
  }

  /** The "skip to content" link: moves the keyboard focus straight to the main content. */
  protected skipToContent(event: Event): void {
    event.preventDefault();
    document.getElementById('main-content')?.focus();
  }

  protected async logout(): Promise<void> {
    const confirmed = await this.confirmService.confirm({
      title: 'Çıkış yap',
      message: 'Oturumunuzu kapatmak istediğinize emin misiniz?',
      confirmText: 'Çıkış yap',
    });

    if (!confirmed) {
      return;
    }

    try {
      await this.auth.logout();
    } catch {
      // If the server could not end the session, the local state is cleared anyway.
      this.auth.clearSession();
    }

    await this.router.navigateByUrl('/login');
  }

  private async checkSession(): Promise<void> {
    try {
      if (!(await this.auth.loadCurrentUser())) {
        await this.router.navigateByUrl('/login');
      }
    } catch {
      // A network problem is not a reason to sign the user out.
    }
  }
}