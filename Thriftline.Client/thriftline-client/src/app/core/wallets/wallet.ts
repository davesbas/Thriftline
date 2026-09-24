import { Injectable, inject } from '@angular/core';
import { HttpClient } from '@angular/common/http';
import { Observable } from 'rxjs';

export interface WalletResponse {
  balance: number;
}

export interface WalletTransactionResponse {
  id: string;
  type: string;
  amount: number;
  balanceAfter: number;
  description: string;
  createdAt: string;
}

@Injectable({ providedIn: 'root' })
export class WalletService {
  private http = inject(HttpClient);

  getMyWallet(): Observable<WalletResponse> {
    return this.http.get<WalletResponse>('/api/wallet');
  }

  getTransactions(): Observable<WalletTransactionResponse[]> {
    return this.http.get<WalletTransactionResponse[]>('/api/wallet/transactions');
  }

  topUp(amount: number): Observable<WalletResponse> {
    return this.http.post<WalletResponse>('/api/wallet/topup', { amount });
  }
}
