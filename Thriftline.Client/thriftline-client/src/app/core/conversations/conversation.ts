import { Injectable, inject } from '@angular/core';
import { HttpClient } from '@angular/common/http';
import { Observable } from 'rxjs';

export interface ConversationSummaryResponse {
  id: string;
  storeId: string;
  storeName: string;
  buyerId: string;
  buyerName: string;
  productId: string | null;
  productName: string | null;
  createdAt: string;
  lastMessageAt: string | null;
  lastMessagePreview: string | null;
}

export interface MessageResponse {
  id: string;
  senderId: string;
  senderName: string;
  content: string;
  isRead: boolean;
  sentAt: string;
  productId: string | null;
  productName: string | null;
  productPrice: number | null;
  productImageUrl: string | null;
}

export interface ConversationDetailResponse {
  id: string;
  storeId: string;
  storeName: string;
  buyerId: string;
  buyerName: string;
  productId: string | null;
  productName: string | null;
  createdAt: string;
  messages: MessageResponse[];
}

export interface StartConversationRequest {
  storeId: string;
  productId?: string;
}

@Injectable({ providedIn: 'root' })
export class ConversationService {
  private http = inject(HttpClient);

  getMyConversations(): Observable<ConversationSummaryResponse[]> {
    return this.http.get<ConversationSummaryResponse[]>('/api/conversation');
  }

  getById(id: string): Observable<ConversationDetailResponse> {
    return this.http.get<ConversationDetailResponse>(`/api/conversation/${id}`);
  }

  start(request: StartConversationRequest): Observable<ConversationDetailResponse> {
    return this.http.post<ConversationDetailResponse>('/api/conversation', request);
  }

  sendMessage(id: string, content: string, productId?: string): Observable<MessageResponse> {
    return this.http.post<MessageResponse>(`/api/conversation/${id}/messages`, { content, productId });
  }
}
