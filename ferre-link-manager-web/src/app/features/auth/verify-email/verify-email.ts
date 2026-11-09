import { Component, OnInit, inject, signal } from '@angular/core';
import { MatButtonModule } from '@angular/material/button';
import { MatProgressBarModule } from '@angular/material/progress-bar';
import { ActivatedRoute, Router, RouterLink } from '@angular/router';
import { AuthService } from '../../../core/auth/auth.service';

type VerifyStatus = 'checking' | 'success' | 'failed';

@Component({
  selector: 'app-verify-email',
  imports: [RouterLink, MatButtonModule, MatProgressBarModule],
  templateUrl: './verify-email.html',
})
export class VerifyEmail implements OnInit {
  private readonly route = inject(ActivatedRoute);
  private readonly router = inject(Router);
  private readonly auth = inject(AuthService);

  protected readonly status = signal<VerifyStatus>('checking');

  async ngOnInit(): Promise<void> {
    const userId = this.route.snapshot.queryParamMap.get('userId');
    const token = this.route.snapshot.queryParamMap.get('token');

    // Remove the token from the address bar and the browser history right away.
    await this.router.navigate([], { relativeTo: this.route, replaceUrl: true });

    if (!userId || !token) {
      this.status.set('failed');
      return;
    }

    try {
      await this.auth.confirmEmail(userId, token);
      this.status.set('success');
    } catch {
      this.status.set('failed');
    }
  }
}