import { Component, OnInit, inject, signal, computed } from '@angular/core';
import { ActivatedRoute, Router, RouterLink } from '@angular/router';
import { MatButtonModule } from '@angular/material/button';
import { MatIconModule } from '@angular/material/icon';
import { MatSnackBar } from '@angular/material/snack-bar';
import { AuthService } from '../../core/auth/auth';
import { ProductService, ProductDetailResponse } from '../../core/products/product';
import { WishlistService } from '../../core/wishlist/wishlist';
import { CartService } from '../../core/cart/cart';
import { ConversationService } from '../../core/conversations/conversation';
import { ReviewService, ReviewResponse } from '../../core/reviews/review';
import { AppHeader } from '../../shared/app-header/app-header';
import { MEDIA_TYPE_VIDEO } from '../../core/uploads/upload';

@Component({
  selector: 'app-product-detail',
  standalone: true,
  imports: [MatButtonModule, MatIconModule, AppHeader, RouterLink],
  templateUrl: './product-detail.html',
  styleUrl: './product-detail.scss'
})
export class ProductDetail implements OnInit {
  private route = inject(ActivatedRoute);
  private router = inject(Router);
  private productService = inject(ProductService);
  private wishlistService = inject(WishlistService);
  private cartService = inject(CartService);
  private authService = inject(AuthService);
  private snackBar = inject(MatSnackBar);
  private conversationService = inject(ConversationService);
  private reviewService = inject(ReviewService);

  product = signal<ProductDetailResponse | null>(null);
  activeImageIndex = signal(0);
  quantity = signal(1);
  isLoading = signal(true);
  isAdding = signal(false);
  isStartingChat = signal(false);
  reviews = signal<ReviewResponse[]>([]);

  readonly stars = [1, 2, 3, 4, 5];
  readonly MEDIA_TYPE_VIDEO = MEDIA_TYPE_VIDEO;
  private readonly conditionLabels = ['Baru', 'Seperti Baru', 'Baik', 'Cukup Baik', 'Kurang Baik'];

  averageRating = computed(() => {
    const list = this.reviews();
    if (list.length === 0) return 0;
    return list.reduce((sum, r) => sum + r.rating, 0) / list.length;
  });

  ngOnInit(): void {
    const id = this.route.snapshot.paramMap.get('id');
    if (!id) {
      this.router.navigate(['/']);
      return;
    }

    this.productService.getById(id).subscribe({
      next: (data) => {
        this.product.set(data);
        this.isLoading.set(false);
      },
      error: () => {
        this.isLoading.set(false);
      }
    });

    this.reviewService.getByProduct(id).subscribe((data) => this.reviews.set(data));
  }

  round(value: number): number {
    return Math.round(value);
  }

  formatDate(date: string): string {
    return new Intl.DateTimeFormat('id-ID', { dateStyle: 'medium' }).format(new Date(date));
  }

  conditionLabel(condition: number): string {
    return this.conditionLabels[condition] ?? 'Tidak diketahui';
  }

  formatPrice(price: number): string {
    return new Intl.NumberFormat('id-ID', {
      style: 'currency',
      currency: 'IDR',
      maximumFractionDigits: 0
    }).format(price);
  }

  decreaseQuantity(): void {
    if (this.quantity() > 1) {
      this.quantity.update((q) => q - 1);
    }
  }

  increaseQuantity(): void {
    const stock = this.product()?.stock ?? 1;
    if (this.quantity() < stock) {
      this.quantity.update((q) => q + 1);
    }
  }

  toggleWishlist(): void {
    const product = this.product();
    if (!product) return;

    if (!this.authService.isLoggedIn()) {
      this.router.navigate(['/login']);
      return;
    }

    this.wishlistService.toggle(product.id).subscribe((result) => {
      this.product.update((p) => (p ? { ...p, isWishlisted: result.wishlisted } : p));
    });
  }

  addToCart(): void {
    const product = this.product();
    if (!product) return;

    if (!this.authService.isLoggedIn()) {
      this.router.navigate(['/login']);
      return;
    }

    this.isAdding.set(true);
    this.cartService.addToCart({ productId: product.id, quantity: this.quantity() }).subscribe({
      next: () => {
        this.isAdding.set(false);
        this.snackBar.open('Produk ditambahkan ke keranjang.', 'Tutup', { duration: 3000 });
      },
      error: (err) => {
        this.isAdding.set(false);
        const message = typeof err.error === 'string' ? err.error : 'Gagal menambahkan ke keranjang.';
        this.snackBar.open(message, 'Tutup', { duration: 3000 });
      }
    });
  }

  buyNow(): void {
    const product = this.product();
    if (!product) return;

    if (!this.authService.isLoggedIn()) {
      this.router.navigate(['/login']);
      return;
    }

    this.isAdding.set(true);
    this.cartService.addToCart({ productId: product.id, quantity: this.quantity() }).subscribe({
      next: () => {
        this.isAdding.set(false);
        this.snackBar.open('Ditambahkan ke keranjang. Halaman checkout segera hadir.', 'Tutup', { duration: 3000 });
      },
      error: (err) => {
        this.isAdding.set(false);
        const message = typeof err.error === 'string' ? err.error : 'Gagal memproses.';
        this.snackBar.open(message, 'Tutup', { duration: 3000 });
      }
    });
  }

  nextImage(): void {
    const product = this.product();
    if (!product) return;
    this.activeImageIndex.update((i) => (i + 1) % product.media.length);
  }

  prevImage(): void {
    const product = this.product();
    if (!product) return;
    this.activeImageIndex.update((i) => (i - 1 + product.media.length) % product.media.length);
  }

  startChat(): void {
    if (this.isStartingChat()) return;

    const product = this.product();
    if (!product) return;

    if (!this.authService.isLoggedIn()) {
      this.router.navigate(['/login']);
      return;
    }

    this.isStartingChat.set(true);
    this.conversationService.start({ storeId: product.storeId, productId: product.id }).subscribe({
      next: (conversation) => {
        this.isStartingChat.set(false);
        this.router.navigate(['/conversations', conversation.id], {
          state: {
            attachProduct: {
              id: product.id,
              name: product.name,
              price: product.price,
              imageUrl: product.media.find((m) => m.mediaType !== MEDIA_TYPE_VIDEO)?.url ?? null
            }
          }
        });
      },
      error: (err) => {
        this.isStartingChat.set(false);
        const message = typeof err.error === 'string' ? err.error : 'Gagal memulai chat.';
        this.snackBar.open(message, 'Tutup', { duration: 3000 });
      }
    });
  }
}
