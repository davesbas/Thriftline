import { Component, Inject, computed, inject, signal } from '@angular/core';
import { FormsModule } from '@angular/forms';
import { MatDialogRef, MAT_DIALOG_DATA, MatDialogModule } from '@angular/material/dialog';
import { MatIconModule } from '@angular/material/icon';
import { MatButtonModule } from '@angular/material/button';
import { ProductDetailResponse } from '../../core/products/product';

export interface ProductPickerData {
    products: ProductDetailResponse[];
}

@Component({
    selector: 'app-product-picker-dialog',
    standalone: true,
    imports: [FormsModule, MatDialogModule, MatIconModule, MatButtonModule],
    templateUrl: './product-picker-dialog.html',
    styleUrl: './product-picker-dialog.scss'
})
export class ProductPickerDialog {
    search = signal('');

    filteredProducts = computed(() => {
        const term = this.search().trim().toLowerCase();
        if (!term) return this.data.products;
        return this.data.products.filter((p) => p.name.toLowerCase().includes(term));
    });

    constructor(
        private dialogRef: MatDialogRef<ProductPickerDialog, ProductDetailResponse | null>,
        @Inject(MAT_DIALOG_DATA) public data: ProductPickerData
    ){}

    choose(product: ProductDetailResponse): void {
        this.dialogRef.close(product);
    }

    close(): void {
        this.dialogRef.close(null);
    }

    formatPrice(price: number): string {
        return new Intl.NumberFormat('id-ID', { style: 'currency', currency: 'IDR', maximumFractionDigits: 0 }).format(price);
    }
}
