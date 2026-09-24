import { Injectable, inject } from '@angular/core';
import { HttpClient } from '@angular/common/http';
import { Observable } from 'rxjs';

export interface PaymentResponse {
  id: string;
  orderId: string;
  orderNumber: string;
  method: string;
  status: string;
  amount: number;
  transactionReference: string | null;
  paidAt: string | null;
  createdAt: string;
}

export interface CreatePaymentRequest {
  orderId: string;
  method: number;
}

@Injectable({ providedIn: 'root' })
export class PaymentService {
  private http = inject(HttpClient);

  create(request: CreatePaymentRequest): Observable<PaymentResponse> {
    return this.http.post<PaymentResponse>('/api/payment', request);
  }

  confirm(paymentId: string): Observable<PaymentResponse> {
    return this.http.post<PaymentResponse>(`/api/payment/${paymentId}/confirm`, {});
  }

  createBatch(orderIds: string[], method: number): Observable<PaymentResponse[]> {
    return this.http.post<PaymentResponse[]>('/api/payment/batch', { orderIds, method });
  }
}
