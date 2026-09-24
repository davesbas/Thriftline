import { Component, OnInit, inject, signal } from '@angular/core';
import { Router, RouterLink } from '@angular/router';
import { MatIconModule } from '@angular/material/icon';
import { MatButtonModule } from '@angular/material/button';
import { MatSnackBar } from '@angular/material/snack-bar';
import { ReviewService, ReviewableItemResponse } from '../../core/reviews/review';
import { AppHeader } from '../../shared/app-header/app-header';

@Component({
  selector: 'app-write-review',
  standalone: true,
  imports: [RouterLink, MatIconModule, MatButtonModule, AppHeader],
  templateUrl: './write-review.html',
  styleUrl: './write-review.scss'
})
export class WriteReview implements OnInit {
  private reviewService = inject(ReviewService);
  private snackBar = inject(MatSnackBar);
  private router = inject(Router);

  items = signal<ReviewableItemResponse[]>([]);
  isLoading = signal(true);
  ratings = new Map<string, number>();
  comments = new Map<string, string>();
  submittingIds = signal<Set<string>>(new Set());

  readonly stars = [1, 2, 3, 4, 5];

  ngOnInit(): void {
    this.load();
  }

  load(): void {
    this.isLoading.set(true);
    this.reviewService.getReviewable().subscribe({
      next: (data) => {
        this.items.set(data.filter((i) => !i.isReviewed));
        this.isLoading.set(false);
      },
      error: () => this.isLoading.set(false)
    });
  }

  setRating(orderItemId: string, rating: number): void {
    this.ratings.set(orderItemId, rating);
  }

  getRating(orderItemId: string): number {
    return this.ratings.get(orderItemId) ?? 0;
  }

  setComment(orderItemId: string, value: string): void {
    this.comments.set(orderItemId, value);
  }

  isSubmitting(orderItemId: string): boolean {
    return this.submittingIds().has(orderItemId);
  }

  submit(item: ReviewableItemResponse): void {
    const rating = this.getRating(item.orderItemId);
    if (rating < 1) {
      this.snackBar.open('Pilih rating bintang dulu.', 'Tutup', { duration: 3000 });
      return;
    }
    if (this.isSubmitting(item.orderItemId)) return;

    this.submittingIds.update((set) => new Set(set).add(item.orderItemId));

    this.reviewService
      .create({
        orderItemId: item.orderItemId,
        rating,
        comment: this.comments.get(item.orderItemId) || undefined
      })
      .subscribe({
        next: () => {
          this.snackBar.open('Ulasan terkirim. Terima kasih!', 'Tutup', { duration: 3000 });
          this.router.navigate(['/reviews', item.orderItemId]);
        },
        error: (err) => {
          const message = typeof err.error === 'string' ? err.error : 'Gagal mengirim ulasan.';
          this.snackBar.open(message, 'Tutup', { duration: 3000 });
          this.submittingIds.update((set) => {
            const next = new Set(set);
            next.delete(item.orderItemId);
            return next;
          });
        }
      });
  }
}
