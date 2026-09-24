import { inject } from '@angular/core';
import { HttpInterceptorFn } from '@angular/common/http';
import { Router } from '@angular/router';
import { catchError, throwError } from 'rxjs';
import { AUTH_TOKEN_KEY, AUTH_USER_KEY } from './auth.constants';

export const authInterceptor: HttpInterceptorFn = (req, next) => {
  const router = inject(Router);
  const token = localStorage.getItem(AUTH_TOKEN_KEY);

  const cloned = token
    ? req.clone({ setHeaders: { Authorization: `Bearer ${token}` } })
    : req;

  return next(cloned).pipe(
    catchError((error) => {
      if (error?.status === 401 && token) {
        localStorage.removeItem(AUTH_TOKEN_KEY);
        localStorage.removeItem(AUTH_USER_KEY);
        router.navigate(['/login'], { queryParams: { sessionExpired: '1' } });
      }
      return throwError(() => error);
    })
  );
};
