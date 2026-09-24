import { Injectable, inject } from '@angular/core';
import { HttpClient } from '@angular/common/http';
import { Observable } from 'rxjs';

export const MEDIA_TYPE_IMAGE = 0;
export const MEDIA_TYPE_VIDEO = 1;

export interface UploadMediaResponse {
  url: string;
  mediaType: number;
}

@Injectable({ providedIn: 'root' })
export class UploadService {
  private http = inject(HttpClient);

  uploadMedia(file: File): Observable<UploadMediaResponse> {
    const formData = new FormData();
    formData.append('file', file);
    return this.http.post<UploadMediaResponse>('/api/upload/image', formData);
  }
}
