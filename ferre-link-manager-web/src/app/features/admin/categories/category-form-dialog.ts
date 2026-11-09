import { Component, inject, signal } from '@angular/core';
import { takeUntilDestroyed } from '@angular/core/rxjs-interop';
import {
  AbstractControl,
  NonNullableFormBuilder,
  ReactiveFormsModule,
  ValidationErrors,
  Validators,
} from '@angular/forms';
import { MatButtonModule } from '@angular/material/button';
import { MAT_DIALOG_DATA, MatDialogModule, MatDialogRef } from '@angular/material/dialog';
import { MatFormFieldModule } from '@angular/material/form-field';
import { MatInputModule } from '@angular/material/input';
import { ConfirmService } from '../../../shared/confirm/confirm.service';

export interface CategoryFormData {
  title: string;
  name: string;
  /** The question shown after the user presses Save. The message is built from the typed name. */
  confirm: { title: string; message: (name: string) => string; confirmText: string };
  /** Sends the change to the API. It throws when the request fails. */
  save: (name: string) => Promise<void>;
  /** Turns an error into a message for the user. */
  describeError: (error: unknown) => string;
}

const MAX_LENGTH = 100;

// A name made only of spaces is not a name.
const notBlank = (control: AbstractControl): ValidationErrors | null =>
  String(control.value ?? '').trim() ? null : { blank: true };

/** Closes with true when the change was saved, otherwise with false or nothing. */
@Component({
  selector: 'app-category-form-dialog',
  imports: [
    ReactiveFormsModule,
    MatDialogModule,
    MatButtonModule,
    MatFormFieldModule,
    MatInputModule,
  ],
  template: `
    <h2 mat-dialog-title>{{ data.title }}</h2>
    <form [formGroup]="form" (ngSubmit)="submit()" novalidate>
      <mat-dialog-content>
        <mat-form-field appearance="outline" class="category-name-field">
          <mat-label>Kategori adı</mat-label>
          <input matInput formControlName="name" [attr.maxlength]="maxLength" autocomplete="off" />
          <mat-hint align="end">{{ form.controls.name.value.length }}/{{ maxLength }}</mat-hint>
          @if (form.controls.name.hasError('blank')) {
            <mat-error>Kategori adı boş olamaz.</mat-error>
          } @else if (form.controls.name.hasError('maxlength')) {
            <mat-error>Kategori adı en fazla {{ maxLength }} karakter olabilir.</mat-error>
          }
        </mat-form-field>

        @if (error(); as message) {
          <div class="auth-message auth-error" role="alert">
            <p>{{ message }}</p>
          </div>
        }
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
      .category-name-field {
        width: 100%;
        min-width: 260px;
      }
    `,
  ],
})
export class CategoryFormDialog {
  protected readonly data = inject<CategoryFormData>(MAT_DIALOG_DATA);
  private readonly dialogRef = inject<MatDialogRef<CategoryFormDialog, boolean>>(MatDialogRef);
  private readonly formBuilder = inject(NonNullableFormBuilder);
  private readonly confirmService = inject(ConfirmService);

  protected readonly maxLength = MAX_LENGTH;
  protected readonly busy = signal(false);
  protected readonly error = signal<string | null>(null);

  protected readonly form = this.formBuilder.group({
    name: [this.data.name, [notBlank, Validators.maxLength(MAX_LENGTH)]],
  });

  constructor() {
    // An old error message must disappear as soon as the user starts correcting the name.
    this.form.controls.name.valueChanges.pipe(takeUntilDestroyed()).subscribe(() => this.error.set(null));
  }

  protected async submit(): Promise<void> {
    if (this.busy()) {
      return;
    }

    if (this.form.invalid) {
      this.form.markAllAsTouched();
      return;
    }

    const name = this.form.controls.name.value.trim();

    // Nothing was changed, so there is nothing to save.
    if (name === this.data.name) {
      this.dialogRef.close(false);
      return;
    }

    this.error.set(null);

    const confirmed = await this.confirmService.confirm({
      title: this.data.confirm.title,
      message: this.data.confirm.message(name),
      confirmText: this.data.confirm.confirmText,
    });

    if (!confirmed) {
      return;
    }

    this.busy.set(true);
    this.dialogRef.disableClose = true;

    try {
      await this.data.save(name);
      this.dialogRef.close(true);
    } catch (error) {
      // The window stays open and shows the reason, so the user can fix the name and try again.
      this.error.set(this.data.describeError(error));
    } finally {
      this.busy.set(false);
      this.dialogRef.disableClose = false;
    }
  }
}