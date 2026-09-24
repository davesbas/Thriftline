import { Injectable, inject } from '@angular/core';
import { HttpClient } from '@angular/common/http';
import { Observable } from 'rxjs';
import { PagedResult } from '../products/product';

export interface ForumMediaItem {
  url: string;
  mediaType: number;
}

export interface ForumPostSummaryResponse {
  id: string;
  title: string;
  authorId: string;
  authorName: string;
  authorAvatarUrl: string | null;
  postedAsStoreId: string | null;
  commentCount: number;
  likeCount: number;
  isLikedByMe: boolean;
  thumbnailUrl: string | null;
  productId: string | null;
  productName: string | null;
  productPrice: number | null;
  productImageUrl: string | null;
  createdAt: string;
}

export interface ForumCommentResponse {
  id: string;
  authorId: string;
  authorName: string;
  authorAvatarUrl: string | null;
  postedAsStoreId: string | null;
  content: string;
  productId: string | null;
  productName: string | null;
  productPrice: number | null;
  productImageUrl: string | null;
  createdAt: string;
}

export interface ForumPostDetailResponse {
  id: string;
  title: string;
  content: string;
  authorId: string;
  authorName: string;
  authorAvatarUrl: string | null;
  postedAsStoreId: string | null;
  createdAt: string;
  updatedAt: string | null;
  media: ForumMediaItem[];
  productId: string | null;
  productName: string | null;
  productPrice: number | null;
  productImageUrl: string | null;
  likeCount: number;
  isLikedByMe: boolean;
  comments: ForumCommentResponse[];
}

export interface CreateForumPostRequest {
  title: string;
  content: string;
  media?: ForumMediaItem[];
  productId?: string | null;
  storeId?: string | null;
}

export interface ToggleLikeResponse {
  liked: boolean;
  likeCount: number;
}

@Injectable({ providedIn: 'root' })
export class ForumService {
  private http = inject(HttpClient);

  getAll(page = 1, pageSize = 20): Observable<PagedResult<ForumPostSummaryResponse>> {
    return this.http.get<PagedResult<ForumPostSummaryResponse>>('/api/forum', {
      params: { page: String(page), pageSize: String(pageSize) }
    });
  }

  getMine(): Observable<ForumPostSummaryResponse[]> {
    return this.http.get<ForumPostSummaryResponse[]>('/api/forum/mine');
  }

  getById(id: string): Observable<ForumPostDetailResponse> {
    return this.http.get<ForumPostDetailResponse>(`/api/forum/${id}`);
  }

  create(request: CreateForumPostRequest): Observable<ForumPostDetailResponse> {
    return this.http.post<ForumPostDetailResponse>('/api/forum', request);
  }

  remove(id: string): Observable<void> {
    return this.http.delete<void>(`/api/forum/${id}`);
  }

  toggleLike(id: string): Observable<ToggleLikeResponse> {
    return this.http.post<ToggleLikeResponse>(`/api/forum/${id}/like`, {});
  }

  addComment(postId: string, content: string, productId?: string | null, storeId?: string | null): Observable<ForumCommentResponse> {
    return this.http.post<ForumCommentResponse>(`/api/forum/${postId}/comments`, { content, productId, storeId });
  }

  removeComment(commentId: string): Observable<void> {
    return this.http.delete<void>(`/api/forum/comments/${commentId}`);
  }
}
