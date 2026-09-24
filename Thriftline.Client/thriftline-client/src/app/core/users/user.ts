import { Injectable, inject } from '@angular/core';
import { HttpClient } from '@angular/common/http';
import { Observable } from 'rxjs';

export interface UserProfileResponse {
  id: string;
  fullName: string;
  email: string;
  phoneNumber: string | null;
  address: string | null;
  profilePictureUrl: string | null;
}

export interface UpdateProfileRequest {
  fullName: string;
  phoneNumber: string;
  address?: string;
  profilePictureUrl?: string;
}

@Injectable({ providedIn: 'root' })
export class UserService {
  private http = inject(HttpClient);

  getMe(): Observable<UserProfileResponse> {
    return this.http.get<UserProfileResponse>('/api/user/me');
  }

  updateMe(request: UpdateProfileRequest): Observable<UserProfileResponse> {
    return this.http.put<UserProfileResponse>('/api/user/me', request);
  }
}
