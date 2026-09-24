import { Component, OnInit, inject, signal } from '@angular/core';
import { RouterLink } from '@angular/router';
import { MatIconModule } from '@angular/material/icon';
import { ProductSummaryResponse } from '../../core/products/product';
import { WishlistService } from '../../core/wishlist/wishlist';
import { AppHeader } from '../../shared/app-header/app-header';

@Component({
  selector: 'app-wishlist',
  standalone: true,
  imports: [RouterLink, MatIconModule, AppHeader],
  templateUrl: './wishlist.html',
  styleUrl: './wishlist.scss'
})
export class Wishlist implements OnInit {
  private wishlistService = inject(WishlistService);

  products = signal<ProductSummaryResponse[]>([]);
  isLoading = signal(true);

  ngOnInit(): void {
    this.load();
  }

  load(): void {
    this.isLoading.set(true);
    this.wishlistService.getMyWishlist().subscribe({
      next: (data) => {
        this.products.set(data);
        this.isLoading.set(false);
      },
      error: () => this.isLoading.set(false)
    });
  }

  removeFromWishlist(event: Event, product: ProductSummaryResponse): void {
    event.stopPropagation();
    event.preventDefault();

    this.wishlistService.toggle(product.id).subscribe(() => {
      this.products.update((items) => items.filter((p) => p.id !== product.id));
    });
  }

  formatPrice(price: number): string {
    return new Intl.NumberFormat('id-ID', { style: 'currency', currency: 'IDR', maximumFractionDigits: 0 }).format(price);
  }
}
