import { HttpErrorResponse } from '@angular/common/http';

const MESSAGES: Record<string, string> = {
  InvalidCredentials: 'E-posta veya parola hatalı.',
  AccountLocked: 'Çok fazla hatalı deneme yapıldı. Hesabınız geçici olarak kilitlendi, birkaç dakika sonra tekrar deneyin.',
  EmailNotConfirmed: 'E-posta adresiniz henüz doğrulanmadı. Gelen kutunuzdaki bağlantıyı kullanın veya doğrulama e-postasını aşağıdan yeniden isteyin.',
  TooManyRequests: 'Çok fazla istek gönderdiniz. Lütfen birkaç dakika bekleyip tekrar deneyin.',
  InvalidPage: 'Geçersiz sayfa numarası.',
  InvalidPageSize: 'Geçersiz sayfa boyutu.',
  InvalidAction: 'Geçersiz işlem türü.',
  InvalidDateRange: 'Başlangıç tarihi bitiş tarihinden sonra olamaz.',
  InvalidEmail: 'Geçerli bir e-posta adresi girin.',
  DuplicateEmail: 'Bu e-posta adresiyle zaten bir hesap var.',
  DuplicateUserName: 'Bu e-posta adresiyle zaten bir hesap var.',
  PasswordRequired: 'Parola gerekli.',
  PasswordTooShort: 'Parola en az 6 karakter olmalı.',
  PasswordTooLong: 'Parola en fazla 128 karakter olabilir.',
  PasswordRequiresDigit: 'Parola en az bir rakam içermeli.',
  PasswordRequiresLower: 'Parola en az bir küçük harf içermeli.',
  PasswordRequiresUpper: 'Parola en az bir büyük harf içermeli.',
  PasswordRequiresNonAlphanumeric: 'Parola en az bir özel karakter içermeli.',
  InvalidToken: 'Doğrulama bağlantısı geçersiz veya süresi dolmuş.',
  InvalidAntiforgeryToken: 'Güvenlik doğrulaması başarısız oldu. Sayfayı yenileyip tekrar deneyin.',
  RoleAssignmentFailed: 'Hesap oluşturulamadı. Lütfen tekrar deneyin.',
  NetworkError: 'Sunucuya ulaşılamadı. Bağlantınızı kontrol edin.',
};

const FALLBACK = 'Beklenmeyen bir hata oluştu. Lütfen tekrar deneyin.';

/** Reads the error codes the API returns in the "errors" field of its problem responses. */
export function getErrorCodes(error: unknown): string[] {
  if (error instanceof HttpErrorResponse) {
    if (error.status === 0) {
      return ['NetworkError'];
    }

    const codes = (error.error as { errors?: unknown } | null)?.errors;

    if (Array.isArray(codes) && codes.every((code) => typeof code === 'string')) {
      return codes as string[];
    }

    // The rate limiter answers 429 without a body.
    if (error.status === 429) {
      return ['TooManyRequests'];
    }
  }

  return [];
}

/** Turns error codes into user-facing Turkish messages (without duplicates). */
export function toMessages(codes: string[]): string[] {
  const messages = codes.map((code) => MESSAGES[code] ?? FALLBACK);

  return messages.length > 0 ? [...new Set(messages)] : [FALLBACK];
}