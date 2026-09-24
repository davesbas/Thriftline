import { Injectable, inject } from '@angular/core';
import { HttpClient } from '@angular/common/http';
import { Observable } from 'rxjs';

export interface CartItemResponse {
  id: string;
  productId: string;
  productName: string;
  productPrice: number;
  productImageUrl: string | null;
  storeId: string;
  storeName: string;
  isAvailable: boolean;
  quantity: number;
  subtotal: number;
}

export interface CartSummaryResponse {
  items: CartItemResponse[];
  totalItems: number;
  totalPrice: number;
}

export interface AddToCartRequest {
  productId: string;
  quantity: number;
}

@Injectable({ providedIn: 'root' })
export class CartService {
  private http = inject(HttpClient);

  getSummary(): Observable<CartSummaryResponse> {
    return this.http.get<CartSummaryResponse>('/api/cart');
  }

  addToCart(request: AddToCartRequest): Observable<CartSummaryResponse> {
    return this.http.post<CartSummaryResponse>('/api/cart', request);
  }

  updateQuantity(id: string, quantity: number): Observable<CartSummaryResponse> {
    return this.http.put<CartSummaryResponse>(`/api/cart/${id}`, { quantity });
  }

  removeItem(id: string): Observable<void> {
    return this.http.delete<void>(`/api/cart/${id}`);
  }
}
