import { Injectable, inject } from '@angular/core';
import { HttpClient } from '@angular/common/http';
import { Observable } from 'rxjs';

export interface AddressResponse {
  id: string;
  label: string;
  recipientName: string;
  phoneNumber: string;
  fullAddress: string;
  isPrimary: boolean;
}

export interface AddressRequest {
  label: string;
  recipientName: string;
  phoneNumber: string;
  fullAddress: string;
  isPrimary: boolean;
}

@Injectable({ providedIn: 'root' })
export class AddressService {
  private http = inject(HttpClient);

  getMine(): Observable<AddressResponse[]> {
    return this.http.get<AddressResponse[]>('/api/address');
  }

  create(request: AddressRequest): Observable<AddressResponse> {
    return this.http.post<AddressResponse>('/api/address', request);
  }

  update(id: string, request: AddressRequest): Observable<AddressResponse> {
    return this.http.put<AddressResponse>(`/api/address/${id}`, request);
  }

  setPrimary(id: string): Observable<void> {
    return this.http.put<void>(`/api/address/${id}/primary`, {});
  }

  remove(id: string): Observable<void> {
    return this.http.delete<void>(`/api/address/${id}`);
  }
}
