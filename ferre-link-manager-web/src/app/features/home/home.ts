import { Component, OnInit, inject } from '@angular/core';
import { MatButtonModule } from '@angular/material/button';
import { Router } from '@angular/router';
import { AuthService } from '../../core/auth/auth.service';

@Component({
  selector: 'app-home',
  imports: [MatButtonModule],
  template: `
    <main class="auth-page">
      <section class="auth-card">
        <h1>Hoş geldiniz</h1>
        @if (auth.user(); as user) {
          <p class="auth-subtitle">{{ user.email }}</p>
          <p class="auth-note">Rol: {{ user.roles.join(', ') }}</p>
        }
        <p class="auth-note">Bu geçici bir sayfadır. Link listesi sonraki adımlarda eklenecek.</p>
        <button mat-flat-button class="auth-submit" type="button" (click)="logout()">Çıkış yap</button>
      </section>
    </main>
  `,
})
export class Home implements OnInit {
  protected readonly auth = inject(AuthService);
  private readonly router = inject(Router);

  async ngOnInit(): Promise<void> {
    const user = await this.auth.loadCurrentUser();

    if (!user) {
      await this.router.navigateByUrl('/login');
    }
  }

  protected async logout(): Promise<void> {
    await this.auth.logout();
    await this.router.navigateByUrl('/login');
  }
}