import { Injectable, inject } from '@angular/core';
import { HttpClient } from '@angular/common/http';
import { Observable, catchError, of } from 'rxjs';

export interface StoreResponse {
  id: string;
  ownerId: string;
  name: string;
  description: string | null;
  logoUrl: string | null;
  address: string | null;
  averageRating: number;
  reviewCount: number;
}

export interface CreateStoreRequest {
  name: string;
  description?: string;
  logoUrl?: string;
  address?: string;
  phoneNumber?: string;
  agreedToTerms: boolean;
}

@Injectable({ providedIn: 'root' })
export class StoreService {
  private http = inject(HttpClient);

  getMyStore(): Observable<StoreResponse | null> {
    return this.http.get<StoreResponse>('/api/store/me').pipe(catchError(() => of(null)));
  }

  getById(id: string): Observable<StoreResponse> {
    return this.http.get<StoreResponse>(`/api/store/${id}`);
  }

  create(request: CreateStoreRequest): Observable<StoreResponse> {
    return this.http.post<StoreResponse>('/api/store', request);
  }
}
