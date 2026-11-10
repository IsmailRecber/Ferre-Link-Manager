import { Component } from '@angular/core';
import { MatButtonModule } from '@angular/material/button';
import { RouterLink } from '@angular/router';

@Component({
  selector: 'app-not-found',
  imports: [RouterLink, MatButtonModule],
  template: `
    <main class="auth-page">
      <section class="auth-card">
        <div class="auth-brand">
          <span class="auth-logo">F</span>
          <span class="auth-brand-name">Ferre Link Manager</span>
        </div>

        <h1>Sayfa bulunamadı</h1>
        <p class="auth-subtitle">Aradığınız sayfa yok veya taşınmış olabilir.</p>

        <a mat-flat-button class="auth-submit" routerLink="/">Ana sayfaya dön</a>
      </section>
    </main>
  `,
})
export class NotFound {}