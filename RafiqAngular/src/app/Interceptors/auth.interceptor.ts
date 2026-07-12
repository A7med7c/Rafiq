import { HttpErrorResponse, HttpInterceptorFn } from '@angular/common/http';
import { inject, isDevMode } from '@angular/core';
import { Router } from '@angular/router';
import { catchError, switchMap, throwError } from 'rxjs';
import { AuthService } from '../Services/auth-service';
import { TokenStorageService } from '../Services/token-storage-service';
import { environment } from '../Environments/Environment';

const AUTH_URL = environment.apiUrl + '/auth/';

function isAuthEndpoint(url: string): boolean {
  return url.startsWith(AUTH_URL);
}

function shouldAttachToken(url: string): boolean {
  if (!url.startsWith(environment.apiUrl)) {
    return false;
  }

  const publicAuthPaths = [
    '/auth/login',
    '/auth/register',
    '/auth/google',
    '/auth/refresh-token',
    '/auth/forget-password',
    '/auth/verify-reset-otp',
    '/auth/reset-password',
    '/auth/verify-phone',
    '/auth/resend-phone-code'
  ];

  return !publicAuthPaths.some((path) => url.includes(path));
}

export const authInterceptor: HttpInterceptorFn = (req, next) => {
  const tokenStorage = inject(TokenStorageService);
  const authService = inject(AuthService);
  const router = inject(Router);

  let authReq = req;

  if (shouldAttachToken(req.url)) {
    const token = tokenStorage.getAccessToken();

    if (token) {
      authReq = req.clone({
        setHeaders: {
          Authorization: `Bearer ${token}`
        }
      });

      if (isDevMode()) {
        console.debug(`[Auth] Authorization attached → ${req.method} ${req.url}`);
      }
    } else if (isDevMode()) {
      console.warn(`[Auth] No access token in storage → ${req.method} ${req.url}`);
    }
  }

  return next(authReq).pipe(
    catchError((error: HttpErrorResponse) => {
      if (
        error.status === 401 &&
        shouldAttachToken(req.url) &&
        !req.url.includes('/auth/refresh-token') &&
        tokenStorage.getRefreshToken()
      ) {
        if (isDevMode()) {
          console.debug(`[Auth] 401 on ${req.url} — refreshing (shared single-flight).`);
        }

        // AuthService de-duplicates this: a burst of 401s produces exactly ONE
        // refresh call. Two calls would trip server-side reuse detection and kill
        // the whole token family.
        return authService.refreshToken().pipe(
          switchMap(() => {
            const newToken = tokenStorage.getAccessToken();

            if (!newToken) {
              authService.logout().subscribe();
              return throwError(() => error);
            }

            return next(
              req.clone({
                setHeaders: {
                  Authorization: `Bearer ${newToken}`
                }
              })
            );
          }),
          catchError((refreshError) => {
            if (isAuthEndpoint(req.url)) {
              authService.logout().subscribe();
            } else {
              router.navigate(['/login']);
              tokenStorage.clear();
            }

            return throwError(() => refreshError);
          })
        );
      }

      return throwError(() => error);
    })
  );
};
