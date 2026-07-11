import { Injectable } from '@angular/core';
import { Account } from '../Modles/account';
import { AuthResponse } from '../Modles/auth-response';

@Injectable({
  providedIn: 'root'
})
export class TokenStorageService {

  private readonly accessTokenKey = 'accessToken';
  private readonly refreshTokenKey = 'refreshToken';
  private readonly userKey = 'currentUser';
  private readonly onboardingPrefix = 'onboardingCompleted_';

  setTokens(tokens: AuthResponse['data']): void {
    localStorage.setItem(this.accessTokenKey, tokens.accessToken);
    localStorage.setItem(this.refreshTokenKey, tokens.refreshToken);
    localStorage.setItem('hasEmergencyContacts', String(tokens.hasEmergencyContacts));
  }

  getAccessToken(): string | null {
    return localStorage.getItem(this.accessTokenKey);
  }

  getRefreshToken(): string | null {
    return localStorage.getItem(this.refreshTokenKey);
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

  clear(): void {
    localStorage.removeItem(this.accessTokenKey);
    localStorage.removeItem(this.refreshTokenKey);
    localStorage.removeItem(this.userKey);
    localStorage.removeItem('hasEmergencyContacts');
  }

  isLoggedIn(): boolean {
    return this.getAccessToken() !== null;
  }

  isOnboardingCompleted(): boolean {
    const user = this.getUser();
    if (!user) return false;
    const hasEmergency = localStorage.getItem('hasEmergencyContacts') === 'true';
    return localStorage.getItem(this.onboardingPrefix + user.userId) === 'true' && hasEmergency;
  }

  markOnboardingCompleted(): void {
    const user = this.getUser();
    if (user) {
      localStorage.setItem(this.onboardingPrefix + user.userId, 'true');
    }
  }

  markEmergencyCompleted(): void {
    localStorage.setItem('hasEmergencyContacts', 'true');
  }
}