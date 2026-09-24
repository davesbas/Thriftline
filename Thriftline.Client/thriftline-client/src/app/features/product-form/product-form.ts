import { Component, OnInit, inject, signal } from '@angular/core';
import { FormsModule, FormBuilder, ReactiveFormsModule, Validators } from '@angular/forms';
import { ActivatedRoute, Router } from '@angular/router';
import { MatButtonModule } from '@angular/material/button';
import { MatFormFieldModule } from '@angular/material/form-field';
import { MatInputModule } from '@angular/material/input';
import { MatSelectModule } from '@angular/material/select';
import { MatIconModule } from '@angular/material/icon';
import { MatSnackBar } from '@angular/material/snack-bar';
import { ProductService, CategoryResponse, ProductMediaItem } from '../../core/products/product';
import { UploadService, MEDIA_TYPE_VIDEO } from '../../core/uploads/upload';
import { AppHeader } from '../../shared/app-header/app-header';

@Component({
  selector: 'app-product-form',
  standalone: true,
  imports: [
    FormsModule,
    ReactiveFormsModule,
    MatButtonModule,
    MatFormFieldModule,
    MatInputModule,
    MatSelectModule,
    MatIconModule,
    AppHeader
  ],
  templateUrl: './product-form.html',
  styleUrl: './product-form.scss'
})
export class ProductForm implements OnInit {
  private fb = inject(FormBuilder);
  private route = inject(ActivatedRoute);
  private router = inject(Router);
  private productService = inject(ProductService);
  private uploadService = inject(UploadService);
  private snackBar = inject(MatSnackBar);

  categories = signal<CategoryResponse[]>([]);
  media = signal<ProductMediaItem[]>([]);
  isEditMode = signal(false);
  isSubmitting = signal(false);
  isUploading = signal(false);
  productId: string | null = null;

  readonly MEDIA_TYPE_VIDEO = MEDIA_TYPE_VIDEO;

  readonly conditions = [
    { value: 0, label: 'Baru' },
    { value: 1, label: 'Seperti Baru' },
    { value: 2, label: 'Baik' },
    { value: 3, label: 'Cukup Baik' },
    { value: 4, label: 'Kurang Baik' }
  ];

  readonly statuses = [
    { value: 0, label: 'Tersedia' },
    { value: 1, label: 'Dipesan' },
    { value: 2, label: 'Terjual' },
    { value: 3, label: 'Nonaktif' }
  ];

  form = this.fb.nonNullable.group({
    categoryId: ['', Validators.required],
    name: ['', Validators.required],
    description: [''],
    price: [0, [Validators.required, Validators.min(1)]],
    condition: [0, Validators.required],
    stock: [1, [Validators.required, Validators.min(0)]],
    status: [0]
  });

  ngOnInit(): void {
    this.productService.getCategories().subscribe((data) => this.categories.set(data));

    this.productId = this.route.snapshot.paramMap.get('id');
    this.isEditMode.set(!!this.productId);

    if (this.productId) {
      this.productService.getById(this.productId).subscribe((product) => {
        this.form.patchValue({
          categoryId: product.categoryId,
          name: product.name,
          description: product.description ?? '',
          price: product.price,
          condition: product.condition,
          stock: product.stock,
          status: product.status
        });
        this.media.set(product.media);
      });
    }
  }

  onFileSelected(event: Event): void {
    const input = event.target as HTMLInputElement;
    if (!input.files || input.files.length === 0) return;

    const files = Array.from(input.files);
    this.isUploading.set(true);

    let remaining = files.length;
    files.forEach((file) => {
      this.uploadService.uploadMedia(file).subscribe({
        next: (result) => {
          this.media.update((items) => [...items, { url: result.url, mediaType: result.mediaType }]);
          remaining -= 1;
          if (remaining === 0) this.isUploading.set(false);
        },
        error: (err) => {
          this.snackBar.open(this.extractError(err, 'Gagal upload file.'), 'Tutup', { duration: 5000 });
          remaining -= 1;
          if (remaining === 0) this.isUploading.set(false);
        }
      });
    });

    input.value = '';
  }

  removeMedia(index: number): void {
    this.media.update((items) => items.filter((_, i) => i !== index));
  }

  submit(): void {
    if (this.isSubmitting()) {
      return;
    }

    if (this.form.invalid) {
      this.form.markAllAsTouched();
      return;
    }

    const raw = this.form.getRawValue();
    this.isSubmitting.set(true);

    if (this.isEditMode() && this.productId) {
      this.productService
        .update(this.productId, { ...raw, media: this.media() })
        .subscribe({
          next: () => {
            this.isSubmitting.set(false);
            this.snackBar.open('Produk diperbarui.', 'Tutup', { duration: 3000 });
            this.router.navigate(['/store']);
          },
          error: (err) => {
            this.isSubmitting.set(false);
            this.snackBar.open(this.extractError(err, 'Gagal memperbarui produk.'), 'Tutup', { duration: 3000 });
          }
        });
    } else {
      this.productService
        .create({ ...raw, media: this.media() })
        .subscribe({
          next: () => {
            this.isSubmitting.set(false);
            this.snackBar.open('Produk ditambahkan.', 'Tutup', { duration: 3000 });
            this.router.navigate(['/store']);
          },
          error: (err) => {
            this.isSubmitting.set(false);
            this.snackBar.open(this.extractError(err, 'Gagal menambahkan produk.'), 'Tutup', { duration: 3000 });
          }
        });
    }
  }

  private extractError(err: unknown, fallback: string): string {
    const errorBody = (err as { error?: unknown })?.error;
    return typeof errorBody === 'string' ? errorBody : fallback;
  }
}
