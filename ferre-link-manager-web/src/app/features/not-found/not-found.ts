import { Component } from '@angular/core';
import { MatButtonModule } from '@angular/material/button';
import { RouterLink } from '@angular/router';
import { Brand } from '../../shared/brand/brand';

@Component({
  selector: 'app-not-found',
  imports: [RouterLink, MatButtonModule, Brand],
  template: `
    <main class="auth-page">
      <section class="auth-card">
        <div class="auth-brand">
          <app-brand size="large" />
        </div>

        <h1>Sayfa bulunamadı</h1>
        <p class="auth-subtitle">Aradığınız sayfa yok veya taşınmış olabilir.</p>

        <a mat-flat-button class="auth-submit" routerLink="/">Ana sayfaya dön</a>
      </section>
    </main>
  `,
})
export class NotFound {}