import { Injectable, inject } from '@angular/core';
import { HttpClient } from '@angular/common/http';
import { Observable } from 'rxjs';
import { ProductSummaryResponse } from '../products/product';

export interface ToggleWishlistResponse {
  wishlisted: boolean;
}

@Injectable({ providedIn: 'root' })
export class WishlistService {
  private http = inject(HttpClient);

  getMyWishlist(): Observable<ProductSummaryResponse[]> {
    return this.http.get<ProductSummaryResponse[]>('/api/wishlist');
  }

  toggle(productId: string): Observable<ToggleWishlistResponse> {
    return this.http.post<ToggleWishlistResponse>(`/api/wishlist/${productId}`, {});
  }
}
