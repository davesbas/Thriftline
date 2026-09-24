import { Component, OnInit, inject, signal } from '@angular/core';
import { FormsModule } from '@angular/forms';
import { Router, RouterLink } from '@angular/router';
import { MatIconModule } from '@angular/material/icon';
import { MatButtonModule } from '@angular/material/button';
import { MatMenuModule } from '@angular/material/menu';
import { MatSnackBar } from '@angular/material/snack-bar';
import { MatDialog } from '@angular/material/dialog';
import { AuthService } from '../../core/auth/auth';
import { ForumService, ForumPostSummaryResponse, ForumMediaItem } from '../../core/forum/forum';
import { ProductService, ProductDetailResponse } from '../../core/products/product';
import { StoreService, StoreResponse } from '../../core/stores/store';
import { UploadService, MEDIA_TYPE_VIDEO } from '../../core/uploads/upload';
import { AppHeader } from '../../shared/app-header/app-header';
import { CropDialog } from '../../shared/crop-dialog/crop-dialog';
import { ProductPickerDialog } from '../../shared/product-picker-dialog/product-picker-dialog';

@Component({
  selector: 'app-forum',
  standalone: true,
  imports: [FormsModule, RouterLink, MatIconModule, MatButtonModule, MatMenuModule, AppHeader],
  templateUrl: './forum.html',
  styleUrl: './forum.scss'
})
export class Forum implements OnInit {
  private forumService = inject(ForumService);
  private productService = inject(ProductService);
  private storeService = inject(StoreService);
  private uploadService = inject(UploadService);
  private authService = inject(AuthService);
  private router = inject(Router);
  private snackBar = inject(MatSnackBar);
  private dialog = inject(MatDialog);

  posts = signal<ForumPostSummaryResponse[]>([]);
  myProducts = signal<ProductDetailResponse[]>([]);
  myStore = signal<StoreResponse | null>(null);
  isLoading = signal(true);
  showComposer = signal(false);
  isSubmitting = signal(false);
  isUploading = signal(false);
  media = signal<ForumMediaItem[]>([]);
  previewIndex = signal(0);
  selectedProductId: string | null = null;
  postAsStoreId: string | null = null;

  readonly MEDIA_TYPE_VIDEO = MEDIA_TYPE_VIDEO;
  private readonly avatarColors = ['#4f6bff', '#22c55e', '#f59e0b', '#a855f7', '#ef4444', '#0ea5e9'];

  newTitle = '';
  newContent = '';

  ngOnInit(): void {
    this.load();
    if (this.authService.isLoggedIn()) {
      this.storeService.getMyStore().subscribe((store) => this.myStore.set(store));
    }
  }

  load(): void {
    this.isLoading.set(true);
    this.forumService.getAll().subscribe({
      next: (result) => {
        this.posts.set(result.items);
        this.isLoading.set(false);
      },
      error: () => this.isLoading.set(false)
    });
  }

