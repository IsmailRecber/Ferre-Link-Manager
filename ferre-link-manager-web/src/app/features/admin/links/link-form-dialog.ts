import { Component, inject, signal } from '@angular/core';
import { takeUntilDestroyed } from '@angular/core/rxjs-interop';
import {
  AbstractControl,
  NonNullableFormBuilder,
  ReactiveFormsModule,
  ValidationErrors,
  ValidatorFn,
  Validators,
} from '@angular/forms';
import { MatButtonModule } from '@angular/material/button';
import { MatCheckboxModule } from '@angular/material/checkbox';
import { MAT_DIALOG_DATA, MatDialogModule, MatDialogRef } from '@angular/material/dialog';
import { MatFormFieldModule } from '@angular/material/form-field';
import { MatInputModule } from '@angular/material/input';
import { MatSelectModule } from '@angular/material/select';
import { ConfirmService } from '../../../shared/confirm/confirm.service';
import { Category } from '../categories/categories.service';
import { LinkFormValue, ROLE_OPTIONS } from './links-admin.service';

export interface LinkFormData {
  title: string;
  categories: Category[];
  initial: LinkFormValue;
  /** The question shown after the user presses Save. */
  confirm: { title: string; message: (value: LinkFormValue) => string; confirmText: string };
  /** Sends the change to the API. It throws when the request fails. */
  save: (value: LinkFormValue) => Promise<void>;
  /** Turns an error into a message for the user. */
  describeError: (error: unknown) => string;
}

const TITLE_MAX = 200;
const URL_MAX = 2048;

const notBlank: ValidatorFn = (control: AbstractControl): ValidationErrors | null =>
  String(control.value ?? '').trim() ? null : { blank: true };

// The API accepts only absolute http and https addresses without credentials. This is a convenience
// check for the user. The API checks the address again.
const httpUrl: ValidatorFn = (control: AbstractControl): ValidationErrors | null => {
  const value = String(control.value ?? '').trim();

  if (!value) {
    return null;
  }

  try {
    const url = new URL(value);

    if (url.protocol !== 'http:' && url.protocol !== 'https:') {
      return { scheme: true };
    }

    return url.username || url.password ? { credentials: true } : null;
  } catch {
    return { invalidUrl: true };
  }
};

const atLeastOneRole: ValidatorFn = (group: AbstractControl): ValidationErrors | null =>
  Object.values((group as { value: Record<string, boolean> }).value).some(Boolean) ? null : { noRole: true };

/** Closes with true when the change was saved, otherwise with false or nothing. */
@Component({
  selector: 'app-link-form-dialog',
  imports: [
    ReactiveFormsModule,
    MatDialogModule,
    MatButtonModule,
    MatCheckboxModule,
    MatFormFieldModule,
    MatInputModule,
    MatSelectModule,
  ],
  template: `
    <h2 mat-dialog-title>{{ data.title }}</h2>
    <form [formGroup]="form" (ngSubmit)="submit()" novalidate>
      <mat-dialog-content>
        <div class="dialog-form">
          <mat-form-field appearance="outline">
            <mat-label>Başlık</mat-label>
            <input matInput formControlName="title" [attr.maxlength]="titleMax" autocomplete="off" />
            <mat-hint align="end">{{ form.controls.title.value.length }}/{{ titleMax }}</mat-hint>
            @if (form.controls.title.hasError('blank')) {
              <mat-error>Başlık boş olamaz.</mat-error>
            } @else if (form.controls.title.hasError('maxlength')) {
              <mat-error>Başlık en fazla {{ titleMax }} karakter olabilir.</mat-error>
            }
          </mat-form-field>

          <mat-form-field appearance="outline">
            <mat-label>Adres</mat-label>
            <input
              matInput
              type="url"
              formControlName="url"
              placeholder="https://ornek.com"
              autocomplete="off"
            />
            @if (form.controls.url.hasError('blank')) {
              <mat-error>Adres boş olamaz.</mat-error>
            } @else if (form.controls.url.hasError('scheme')) {
              <mat-error>Yalnızca http:// ve https:// adreslerine izin verilir.</mat-error>
            } @else if (form.controls.url.hasError('credentials')) {
              <mat-error>Adres kullanıcı adı veya parola içeremez.</mat-error>
            } @else if (form.controls.url.hasError('invalidUrl')) {
              <mat-error>Geçerli bir adres girin (örneğin https://ornek.com).</mat-error>
            } @else if (form.controls.url.hasError('maxlength')) {
              <mat-error>Adres en fazla {{ urlMax }} karakter olabilir.</mat-error>
            }
          </mat-form-field>

          <mat-form-field appearance="outline">
            <mat-label>Kategori</mat-label>
            <mat-select formControlName="categoryId">
              @for (category of data.categories; track category.id) {
                <mat-option [value]="category.id">
                  {{ category.name }}{{ category.isActive ? '' : ' (pasif)' }}
                </mat-option>
              }
            </mat-select>
            @if (form.controls.categoryId.hasError('required')) {
              <mat-error>Bir kategori seçin.</mat-error>
            }
          </mat-form-field>

          <div formGroupName="roles" class="dialog-roles">
            <span class="dialog-roles-label">Bu bağlantıyı görebilecek roller</span>
            <div class="dialog-roles-options">
              @for (role of roleOptions; track role.value) {
                <mat-checkbox [formControlName]="role.value">{{ role.label }}</mat-checkbox>
              }
            </div>
            @if (form.controls.roles.touched && form.controls.roles.hasError('noRole')) {
              <p class="dialog-field-error">En az bir rol seçin.</p>
            }
                          <p class="dialog-field-hint">
              Yöneticiler her bağlantıyı görür. "Kullanıcı" seçilirse normal kullanıcılar da görür.
            </p>
          </div>

          @if (error(); as message) {
            <div class="auth-message auth-error" role="alert">
              <p>{{ message }}</p>
            </div>
          }
        </div>
      </mat-dialog-content>
      <mat-dialog-actions align="end">
        <button mat-button type="button" mat-dialog-close [disabled]="busy()">Vazgeç</button>
        <button mat-flat-button type="submit" [disabled]="busy()">
          {{ busy() ? 'Kaydediliyor...' : 'Kaydet' }}
        </button>
      </mat-dialog-actions>
    </form>
  `,
  styles: [
    `
      .dialog-form {
        display: flex;
        flex-direction: column;
        gap: 4px;
        min-width: 0;
        padding-top: 6px;
      }

      .dialog-roles-label {
        display: block;
        margin-bottom: 4px;
        font-size: 0.85rem;
        color: var(--mat-sys-on-surface-variant, #5f6368);
      }

      .dialog-roles-options {
        display: flex;
        flex-wrap: wrap;
        gap: 4px 20px;
      }

      .dialog-field-error {
        margin: 4px 0 0;
        font-size: 0.8rem;
        color: var(--mat-sys-error, #b3261e);
      }

            .dialog-field-hint {
        margin: 6px 0 0;
        font-size: 0.8rem;
        color: var(--mat-sys-on-surface-variant, #5f6368);
      }
    `,
  ],
})
export class LinkFormDialog {
  protected readonly data = inject<LinkFormData>(MAT_DIALOG_DATA);
  private readonly dialogRef = inject<MatDialogRef<LinkFormDialog, boolean>>(MatDialogRef);
  private readonly formBuilder = inject(NonNullableFormBuilder);
  private readonly confirmService = inject(ConfirmService);

