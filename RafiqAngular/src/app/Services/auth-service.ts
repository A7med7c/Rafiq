import { HttpClient } from '@angular/common/http';
import { Injectable, inject, isDevMode } from '@angular/core';
import { Router } from '@angular/router';
import {
  BehaviorSubject, Observable, Subject, catchError, finalize, firstValueFrom,
  map, of, shareReplay, tap, throwError
} from 'rxjs';
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

  /**
   * The single in-flight refresh, shared by every caller.
   *
   * The server rotates refresh tokens and treats a second use of the same token as
   * theft: it revokes the whole family, including the token the winning refresh just
   * issued. So concurrent refreshes must never happen — all callers wait on this one.
   */
  private refreshInFlight$: Observable<AuthResponse> | null = null;

  /** Emits after the access token has been replaced, so dependants (SignalR) can reconnect. */
  private readonly tokensRefreshedSubject = new Subject<void>();
  readonly tokensRefreshed$ = this.tokensRefreshedSubject.asObservable();

  get isLoggedIn(): boolean {
    return this.tokenStorage.isLoggedIn();
  }

  get currentUser(): Account | null {
    return this.currentUserSubject.value;
  }

  navigateToAppEntry(): void {
    void this.router.navigate([this.isLoggedIn ? '/dashboard' : '/login']);
  }

  /** Resolves the current user's avatar to an absolute URL, falling back to the default avatar. */
  get avatarUrl(): string {
    return this.resolveAvatarUrl(this.currentUser?.profileImageUrl);
  }

  resolveAvatarUrl(profileImageUrl: string | null | undefined): string {
    if (!profileImageUrl) {
      return 'images/ahmed_avatar.png';
    }

    return `${environment.fileBaseUrl}${profileImageUrl}`;
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

  register(request: RegisterRequest, profileImage?: File | null): Observable<ApiResponse<RegisterResponse>> {
    const formData = new FormData();
    formData.append('firstName', request.firstName);
    formData.append('lastName', request.lastName);
    formData.append('email', request.email);
    formData.append('phoneNumber', request.phoneNumber);
    formData.append('password', request.password);
    formData.append('confirmPassword', request.confirmPassword);

    if (profileImage) {
      formData.append('profileImage', profileImage, profileImage.name);
    }

    return this.http.post<ApiResponse<RegisterResponse>>(
      `${environment.apiUrl}/auth/register`,
      formData
    );
  }

  verifyAccount(email: string, code: string): Observable<ApiResponseBase> {
    return this.http.post<ApiResponseBase>(
      `${environment.apiUrl}/auth/verify-phone`,
      {
        email,
        code,
        purpose: 'EmailVerification'
      }
    );
  }

  resendOtp(email: string): Observable<ApiResponseBase> {
    return this.http.post<ApiResponseBase>(
      `${environment.apiUrl}/auth/resend-phone-code`,
      {
        email,
        purpose: 'EmailVerification'
      }
    );
  }

  forgotPassword(email: string): Observable<ApiResponseBase> {
    return this.http.post<ApiResponseBase>(
      `${environment.apiUrl}/auth/forget-password`,
      { email }
    );
  }

  verifyResetOtp(email: string, code: string): Observable<ApiResponse<{ resetToken: string }>> {
    return this.http.post<ApiResponse<{ resetToken: string }>>(
      `${environment.apiUrl}/auth/verify-reset-otp`,
      { email, code }
    );
  }

  resetPassword(resetToken: string, newPassword: string): Observable<ApiResponseBase> {
    return this.http.post<ApiResponseBase>(
      `${environment.apiUrl}/auth/reset-password`,
      { resetToken, newPassword }
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

  /**
   * Refreshes the access token. Concurrent callers share one HTTP request — firing two
   * would trip the server's refresh-token reuse detection and kill the whole session.
   */
  refreshToken(): Observable<AuthResponse> {
    if (this.refreshInFlight$) {
      this.log('Refresh already in flight — joining it.');
      return this.refreshInFlight$;
    }

    const refreshToken = this.tokenStorage.getRefreshToken();

    if (!refreshToken) {
      return throwError(() => new Error('No refresh token available.'));
    }

    this.log('Refreshing access token.');

    this.refreshInFlight$ = this.http.post<AuthResponse>(
      `${environment.apiUrl}/auth/refresh-token`,
      { refreshToken }
    ).pipe(
      tap((response) => {
        this.handleAuthSuccess(response, false);
        this.log('Access token refreshed.');
        this.tokensRefreshedSubject.next();
      }),
      // Release the slot once this attempt settles, win or lose, so a later 401 can retry.
      finalize(() => { this.refreshInFlight$ = null; }),
      shareReplay({ bufferSize: 1, refCount: false })
    );

    return this.refreshInFlight$;
  }

  /** True when the stored access token is absent or its `exp` has passed. */
  isAccessTokenExpired(skewSeconds = 30): boolean {
    const token = this.tokenStorage.getAccessToken();
    if (!token) return true;

    const exp = this.readExpiry(token);
    if (exp === null) return false; // Unreadable: let the server be the judge.

    return Date.now() >= (exp * 1000) - skewSeconds * 1000;
  }

  /**
   * Returns an access token that is valid right now, refreshing first if the stored one
   * has expired. SignalR uses this so it never negotiates with a dead token.
   */
  async getValidAccessToken(): Promise<string> {
    if (!this.tokenStorage.getRefreshToken() && !this.tokenStorage.getAccessToken()) {
      return '';
    }

    if (this.isAccessTokenExpired()) {
      try {
        await firstValueFrom(this.refreshToken());
      } catch {
        return '';
      }
    }

    return this.tokenStorage.getAccessToken() ?? '';
  }

  /** Reads the `exp` claim without pulling in a JWT library. */
  private readExpiry(token: string): number | null {
    try {
      const payload = token.split('.')[1];
      if (!payload) return null;

      const json = atob(payload.replace(/-/g, '+').replace(/_/g, '/'));
      const exp = JSON.parse(json)?.exp;

      return typeof exp === 'number' ? exp : null;
    } catch {
      return null;
    }
  }

  private log(message: string): void {
    if (isDevMode()) {
      console.debug(`[Auth] ${message}`);
    }
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
