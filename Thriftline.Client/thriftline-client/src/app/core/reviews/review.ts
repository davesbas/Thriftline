import { Injectable, inject } from '@angular/core';
import { HttpClient } from '@angular/common/http';
import { Observable } from 'rxjs';

export interface ReviewResponse {
  id: string;
  productId: string;
  productName: string;
  productImageUrl: string | null;
  authorId: string;
  authorName: string;
  rating: number;
  comment: string | null;
  createdAt: string;
}

export interface ReviewableItemResponse {
  orderItemId: string;
  productId: string;
  productName: string;
  productImageUrl: string | null;
  isReviewed: boolean;
}

export interface CreateReviewRequest {
  orderItemId: string;
  rating: number;
  comment?: string;
}

@Injectable({ providedIn: 'root' })
export class ReviewService {
  private http = inject(HttpClient);

  getReviewable(): Observable<ReviewableItemResponse[]> {
    return this.http.get<ReviewableItemResponse[]>('/api/review/reviewable');
  }

  create(request: CreateReviewRequest): Observable<ReviewResponse> {
    return this.http.post<ReviewResponse>('/api/review', request);
  }

  getByProduct(productId: string): Observable<ReviewResponse[]> {
    return this.http.get<ReviewResponse[]>(`/api/review/product/${productId}`);
  }

  getByStore(storeId: string): Observable<ReviewResponse[]> {
    return this.http.get<ReviewResponse[]>(`/api/review/store/${storeId}`);
  }

  getMineByOrderItem(orderItemId: string): Observable<ReviewResponse> {
    return this.http.get<ReviewResponse>(`/api/review/mine/${orderItemId}`);
  }
}
