import { Component, DestroyRef, inject, signal } from '@angular/core';
import { takeUntilDestroyed } from '@angular/core/rxjs-interop';
import { NonNullableFormBuilder, ReactiveFormsModule, ValidatorFn, Validators } from '@angular/forms';
import { MatButtonModule } from '@angular/material/button';
import { MatFormFieldModule } from '@angular/material/form-field';
import { MatInputModule } from '@angular/material/input';
import { MatProgressBarModule } from '@angular/material/progress-bar';
import { RouterLink } from '@angular/router';
import { getErrorCodes, toMessages } from '../../../core/api/api-errors';
import { AuthService } from '../../../core/auth/auth.service';
import { Brand } from '../../../shared/brand/brand';
import { passwordRules } from '../password-rules';

const RESEND_COOLDOWN_SECONDS = 60;

const matchesPassword: ValidatorFn = (control) =>
  control.value === control.parent?.get('password')?.value ? null : { mismatch: true };

@Component({
  selector: 'app-register',
  imports: [
    ReactiveFormsModule,
    RouterLink,
    MatButtonModule,
    MatFormFieldModule,
    MatInputModule,
    MatProgressBarModule,
    Brand,
  ],
  templateUrl: './register.html',
})
export class Register {
  private readonly formBuilder = inject(NonNullableFormBuilder);
  private readonly auth = inject(AuthService);
  private readonly destroyRef = inject(DestroyRef);

  protected readonly form = this.formBuilder.group({
    email: ['', [Validators.required, Validators.email, Validators.maxLength(256)]],
    password: ['', passwordRules],
    confirmPassword: ['', [Validators.required, matchesPassword]],
  });

  protected readonly busy = signal(false);
  protected readonly errors = signal<string[]>([]);
  protected readonly showPassword = signal(false);

  // Set after a successful registration. Needed to ask for a new verification e-mail.
  protected readonly registeredUserId = signal<string | null>(null);
  protected readonly resendCooldown = signal(0);
  protected readonly resendMessage = signal<string | null>(null);

  private timer: ReturnType<typeof setInterval> | null = null;

  constructor() {
    // The "repeat password" check must run again whenever the password changes.
    this.form.controls.password.valueChanges
      .pipe(takeUntilDestroyed())
      .subscribe(() => this.form.controls.confirmPassword.updateValueAndValidity());

    this.destroyRef.onDestroy(() => this.stopTimer());
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

    try {
      const { email, password } = this.form.getRawValue();
      const response = await this.auth.register(email.trim(), password);
      this.registeredUserId.set(response.userId);
      this.startCooldown();
    } catch (error) {
      this.errors.set(toMessages(getErrorCodes(error)));
    } finally {
      this.busy.set(false);
    }
  }

  protected async resend(): Promise<void> {
    const userId = this.registeredUserId();

    if (!userId || this.resendCooldown() > 0 || this.busy()) {
      return;
    }

    this.busy.set(true);
    this.resendMessage.set(null);

    try {
      await this.auth.resendVerification(userId);
      this.resendMessage.set('Doğrulama e-postası yeniden gönderildi.');
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