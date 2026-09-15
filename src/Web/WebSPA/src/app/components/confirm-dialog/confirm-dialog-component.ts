import { Component, inject } from '@angular/core';
import { MatDialogRef, MAT_DIALOG_DATA, MatDialogModule } from '@angular/material/dialog';
import { MatButtonModule } from '@angular/material/button';

@Component({
  selector: 'app-confirm-dialog-component',
  standalone: true,
  imports: [MatDialogModule, MatButtonModule],
  templateUrl: './confirm-dialog-component.html',
  styleUrl: './confirm-dialog-component.scss',
})
export class ConfirmDialogComponent {
  private dialogRef = inject<MatDialogRef<ConfirmDialogComponent>>(MatDialogRef);
  public data = inject<{ message: string; confirmLabel?: string; showCancel?: boolean }>(MAT_DIALOG_DATA);

  get confirmLabel(): string {
    return this.data.confirmLabel ?? 'Confirm';
  }

  get showCancel(): boolean {
    return this.data.showCancel ?? true;
  }

  onCancel(): void {
    this.dialogRef.close(false);
  }

  onConfirm(): void {
    this.dialogRef.close(true);
  }
}
