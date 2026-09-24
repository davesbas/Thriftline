import { Injectable, inject } from '@angular/core';
import { HttpClient } from '@angular/common/http';
import { Observable } from 'rxjs';
import { PagedResult } from '../products/product';

export interface UnreadCountResponse {
  unreadCount: number;
}

export interface NotificationResponse {
  id: string;
  type: string;
  title: string;
  message: string;
  isRead: boolean;
  relatedEntityType: string | null;
  relatedEntityId: string | null;
  createdAt: string;
}

@Injectable({ providedIn: 'root' })
export class NotificationService {
  private http = inject(HttpClient);

  getUnreadCount(): Observable<UnreadCountResponse> {
    return this.http.get<UnreadCountResponse>('/api/notification/unread-count');
  }

  getAll(page = 1, pageSize = 20): Observable<PagedResult<NotificationResponse>> {
    return this.http.get<PagedResult<NotificationResponse>>('/api/notification', {
      params: { page: String(page), pageSize: String(pageSize) }
    });
  }

  markAsRead(id: string): Observable<void> {
    return this.http.put<void>(`/api/notification/${id}/read`, {});
  }

  markAllAsRead(): Observable<void> {
    return this.http.put<void>('/api/notification/read-all', {});
  }
}
