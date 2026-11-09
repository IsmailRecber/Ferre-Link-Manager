import { Component } from '@angular/core';
import { MatButtonModule } from '@angular/material/button';
import { RouterLink, RouterLinkActive, RouterOutlet } from '@angular/router';

@Component({
  selector: 'app-admin-home',
  imports: [RouterLink, RouterLinkActive, RouterOutlet, MatButtonModule],
  template: `
    <section class="page">
      <h1>Yönetim</h1>

      <nav class="admin-tabs" aria-label="Yönetim bölümleri">
        <a mat-button routerLink="categories" routerLinkActive="active">Kategoriler</a>
        <a mat-button routerLink="links" routerLinkActive="active">Linkler</a>
      </nav>

      <router-outlet />
    </section>
  `,
})
export class AdminHome {}