import { Injectable, inject } from '@angular/core';
import { HttpClient } from '@angular/common/http';
import { Observable } from 'rxjs';

export interface CategoryResponse {
  id: string;
  parentCategoryId: string | null;
  name: string;
  description: string | null;
  iconUrl: string | null;
  subCategories: CategoryResponse[];
}

export interface ProductMediaItem {
  url: string;
  mediaType: number; // 0 = Image, 1 = Video
}

export interface ProductSummaryResponse {
  id: string;
  name: string;
  price: number;
  condition: number;
  status: number;
  primaryImageUrl: string | null;
  storeName: string;
  categoryName: string;
  isWishlisted: boolean;
}

export interface PagedResult<T> {
  items: T[];
  totalCount: number;
  page: number;
  pageSize: number;
}

export interface ProductQuery {
  search?: string;
  categoryId?: string;
  storeId?: string;
  page?: number;
  pageSize?: number;
}

export interface ProductDetailResponse {
  id: string;
  name: string;
  description: string | null;
  price: number;
  condition: number;
  stock: number;
  status: number;
  categoryId: string;
  categoryName: string;
  storeId: string;
  storeName: string;
  media: ProductMediaItem[];
  isWishlisted: boolean;
  createdAt: string;
}

export interface CreateProductRequest {
  categoryId: string;
  name: string;
  description?: string;
  price: number;
  condition: number;
  stock: number;
  media?: ProductMediaItem[];
}

export interface UpdateProductRequest {
  categoryId: string;
  name: string;
  description?: string;
  price: number;
  condition: number;
  stock: number;
  status: number;
  media?: ProductMediaItem[];
}

@Injectable({ providedIn: 'root' })
export class ProductService {
  private http = inject(HttpClient);

  getCategories(): Observable<CategoryResponse[]> {
    return this.http.get<CategoryResponse[]>('/api/category');
  }

  getProducts(query: ProductQuery): Observable<PagedResult<ProductSummaryResponse>> {
    const params: Record<string, string> = {};
    if (query.search) params['search'] = query.search;
    if (query.categoryId) params['categoryId'] = query.categoryId;
    if (query.storeId) params['storeId'] = query.storeId;
    params['page'] = String(query.page ?? 1);
    params['pageSize'] = String(query.pageSize ?? 20);

    return this.http.get<PagedResult<ProductSummaryResponse>>('/api/product', { params });
  }

  getById(id: string): Observable<ProductDetailResponse> {
    return this.http.get<ProductDetailResponse>(`/api/product/${id}`);
  }

  getMyProducts(): Observable<ProductDetailResponse[]> {
    return this.http.get<ProductDetailResponse[]>('/api/product/mine');
  }

  create(request: CreateProductRequest): Observable<ProductDetailResponse> {
    return this.http.post<ProductDetailResponse>('/api/product', request);
  }

  update(id: string, request: UpdateProductRequest): Observable<void> {
    return this.http.put<void>(`/api/product/${id}`, request);
  }

  remove(id: string): Observable<void> {
    return this.http.delete<void>(`/api/product/${id}`);
  }
}
