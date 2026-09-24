import { Component, OnInit, inject, signal } from '@angular/core';
import { ActivatedRoute, Router, RouterLink } from '@angular/router';
import { DecimalPipe } from '@angular/common';
import { MatIconModule } from '@angular/material/icon';
import { StoreService, StoreResponse } from '../../core/stores/store';
import { ProductService, ProductSummaryResponse } from '../../core/products/product';
import { WishlistService } from '../../core/wishlist/wishlist';
import { ReviewService, ReviewResponse } from '../../core/reviews/review';
import { AuthService } from '../../core/auth/auth';
import { AppHeader } from '../../shared/app-header/app-header';

@Component({
  selector: 'app-store-profile',
  standalone: true,
  imports: [RouterLink, DecimalPipe, MatIconModule, AppHeader],
  templateUrl: './store-profile.html',
  styleUrl: './store-profile.scss'
})
export class StoreProfile implements OnInit {
  private route = inject(ActivatedRoute);
  private storeService = inject(StoreService);
  private productService = inject(ProductService);
  private wishlistService = inject(WishlistService);
  private reviewService = inject(ReviewService);
  private authService = inject(AuthService);
  private router = inject(Router);

  store = signal<StoreResponse | null>(null);
  products = signal<ProductSummaryResponse[]>([]);
  reviews = signal<ReviewResponse[]>([]);
  isLoading = signal(true);

  readonly stars = [1, 2, 3, 4, 5];

  ngOnInit(): void {
    const id = this.route.snapshot.paramMap.get('id');
    if (!id) return;

    this.storeService.getById(id).subscribe({
      next: (store) => {
        this.store.set(store);
        this.isLoading.set(false);
      },
      error: () => this.isLoading.set(false)
    });

    this.productService.getProducts({ storeId: id, pageSize: 50 }).subscribe((result) => {
      this.products.set(result.items);
    });

    this.reviewService.getByStore(id).subscribe((data) => this.reviews.set(data));
  }

  toggleWishlist(event: Event, product: ProductSummaryResponse): void {
    event.stopPropagation();
    event.preventDefault();

    if (!this.authService.isLoggedIn()) {
      this.router.navigate(['/login']);
      return;
    }

    this.wishlistService.toggle(product.id).subscribe((result) => {
      this.products.update((items) =>
        items.map((p) => (p.id === product.id ? { ...p, isWishlisted: result.wishlisted } : p))
      );
    });
  }

  formatPrice(price: number): string {
    return new Intl.NumberFormat('id-ID', { style: 'currency', currency: 'IDR', maximumFractionDigits: 0 }).format(price);
  }

  round(value: number): number {
    return Math.round(value);
  }

  formatDate(date: string): string {
    return new Intl.DateTimeFormat('id-ID', { dateStyle: 'medium' }).format(new Date(date));
  }
}
