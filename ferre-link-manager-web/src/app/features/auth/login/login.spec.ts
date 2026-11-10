import { HttpErrorResponse } from '@angular/common/http';
import { ComponentFixture, TestBed } from '@angular/core/testing';
import { provideRouter } from '@angular/router';
import { beforeEach, describe, expect, it, vi } from 'vitest';
import { AuthService } from '../../../core/auth/auth.service';
import { Login } from './login';

const authStub = {
  login: vi.fn(),
  resendVerificationByEmail: vi.fn(),
};

function setValue(root: HTMLElement, selector: string, value: string): void {
  const input = root.querySelector<HTMLInputElement>(selector)!;
  input.value = value;
  input.dispatchEvent(new Event('input'));
}

function submit(root: HTMLElement): void {
  root.querySelector('form')!.dispatchEvent(new Event('submit'));
}

describe('Login', () => {
  let fixture: ComponentFixture<Login>;
  let root: HTMLElement;

  // The screen updates after asynchronous work, so the checks retry until they hold.
  const eventually = (check: () => void) =>
    vi.waitFor(() => {
      fixture.detectChanges();
      check();
    });

  beforeEach(async () => {
    authStub.login.mockReset();
    authStub.resendVerificationByEmail.mockReset();

    await TestBed.configureTestingModule({
      imports: [Login],
      providers: [provideRouter([]), { provide: AuthService, useValue: authStub }],
    }).compileComponents();

    fixture = TestBed.createComponent(Login);
    fixture.detectChanges();
    root = fixture.nativeElement as HTMLElement;
  });

  it('does not call the API when the form is empty', async () => {
    submit(root);

    await eventually(() => expect(root.textContent).toContain('E-posta gerekli.'));
    expect(authStub.login).not.toHaveBeenCalled();
  });

  it('offers to send the verification e-mail again when the address is not confirmed', async () => {
    authStub.login.mockRejectedValue(
      new HttpErrorResponse({ status: 403, error: { errors: ['EmailNotConfirmed'] } }),
    );

    setValue(root, 'input[type="email"]', 'user@example.com');
    setValue(root, 'input[autocomplete="current-password"]', 'Abc123');
    submit(root);

    await eventually(() => {
      expect(root.textContent).toContain('henüz doğrulanmadı');
      expect(root.textContent).toContain('Doğrulama e-postasını tekrar gönder');
    });
  });

  it('shows the wrong-password message without offering the e-mail button', async () => {
    authStub.login.mockRejectedValue(
      new HttpErrorResponse({ status: 401, error: { errors: ['InvalidCredentials'] } }),
    );

    setValue(root, 'input[type="email"]', 'user@example.com');
    setValue(root, 'input[autocomplete="current-password"]', 'wrong');
    submit(root);

    await eventually(() => expect(root.textContent).toContain('E-posta veya parola hatalı.'));
    expect(root.textContent).not.toContain('Doğrulama e-postasını tekrar gönder');
  });
});