import { Component, OnInit, inject, signal } from '@angular/core';
import { FormsModule } from '@angular/forms';
import { ActivatedRoute, Router, RouterLink } from '@angular/router';
import { MatIconModule } from '@angular/material/icon';
import { MatButtonModule } from '@angular/material/button';
import { MatDialog } from '@angular/material/dialog';
import { AuthService } from '../../core/auth/auth';
import { ForumService, ForumPostDetailResponse } from '../../core/forum/forum';
import { ProductService, ProductDetailResponse } from '../../core/products/product';
import { StoreService, StoreResponse } from '../../core/stores/store';
import { MEDIA_TYPE_VIDEO } from '../../core/uploads/upload';
import { AppHeader } from '../../shared/app-header/app-header';
import { ProductPickerDialog } from '../../shared/product-picker-dialog/product-picker-dialog';

@Component({
  selector: 'app-forum-post',
  standalone: true,
  imports: [FormsModule, RouterLink, MatIconModule, MatButtonModule, AppHeader],
  templateUrl: './forum-post.html',
  styleUrl: './forum-post.scss'
})
export class ForumPost implements OnInit {
  private route = inject(ActivatedRoute);
  private router = inject(Router);
  private forumService = inject(ForumService);
  private productService = inject(ProductService);
  private storeService = inject(StoreService);
  private dialog = inject(MatDialog);
  authService = inject(AuthService);

  post = signal<ForumPostDetailResponse | null>(null);
  myProducts = signal<ProductDetailResponse[]>([]);
  myStore = signal<StoreResponse | null>(null);
  isLoading = signal(true);
  newComment = '';
  selectedProductId: string | null = null;
  commentAsStoreId: string | null = null;
  isSubmitting = signal(false);

  readonly MEDIA_TYPE_VIDEO = MEDIA_TYPE_VIDEO;

  ngOnInit(): void {
    const id = this.route.snapshot.paramMap.get('id')!;
    this.load(id);

    if (this.authService.isLoggedIn()) {
      this.productService.getMyProducts().subscribe((products) => this.myProducts.set(products));
      this.storeService.getMyStore().subscribe((store) => this.myStore.set(store));
    }
  }

  load(id: string): void {
    this.isLoading.set(true);
    this.forumService.getById(id).subscribe({
      next: (post) => {
        this.post.set(post);
        this.isLoading.set(false);
      },
      error: () => this.isLoading.set(false)
    });
  }

  isPostOwner(): boolean {
    return this.authService.currentUser()?.userId === this.post()?.authorId;
  }

  isCommentOwner(authorId: string): boolean {
    return this.authService.currentUser()?.userId === authorId;
  }

  selectedProduct(): ProductDetailResponse | undefined {
    return this.myProducts().find((p) => p.id === this.selectedProductId);
  }

  openProductPicker(): void {
    if (this.myProducts().length === 0) return;

    const dialogRef = this.dialog.open(ProductPickerDialog, {
      data: { products: this.myProducts() },
      width: '420px',
      maxWidth: '95vw'
    });

    dialogRef.afterClosed().subscribe((product: ProductDetailResponse | null) => {
      if (product) {
        this.selectedProductId = product.id;
      }
    });
  }

  clearSelectedProduct(): void {
    this.selectedProductId = null;
  }

  toggleLike(): void {
    const post = this.post();
    if (!post) return;

    if (!this.authService.isLoggedIn()) {
      this.router.navigate(['/login']);
      return;
    }

    this.forumService.toggleLike(post.id).subscribe((result) => {
      this.post.update((p) => (p ? { ...p, isLikedByMe: result.liked, likeCount: result.likeCount } : p));
    });
  }

  deletePost(): void {
    const post = this.post();
    if (!post || !confirm('Hapus diskusi ini?')) return;

    this.forumService.remove(post.id).subscribe(() => {
      this.router.navigate(['/forum']);
    });
  }

  submitComment(): void {
    const post = this.post();
    if (!post || !this.newComment.trim() || this.isSubmitting()) return;

    if (!this.authService.isLoggedIn()) {
      this.router.navigate(['/login']);
      return;
    }

    this.isSubmitting.set(true);
    this.forumService.addComment(post.id, this.newComment.trim(), this.selectedProductId, this.commentAsStoreId).subscribe({
      next: (comment) => {
        this.post.update((p) => (p ? { ...p, comments: [...p.comments, comment] } : p));
        this.newComment = '';
        this.selectedProductId = null;
        this.isSubmitting.set(false);
      },
      error: () => this.isSubmitting.set(false)
    });
  }

  deleteComment(commentId: string): void {
    if (!confirm('Hapus komentar ini?')) return;

    this.forumService.removeComment(commentId).subscribe(() => {
      this.post.update((p) => (p ? { ...p, comments: p.comments.filter((c) => c.id !== commentId) } : p));
    });
  }

  formatDate(date: string): string {
    return new Intl.DateTimeFormat('id-ID', { dateStyle: 'medium', timeStyle: 'short' }).format(new Date(date));
  }

  formatPrice(price: number): string {
    return new Intl.NumberFormat('id-ID', { style: 'currency', currency: 'IDR', maximumFractionDigits: 0 }).format(price);
  }
}
