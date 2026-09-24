import { Component, Inject, signal } from '@angular/core';
import { MatDialogRef, MAT_DIALOG_DATA, MatDialogModule } from '@angular/material/dialog';
import { MatButtonModule } from '@angular/material/button';
import { ImageCropperComponent, ImageCroppedEvent } from 'ngx-image-cropper';

export interface CropDialogData {
  file: File;
}

@Component({
  selector: 'app-crop-dialog',
  standalone: true,
  imports: [MatDialogModule, MatButtonModule, ImageCropperComponent],
  templateUrl: './crop-dialog.html',
  styleUrl: './crop-dialog.scss'
})
export class CropDialog {
  croppedBlob: Blob | null = null;
  aspectRatio = signal(1 / 1);

  readonly aspectOptions = [
    { label: '1:1', value: 1 / 1 },
    { label: '4:5', value: 4 / 5 },
    { label: '16:9', value: 16 / 9 }
  ];

  constructor(
    private dialogRef: MatDialogRef<CropDialog, Blob | null>,
    @Inject(MAT_DIALOG_DATA) public data: CropDialogData
  ) {}

  onImageCropped(event: ImageCroppedEvent): void {
    this.croppedBlob = event.blob ?? null;
  }

  setAspect(value: number): void {
    this.aspectRatio.set(value);
  }

  use(): void {
    this.dialogRef.close(this.croppedBlob);
  }

  cancel(): void {
    this.dialogRef.close(null);
  }
}
