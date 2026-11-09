import { ValidatorFn, Validators } from '@angular/forms';

// Same rules as the API: at least 6 characters, one uppercase letter, one lowercase letter, one digit.
const hasUpper: ValidatorFn = (control) => (/[A-Z]/.test(control.value ?? '') ? null : { upper: true });
const hasLower: ValidatorFn = (control) => (/[a-z]/.test(control.value ?? '') ? null : { lower: true });
const hasDigit: ValidatorFn = (control) => (/[0-9]/.test(control.value ?? '') ? null : { digit: true });

export const passwordRules: ValidatorFn[] = [
  Validators.required,
  Validators.minLength(6),
  Validators.maxLength(128),
  hasUpper,
  hasLower,
  hasDigit,
];