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

  // Always attach this — every request through the ngrok free-tier tunnel
  // (including public auth endpoints) can be intercepted by ngrok's
  // browser-warning interstitial page, which returns 200 + HTML with no
  // CORS headers instead of reaching our backend at all. This header tells
  // ngrok to skip that page and forward the request straight through.
  let authReq = req.clone({
    setHeaders: {
      'ngrok-skip-browser-warning': 'true'
    }
  });

  if (shouldAttachToken(req.url)) {
    const token = tokenStorage.getAccessToken();

    if (token) {
      authReq = authReq.clone({
        setHeaders: {
          Authorization: `Bearer ${token}`
        }
      });

      console.debug(`[Auth] Interceptor executing for → ${req.method} ${req.url}`);
      console.debug(`[Auth] Authorization header present: YES`);
      console.debug(`[Auth] Token length: ${token.length}`);
      console.debug(`[Auth] Token first 20 chars: ${token.substring(0, 20)}`);
      console.debug(`[Auth] Token last 20 chars: ${token.substring(token.length - 20)}`);

    } else {
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
                  Authorization: `Bearer ${newToken}`,
                  'ngrok-skip-browser-warning': 'true'
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