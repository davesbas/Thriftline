import { Injectable, signal, computed, inject } from '@angular/core';
import { HttpClient } from '@angular/common/http';
import { Observable, tap } from 'rxjs';
import { AUTH_TOKEN_KEY, AUTH_USER_KEY } from './auth.constants';

export interface AuthUser {
  userId: string;
  fullName: string;
  email: string;
}

interface AuthResponse {
  token: string;
  userId: string;
  fullName: string;
  email: string;
  expiresAt: string;
}

export interface RegisterRequest {
  fullName: string;
  email: string;
  password: string;
  phoneNumber: string;
}

export interface LoginRequest {
  email: string;
  password: string;
}

@Injectable({ providedIn: 'root' })
export class AuthService {
  private http = inject(HttpClient);

  currentUser = signal<AuthUser | null>(this.loadStoredUser());
  isLoggedIn = computed(() => this.currentUser() !== null);

  login(request: LoginRequest): Observable<AuthResponse> {
    return this.http.post<AuthResponse>('/api/auth/login', request).pipe(
      tap((response) => this.storeSession(response))
    );
  }

  register(request: RegisterRequest): Observable<AuthResponse> {
    return this.http.post<AuthResponse>('/api/auth/register', request).pipe(
      tap((response) => this.storeSession(response))
    );
  }

  logout(): void {
    localStorage.removeItem(AUTH_TOKEN_KEY);
    localStorage.removeItem(AUTH_USER_KEY);
    this.currentUser.set(null);
  }

  updateStoredFullName(fullName: string): void {
    const current = this.currentUser();
    if (!current) return;

    const updated = { ...current, fullName };
    localStorage.setItem(AUTH_USER_KEY, JSON.stringify(updated));
    this.currentUser.set(updated);
  }

  getToken(): string | null {
    return localStorage.getItem(AUTH_TOKEN_KEY);
  }

  private storeSession(response: AuthResponse): void {
    const user: AuthUser = {
      userId: response.userId,
      fullName: response.fullName,
      email: response.email
    };

    localStorage.setItem(AUTH_TOKEN_KEY, response.token);
    localStorage.setItem(AUTH_USER_KEY, JSON.stringify(user));
    this.currentUser.set(user);
  }

  private loadStoredUser(): AuthUser | null {
    const raw = localStorage.getItem(AUTH_USER_KEY);
    if (!raw) {
      return null;
    }

    try {
      return JSON.parse(raw) as AuthUser;
    } catch {
      return null;
    }
  }
}
