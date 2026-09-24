import { Component, OnInit, inject, signal } from '@angular/core';
import { FormBuilder, ReactiveFormsModule, Validators } from '@angular/forms';
import { RouterLink } from '@angular/router';
import { MatButtonModule } from '@angular/material/button';
import { MatFormFieldModule } from '@angular/material/form-field';
import { MatInputModule } from '@angular/material/input';
import { MatCheckboxModule } from '@angular/material/checkbox';
import { MatSnackBar } from '@angular/material/snack-bar';
import { StoreService, StoreResponse } from '../../core/stores/store';
import { ProductService, ProductDetailResponse } from '../../core/products/product';
import { AppHeader } from '../../shared/app-header/app-header';

const PHONE_PATTERN = /^(\+62|62|0)8[1-9][0-9]{6,10}$/;

@Component({
  selector: 'app-store-dashboard',
  standalone: true,
  imports: [ReactiveFormsModule, RouterLink, MatButtonModule, MatFormFieldModule, MatInputModule, MatCheckboxModule, AppHeader],
  templateUrl: './store-dashboard.html',
  styleUrl: './store-dashboard.scss'
})
export class StoreDashboard implements OnInit {
  private fb = inject(FormBuilder);
  private storeService = inject(StoreService);
  private productService = inject(ProductService);
  private snackBar = inject(MatSnackBar);

  store = signal<StoreResponse | null>(null);
  products = signal<ProductDetailResponse[]>([]);
  isLoading = signal(true);
  isSubmitting = signal(false);

  storeForm = this.fb.nonNullable.group({
    name: ['', Validators.required],
    description: [''],
    address: [''],
    phoneNumber: ['', [Validators.required, Validators.pattern(PHONE_PATTERN)]],
    agreedToTerms: [false, Validators.requiredTrue]
  });

  private readonly statusLabels = ['Tersedia', 'Dipesan', 'Terjual', 'Nonaktif'];

  ngOnInit(): void {
    this.loadStore();
  }

  loadStore(): void {
    this.isLoading.set(true);
    this.storeService.getMyStore().subscribe((store) => {
      this.store.set(store);
      this.isLoading.set(false);

      if (store) {
        this.loadProducts();
      }
    });
  }

  loadProducts(): void {
    this.productService.getMyProducts().subscribe((data) => this.products.set(data));
  }

  statusLabel(status: number): string {
    return this.statusLabels[status] ?? 'Tidak diketahui';
  }

  formatPrice(price: number): string {
    return new Intl.NumberFormat('id-ID', {
      style: 'currency',
      currency: 'IDR',
      maximumFractionDigits: 0
    }).format(price);
  }

  createStore(): void {
    if (this.storeForm.invalid) {
      this.storeForm.markAllAsTouched();
      return;
    }

    this.isSubmitting.set(true);
    this.storeService.create(this.storeForm.getRawValue()).subscribe({
      next: () => {
        this.isSubmitting.set(false);
        this.snackBar.open('Toko berhasil dibuat!', 'Tutup', { duration: 3000 });
        this.loadStore();
      },
      error: (err) => {
        this.isSubmitting.set(false);
        const message = typeof err.error === 'string' ? err.error : 'Gagal membuat toko.';
        this.snackBar.open(message, 'Tutup', { duration: 3000 });
      }
    });
  }

  deleteProduct(product: ProductDetailResponse): void {
    if (!confirm(`Hapus produk "${product.name}"?`)) return;

    this.productService.remove(product.id).subscribe({
      next: () => {
        this.snackBar.open('Produk dihapus.', 'Tutup', { duration: 3000 });
        this.loadProducts();
      },
      error: () => this.snackBar.open('Gagal menghapus produk.', 'Tutup', { duration: 3000 })
    });
  }
}
