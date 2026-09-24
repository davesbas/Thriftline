import { Component, OnInit, inject, signal } from '@angular/core';
import { ActivatedRoute, RouterLink } from '@angular/router';
import { MatIconModule } from '@angular/material/icon';
import { ReviewService, ReviewResponse } from '../../core/reviews/review';
import { AppHeader } from '../../shared/app-header/app-header';

@Component({
  selector: 'app-review-detail',
  standalone: true,
  imports: [RouterLink, MatIconModule, AppHeader],
  templateUrl: './review-detail.html',
  styleUrl: './review-detail.scss'
})
export class ReviewDetail implements OnInit {
  private route = inject(ActivatedRoute);
  private reviewService = inject(ReviewService);

  review = signal<ReviewResponse | null>(null);
  isLoading = signal(true);
  loadError = signal(false);

  readonly stars = [1, 2, 3, 4, 5];

  ngOnInit(): void {
    const orderItemId = this.route.snapshot.paramMap.get('orderItemId');
    if (!orderItemId) {
      this.isLoading.set(false);
      this.loadError.set(true);
      return;
    }

    this.reviewService.getMineByOrderItem(orderItemId).subscribe({
      next: (data) => {
        this.review.set(data);
        this.isLoading.set(false);
      },
      error: () => {
        this.isLoading.set(false);
        this.loadError.set(true);
      }
    });
  }

  formatDate(date: string): string {
    return new Intl.DateTimeFormat('id-ID', { dateStyle: 'long' }).format(new Date(date));
  }
}