  protected readonly titleMax = TITLE_MAX;
  protected readonly urlMax = URL_MAX;
  protected readonly roleOptions = ROLE_OPTIONS;
  protected readonly busy = signal(false);
  protected readonly error = signal<string | null>(null);

  protected readonly form = this.formBuilder.group({
    title: [this.data.initial.title, [notBlank, Validators.maxLength(TITLE_MAX)]],
    url: [this.data.initial.url, [notBlank, Validators.maxLength(URL_MAX), httpUrl]],
    categoryId: [this.data.initial.categoryId, Validators.required],
    roles: this.formBuilder.group(
      {
        Admin: [this.data.initial.roles.includes('Admin')],
        User: [this.data.initial.roles.includes('User')],
      },
      { validators: atLeastOneRole },
    ),
  });

  constructor() {
    // An old error message must disappear as soon as the user starts correcting the form.
    this.form.valueChanges.pipe(takeUntilDestroyed()).subscribe(() => this.error.set(null));
  }

  protected async submit(): Promise<void> {
    if (this.busy()) {
      return;
    }

    if (this.form.invalid) {
      this.form.markAllAsTouched();
      return;
    }

    const value = this.currentValue();

    // Nothing was changed, so there is nothing to save.
    if (!this.hasChanged(value)) {
      this.dialogRef.close(false);
      return;
    }

    this.error.set(null);

    const confirmed = await this.confirmService.confirm({
      title: this.data.confirm.title,
      message: this.data.confirm.message(value),
      confirmText: this.data.confirm.confirmText,
    });

    if (!confirmed) {
      return;
    }

    this.busy.set(true);
    this.dialogRef.disableClose = true;

    try {
      await this.data.save(value);
      this.dialogRef.close(true);
    } catch (error) {
      // The window stays open and shows the reason, so the user can fix the form and try again.
      this.error.set(this.data.describeError(error));
    } finally {
      this.busy.set(false);
      this.dialogRef.disableClose = false;
    }
  }

  private currentValue(): LinkFormValue {
    const raw = this.form.getRawValue();

    return {
      title: raw.title.trim(),
      url: raw.url.trim(),
      categoryId: raw.categoryId,
      roles: ROLE_OPTIONS.filter((option) => raw.roles[option.value]).map((option) => option.value),
    };
  }

  private hasChanged(value: LinkFormValue): boolean {
    const initial = this.data.initial;
    const roles = (list: string[]) => [...list].sort().join('|');

    return (
      value.title !== initial.title.trim() ||
      value.url !== initial.url.trim() ||
      value.categoryId !== initial.categoryId ||
      roles(value.roles) !== roles(initial.roles)
    );
  }
}