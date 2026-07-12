import { HttpClient } from '@angular/common/http';
import { Injectable, inject } from '@angular/core';
import { Router } from '@angular/router';
import { BehaviorSubject, Observable, catchError, finalize, map, of, tap, throwError } from 'rxjs';
import { Account } from '../Modles/account';
import { ApiResponse, ApiResponseBase } from '../Modles/api-response';
import { AuthResponse } from '../Modles/auth-response';
import { LoginRequest } from '../Modles/login-request';
import { RegisterRequest } from '../Modles/register-request';
import { RegisterResponse } from '../Modles/register-response';
import { environment } from '../Environments/Environment';
import { TokenStorageService } from './token-storage-service';
import { ProfileSelectionService } from './profile-selection.service';

@Injectable({
  providedIn: 'root'
})
export class AuthService {

  private readonly http = inject(HttpClient);
  private readonly tokenStorage = inject(TokenStorageService);
  private readonly router = inject(Router);
  private readonly profileSelectionSvc = inject(ProfileSelectionService);

  private readonly currentUserSubject = new BehaviorSubject<Account | null>(
    this.tokenStorage.getUser()
  );

  readonly currentUser$ = this.currentUserSubject.asObservable();
  private sessionInitialized = false;

  get isLoggedIn(): boolean {
    return this.tokenStorage.isLoggedIn();
  }

  get currentUser(): Account | null {
    return this.currentUserSubject.value;
  }

  initializeSession(): Observable<Account | null> {
    if (this.sessionInitialized) {
      return of(this.currentUserSubject.value);
    }

    this.sessionInitialized = true;

    if (!this.tokenStorage.isLoggedIn()) {
      return of(null);
    }

    return this.getMe().pipe(
      catchError(() => {
        this.clearLocalSession();
        return of(null);
      })
    );
  }

  login(request: LoginRequest): Observable<AuthResponse> {
    return this.http.post<AuthResponse>(
      `${environment.apiUrl}/auth/login`,
      request
    ).pipe(
      tap((response) => this.handleAuthSuccess(response))
    );
  }

  register(request: RegisterRequest): Observable<ApiResponse<RegisterResponse>> {
    return this.http.post<ApiResponse<RegisterResponse>>(
      `${environment.apiUrl}/auth/register`,
      request
    );
  }

  verifyPhone(phoneNumber: string, code: string): Observable<ApiResponseBase> {
    return this.http.post<ApiResponseBase>(
      `${environment.apiUrl}/auth/verify-phone`,
      {
        phoneNumber,
        code,
        purpose: 'PhoneVerification'
      }
    );
  }

  resendPhoneCode(phoneNumber: string): Observable<ApiResponseBase> {
    return this.http.post<ApiResponseBase>(
      `${environment.apiUrl}/auth/resend-phone-code`,
      {
        phoneNumber,
        purpose: 'PhoneVerification'
      }
    );
  }

  loginWithGoogle(idToken: string): Observable<AuthResponse> {
    return this.http.post<AuthResponse>(
      `${environment.apiUrl}/auth/google`,
      { idToken }
    ).pipe(
      tap((response) => this.handleAuthSuccess(response))
    );
  }

  refreshToken(): Observable<AuthResponse> {
    const refreshToken = this.tokenStorage.getRefreshToken();

    if (!refreshToken) {
      return throwError(() => new Error('No refresh token available.'));
    }

    return this.http.post<AuthResponse>(
      `${environment.apiUrl}/auth/refresh-token`,
      { refreshToken }
    ).pipe(
      tap((response) => this.handleAuthSuccess(response, false))
    );
  }

  getMe(): Observable<Account> {
    return this.http.get<ApiResponse<Account>>(
      `${environment.apiUrl}/auth/me`
    ).pipe(
      map((response) => response.data),
      tap((user) => {
        this.tokenStorage.setUser(user);
        this.currentUserSubject.next(user);
      })
    );
  }

  logout(): Observable<void> {
    const refreshToken = this.tokenStorage.getRefreshToken();

    const request$ = refreshToken
      ? this.http.post<ApiResponseBase>(
          `${environment.apiUrl}/auth/logout`,
          { refreshToken, ipAddress: null }
        ).pipe(map(() => undefined))
      : of(undefined);

    return request$.pipe(
      catchError(() => of(undefined)),
      finalize(() => {
        this.clearLocalSession();
        this.router.navigate(['/login']);
      })
    );
  }

  private handleAuthSuccess(response: AuthResponse, loadProfile = true): void {
    this.tokenStorage.setTokens(response.data);

    if (loadProfile) {
      this.getMe().subscribe({
        error: () => this.clearLocalSession()
      });
    }
  }

  private clearLocalSession(): void {
    this.sessionInitialized = false;
    this.tokenStorage.clear();
    this.profileSelectionSvc.clear();
    this.currentUserSubject.next(null);
  }
}