  toggleComposer(): void {
    if (!this.authService.isLoggedIn()) {
      this.router.navigate(['/login']);
      return;
    }
    if (!this.showComposer() && this.myProducts().length === 0) {
      this.productService.getMyProducts().subscribe((products) => this.myProducts.set(products));
    }
    this.showComposer.update((v) => !v);
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

  onFileSelected(event: Event): void {
    const input = event.target as HTMLInputElement;
    if (!input.files || input.files.length === 0) return;

    const files = Array.from(input.files);
    input.value = '';

    this.processFiles(files);
  }

  private processFiles(files: File[]): void {
    if (files.length === 0) return;

    const [file, ...rest] = files;

    if (file.type.startsWith('video/')) {
      this.uploadFile(file, () => this.processFiles(rest));
      return;
    }

    const dialogRef = this.dialog.open(CropDialog, {
      data: { file },
      width: '480px',
      maxWidth: '95vw'
    });

    dialogRef.afterClosed().subscribe((croppedBlob: Blob | null) => {
      if (croppedBlob) {
        const croppedFile = new File([croppedBlob], `crop-${Date.now()}.jpg`, { type: 'image/jpeg' });
        this.uploadFile(croppedFile, () => this.processFiles(rest));
      } else {
        this.processFiles(rest);
      }
    });
  }

  private uploadFile(file: File, onDone: () => void): void {
    this.isUploading.set(true);
    this.uploadService.uploadMedia(file).subscribe({
      next: (result) => {
        this.media.update((items) => [...items, { url: result.url, mediaType: result.mediaType }]);
        this.previewIndex.set(this.media().length - 1);
        this.isUploading.set(false);
        onDone();
      },
      error: (err) => {
        this.snackBar.open(this.extractError(err, 'Gagal upload file.'), 'Tutup', { duration: 5000 });
        this.isUploading.set(false);
        onDone();
      }
    });
  }

  setPreview(index: number): void {
    this.previewIndex.set(index);
  }

  nextPreview(): void {
    this.previewIndex.update((i) => (i + 1) % this.media().length);
  }

  prevPreview(): void {
    this.previewIndex.update((i) => (i - 1 + this.media().length) % this.media().length);
  }

  removeMedia(index: number): void {
    this.media.update((items) => items.filter((_, i) => i !== index));
    this.previewIndex.update((i) => Math.max(0, Math.min(i, this.media().length - 1)));
  }

  submitPost(): void {
    if (!this.newTitle.trim() || !this.newContent.trim()) {
      this.snackBar.open('Judul dan isi diskusi wajib diisi.', 'Tutup', { duration: 3000 });
      return;
    }
    if (this.isSubmitting()) return;

    this.isSubmitting.set(true);
    this.forumService
      .create({
        title: this.newTitle.trim(),
        content: this.newContent.trim(),
        media: this.media(),
        productId: this.selectedProductId,
        storeId: this.postAsStoreId
      })
      .subscribe({
        next: (post) => {
          this.posts.update((items) => [
            {
              id: post.id,
              title: post.title,
              authorId: post.authorId,
              authorName: post.authorName,
              authorAvatarUrl: post.authorAvatarUrl,
              postedAsStoreId: post.postedAsStoreId,
              commentCount: 0,
              likeCount: 0,
              isLikedByMe: false,
              thumbnailUrl: post.media.find((m) => m.mediaType !== MEDIA_TYPE_VIDEO)?.url ?? null,
              productId: post.productId,
              productName: post.productName,
              productPrice: post.productPrice,
              productImageUrl: post.productImageUrl,
              createdAt: post.createdAt
            },
            ...items
          ]);
          this.newTitle = '';
          this.newContent = '';
          this.media.set([]);
          this.previewIndex.set(0);
          this.selectedProductId = null;
          this.postAsStoreId = null;
          this.showComposer.set(false);
          this.isSubmitting.set(false);
        },
        error: (err) => {
          this.isSubmitting.set(false);
          this.snackBar.open(this.extractError(err, 'Gagal membuat diskusi.'), 'Tutup', { duration: 5000 });
        }
      });
  }

  isOwner(post: ForumPostSummaryResponse): boolean {
    return this.authService.currentUser()?.userId === post.authorId;
  }

  toggleLike(event: Event, post: ForumPostSummaryResponse): void {
    event.stopPropagation();
    event.preventDefault();

    if (!this.authService.isLoggedIn()) {
      this.router.navigate(['/login']);
      return;
    }

    this.forumService.toggleLike(post.id).subscribe((result) => {
      this.posts.update((items) =>
        items.map((p) => (p.id === post.id ? { ...p, isLikedByMe: result.liked, likeCount: result.likeCount } : p))
      );
    });
  }

  deletePost(event: Event, id: string): void {
    event.stopPropagation();
    event.preventDefault();
    if (!confirm('Hapus diskusi ini?')) return;

    this.forumService.remove(id).subscribe(() => {
      this.posts.update((items) => items.filter((p) => p.id !== id));
    });
  }

  avatarColor(name: string): string {
    const sum = name.split('').reduce((acc, ch) => acc + ch.charCodeAt(0), 0);
    return this.avatarColors[sum % this.avatarColors.length];
  }

  formatDate(date: string): string {
    return new Intl.DateTimeFormat('id-ID', { dateStyle: 'medium', timeStyle: 'short' }).format(new Date(date));
  }

  formatPrice(price: number): string {
    return new Intl.NumberFormat('id-ID', { style: 'currency', currency: 'IDR', maximumFractionDigits: 0 }).format(price);
  }

  private extractError(err: unknown, fallback: string): string {
    const errorBody = (err as { error?: unknown })?.error;
    return typeof errorBody === 'string' ? errorBody : fallback;
  }
}
