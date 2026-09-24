import { Component, OnInit, inject, signal } from '@angular/core';
import { FormsModule } from '@angular/forms';
import { Router, RouterLink } from '@angular/router';
import { MatIconModule } from '@angular/material/icon';
import { MatMenuModule } from '@angular/material/menu';
import { ProductService, CategoryResponse, ProductSummaryResponse } from '../../core/products/product';
import { WishlistService } from '../../core/wishlist/wishlist';
import { AuthService } from '../../core/auth/auth';
import { AppHeader } from '../../shared/app-header/app-header';

@Component({
  selector: 'app-home',
  standalone: true,
  imports: [FormsModule, RouterLink, MatIconModule, MatMenuModule, AppHeader],
  templateUrl: './home.html',
  styleUrl: './home.scss'
})
export class Home implements OnInit {
  private productService = inject(ProductService);
  private wishlistService = inject(WishlistService);
  private authService = inject(AuthService);
  private router = inject(Router);

  categories = signal<CategoryResponse[]>([]);
  products = signal<ProductSummaryResponse[]>([]);
  selectedCategoryId = signal<string | null>(null);
  searchTerm = signal('');
  isLoading = signal(false);

  ngOnInit(): void {
    this.productService.getCategories().subscribe((data) => this.categories.set(data));
    this.loadProducts();
  }

  loadProducts(): void {
    this.isLoading.set(true);

    this.productService
      .getProducts({
        search: this.searchTerm() || undefined,
        categoryId: this.selectedCategoryId() ?? undefined
      })
      .subscribe({
        next: (result) => {
          this.products.set(result.items);
          this.isLoading.set(false);
        },
        error: () => this.isLoading.set(false)
      });
  }

  onSearchSubmit(): void {
    this.loadProducts();
  }

  selectCategory(categoryId: string | null): void {
    this.selectedCategoryId.set(categoryId);
    this.loadProducts();
  }

  isCategoryOrChildSelected(category: CategoryResponse): boolean {
    const selected = this.selectedCategoryId();
    if (!selected) return false;
    return selected === category.id || category.subCategories.some((s) => s.id === selected);
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
    return new Intl.NumberFormat('id-ID', { style: 'currency', currency: 'IDR', maximumFractionDigits: 0 }).format(
      price
    );
  }
}
