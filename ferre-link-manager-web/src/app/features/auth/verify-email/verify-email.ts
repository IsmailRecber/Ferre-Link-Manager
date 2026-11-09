import { Component, OnInit, inject, signal } from '@angular/core';
import { MatButtonModule } from '@angular/material/button';
import { MatProgressBarModule } from '@angular/material/progress-bar';
import { ActivatedRoute, Router, RouterLink } from '@angular/router';
import { getErrorCodes } from '../../../core/api/api-errors';
import { AuthService } from '../../../core/auth/auth.service';

type VerifyStatus = 'checking' | 'success' | 'failed' | 'retry';

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

  // Kept in memory only, so the user can retry after the address bar has been cleaned.
  private userId: string | null = null;
  private token: string | null = null;

  async ngOnInit(): Promise<void> {
    this.userId = this.route.snapshot.queryParamMap.get('userId');
    this.token = this.route.snapshot.queryParamMap.get('token');

    // Remove the token from the address bar and the browser history right away.
    await this.router.navigate([], { relativeTo: this.route, replaceUrl: true });

    await this.verify();
  }

  protected async verify(): Promise<void> {
    if (!this.userId || !this.token) {
      this.status.set('failed');
      return;
    }

    this.status.set('checking');

    try {
      await this.auth.confirmEmail(this.userId, this.token);
      this.status.set('success');
    } catch (error) {
      const codes = getErrorCodes(error);
      this.status.set(codes.includes('InvalidAntiforgeryToken') ? 'retry' : 'failed');
    }
  }
}