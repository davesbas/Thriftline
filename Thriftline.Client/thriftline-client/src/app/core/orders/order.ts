import { Injectable, inject } from '@angular/core';
import { HttpClient } from '@angular/common/http';
import { Observable } from 'rxjs';

export interface OrderItemResponse {
  orderItemId: string;
  productId: string;
  productName: string;
  productImageUrl: string | null;
  quantity: number;
  unitPrice: number;
  subtotal: number;
  isReviewed: boolean;
}

export interface OrderSummaryResponse {
  id: string;
  orderNumber: string;
  status: string;
  totalAmount: number;
  itemCount: number;
  createdAt: string;
  items: OrderItemResponse[];
}

export interface OrderDetailResponse {
  id: string;
  orderNumber: string;
  status: string;
  totalAmount: number;
  shippingAddress: string;
  shippingCourier: string;
  shippingCost: number;
  note: string | null;
  createdAt: string;
  items: OrderItemResponse[];
}

export interface StoreShippingOption {
  storeId: string;
  shippingCourier: string;
  note?: string;
}

export interface CheckoutRequest {
  shippingAddress: string;
  cartItemIds: string[];
  storeOptions: StoreShippingOption[];
}

export interface StoreOrderSummaryResponse {
  id: string;
  orderNumber: string;
  status: string;
  totalAmount: number;
  shippingAddress: string;
  shippingCourier: string;
  note: string | null;
  buyerName: string;
  createdAt: string;
  items: OrderItemResponse[];
}

export interface CourierOption {
  name: string;
  cost: number;
}

@Injectable({ providedIn: 'root' })
export class OrderService {
  private http = inject(HttpClient);

  checkout(request: CheckoutRequest): Observable<OrderDetailResponse[]> {
    return this.http.post<OrderDetailResponse[]>('/api/order/checkout', request);
  }

  getById(id: string): Observable<OrderDetailResponse> {
    return this.http.get<OrderDetailResponse>(`/api/order/${id}`);
  }

  getMyOrders(): Observable<OrderSummaryResponse[]> {
    return this.http.get<OrderSummaryResponse[]>('/api/order');
  }

  getStoreOrders(): Observable<StoreOrderSummaryResponse[]> {
    return this.http.get<StoreOrderSummaryResponse[]>('/api/order/store');
  }

  updateStatus(orderId: string, status: string): Observable<void> {
    return this.http.put<void>(`/api/order/${orderId}/status`, { status });
  }

  getCouriers(): Observable<CourierOption[]> {
    return this.http.get<CourierOption[]>('/api/order/couriers');
  }

  cancel(orderId: string): Observable<void> {
    return this.http.put<void>(`/api/order/${orderId}/cancel`, {});
  }
}
