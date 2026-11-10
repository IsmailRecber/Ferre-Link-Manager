import { Component, DestroyRef, inject, signal } from '@angular/core';
import { NonNullableFormBuilder, ReactiveFormsModule, Validators } from '@angular/forms';
import { MatButtonModule } from '@angular/material/button';
import { MatFormFieldModule } from '@angular/material/form-field';
import { MatInputModule } from '@angular/material/input';
import { MatProgressBarModule } from '@angular/material/progress-bar';
import { Router, RouterLink } from '@angular/router';
import { getErrorCodes, toMessages } from '../../../core/api/api-errors';
import { AuthService } from '../../../core/auth/auth.service';

// The API sends one e-mail per address per minute, so the button waits as long.
const RESEND_COOLDOWN_SECONDS = 60;

@Component({
  selector: 'app-login',
  imports: [
    ReactiveFormsModule,
    RouterLink,
    MatButtonModule,
    MatFormFieldModule,
    MatInputModule,
    MatProgressBarModule,
  ],
  templateUrl: './login.html',
})
export class Login {
  private readonly formBuilder = inject(NonNullableFormBuilder);
  private readonly auth = inject(AuthService);
  private readonly router = inject(Router);

  protected readonly form = this.formBuilder.group({
    email: ['', [Validators.required, Validators.email, Validators.maxLength(256)]],
    password: ['', [Validators.required, Validators.maxLength(128)]],
  });

  protected readonly busy = signal(false);
  protected readonly errors = signal<string[]>([]);
  protected readonly showPassword = signal(false);

  // Shown after a login failed because the e-mail address is not confirmed yet.
  protected readonly needsVerification = signal(false);
  protected readonly resendCooldown = signal(0);
  protected readonly resendMessage = signal<string | null>(null);

  private timer: ReturnType<typeof setInterval> | null = null;

  constructor() {
    inject(DestroyRef).onDestroy(() => this.stopTimer());
  }

  protected async submit(): Promise<void> {
    if (this.busy()) {
      return;
    }

    if (this.form.invalid) {
      this.form.markAllAsTouched();
      return;
    }

    this.busy.set(true);
    this.errors.set([]);
    this.needsVerification.set(false);
    this.resendMessage.set(null);

    try {
      const { email, password } = this.form.getRawValue();
      await this.auth.login(email.trim(), password);
      await this.router.navigateByUrl('/');
    } catch (error) {
      const codes = getErrorCodes(error);

      this.errors.set(toMessages(codes));
      this.needsVerification.set(codes.includes('EmailNotConfirmed'));
    } finally {
      this.busy.set(false);
    }
  }

  protected async resend(): Promise<void> {
    if (this.busy() || this.resendCooldown() > 0) {
      return;
    }

    const emailControl = this.form.controls.email;

    if (emailControl.invalid) {
      emailControl.markAsTouched();
      return;
    }

    this.busy.set(true);
    this.resendMessage.set(null);

    try {
      await this.auth.resendVerificationByEmail(emailControl.value.trim());

      // The same wording whether or not an account exists, like the API's answer.
      this.resendMessage.set(
        'Bu adrese kayıtlı, doğrulanmamış bir hesap varsa doğrulama e-postası gönderildi. Gelen kutunuzu ve spam klasörünü kontrol edin.',
      );
      this.startCooldown();
    } catch (error) {
      this.resendMessage.set(toMessages(getErrorCodes(error))[0]);
    } finally {
      this.busy.set(false);
    }
  }

  private startCooldown(): void {
    this.stopTimer();
    this.resendCooldown.set(RESEND_COOLDOWN_SECONDS);

    this.timer = setInterval(() => {
      const remaining = this.resendCooldown() - 1;
      this.resendCooldown.set(Math.max(remaining, 0));

      if (remaining <= 0) {
        this.stopTimer();
      }
    }, 1000);
  }

  private stopTimer(): void {
    if (this.timer !== null) {
      clearInterval(this.timer);
      this.timer = null;
    }
  }
}