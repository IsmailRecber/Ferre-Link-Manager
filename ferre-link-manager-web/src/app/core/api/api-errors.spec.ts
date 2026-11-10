import { HttpErrorResponse } from '@angular/common/http';
import { describe, expect, it } from 'vitest';
import { getErrorCodes, toMessages } from './api-errors';

describe('getErrorCodes', () => {
  it('reads the codes from the "errors" field of an API response', () => {
    const error = new HttpErrorResponse({ status: 401, error: { errors: ['InvalidCredentials'] } });

    expect(getErrorCodes(error)).toEqual(['InvalidCredentials']);
  });

  it('reports a network problem when the status is 0', () => {
    const error = new HttpErrorResponse({ status: 0 });

    expect(getErrorCodes(error)).toEqual(['NetworkError']);
  });

  it('maps a 429 answer without a body to TooManyRequests', () => {
    const error = new HttpErrorResponse({ status: 429 });

    expect(getErrorCodes(error)).toEqual(['TooManyRequests']);
  });

  it('returns no codes for errors it does not know', () => {
    expect(getErrorCodes(new Error('boom'))).toEqual([]);
    expect(getErrorCodes(new HttpErrorResponse({ status: 500, error: 'text' }))).toEqual([]);
  });
});

describe('toMessages', () => {
  it('turns a known code into its Turkish message', () => {
    expect(toMessages(['InvalidCredentials'])).toEqual(['E-posta veya parola hatalı.']);
  });

  it('uses a generic message for an unknown code', () => {
    expect(toMessages(['SomethingNew'])).toEqual(['Beklenmeyen bir hata oluştu. Lütfen tekrar deneyin.']);
  });

  it('uses a generic message when there are no codes', () => {
    expect(toMessages([])).toEqual(['Beklenmeyen bir hata oluştu. Lütfen tekrar deneyin.']);
  });

  it('removes duplicate messages', () => {
    expect(toMessages(['DuplicateEmail', 'DuplicateUserName'])).toHaveLength(1);
  });
});