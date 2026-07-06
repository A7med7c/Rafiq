import { Injectable } from '@angular/core';
import { Account } from '../Modles/account';
import { AuthTokens } from '../Modles/auth-response';

@Injectable({
  providedIn: 'root'
})
export class TokenStorageService {

  private readonly accessTokenKey = 'accessToken';
  private readonly refreshTokenKey = 'refreshToken';
  private readonly userKey = 'currentUser';

  setTokens(tokens: AuthTokens): void {
    localStorage.setItem(this.accessTokenKey, tokens.accessToken);
    localStorage.setItem(this.refreshTokenKey, tokens.refreshToken);
    localStorage.setItem('accessTokenExpiresAt', tokens.accessTokenExpiresAt);
    localStorage.setItem('refreshTokenExpiresAt', tokens.refreshTokenExpiresAt);
  }

  setUser(user: Account): void {
    localStorage.setItem(this.userKey, JSON.stringify(user));
  }

  getUser(): Account | null {
    const raw = localStorage.getItem(this.userKey);
    if (!raw) {
      return null;
    }

    try {
      return JSON.parse(raw) as Account;
    } catch {
      return null;
    }
  }

  getAccessToken(): string | null {
    return localStorage.getItem(this.accessTokenKey);
  }

  getRefreshToken(): string | null {
    return localStorage.getItem(this.refreshTokenKey);
  }

  clear(): void {
    localStorage.removeItem(this.accessTokenKey);
    localStorage.removeItem(this.refreshTokenKey);
    localStorage.removeItem('accessTokenExpiresAt');
    localStorage.removeItem('refreshTokenExpiresAt');
    localStorage.removeItem(this.userKey);
  }

  isLoggedIn(): boolean {
    return this.getAccessToken() !== null;
  }
}
