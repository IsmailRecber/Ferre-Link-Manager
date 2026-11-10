import { Injectable, inject } from '@angular/core';
import { MatDialog } from '@angular/material/dialog';
import { firstValueFrom } from 'rxjs';
import { ConfirmDialog, ConfirmDialogData } from './confirm-dialog';

/** Asks the user to confirm an action. Resolves to true only when the user confirms. */
@Injectable({ providedIn: 'root' })
export class ConfirmService {
  private readonly dialog = inject(MatDialog);

  async confirm(data: ConfirmDialogData): Promise<boolean> {
    const reference = this.dialog.open<ConfirmDialog, ConfirmDialogData, boolean>(ConfirmDialog, {
      data,
      role: 'alertdialog',
      width: '420px',
      maxWidth: '92vw',
      autoFocus: false,
    });

    return (await firstValueFrom(reference.afterClosed())) === true;
  }
}