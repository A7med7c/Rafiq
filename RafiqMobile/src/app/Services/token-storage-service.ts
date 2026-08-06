import { Injectable } from '@angular/core';
import { Capacitor } from '@capacitor/core';
import { SecureStoragePlugin } from 'capacitor-secure-storage-plugin';
import { Account } from '../Modles/account';
import { AuthResponse } from '../Modles/auth-response';

/**
 * Service managing authentication tokens, user session profile data,
 * and application flags with secure persistence across native mobile and web browser.
 *
 * Platform Storage Strategy:
 * - Native Mobile (Android / iOS): Capacitor Secure Storage (Android Keystore / iOS Keychain) via SecureStoragePlugin
 * - Web Browser: localStorage with graceful in-memory fallback
 * - Runtime Performance & Fallback: In-memory Map cache for zero-latency synchronous reads
 *
 * Reads check the in-memory cache first. If missing, data is retrieved from
 * SecureStorage (native) or localStorage (browser), cached in memory, and returned.
 * Writes update the memory cache synchronously and persist to storage asynchronously.
 * Deletes remove data from memory cache and persistent storage.
 */
@Injectable({
  providedIn: 'root'
})
export class TokenStorageService {

  private readonly accessTokenKey = 'accessToken';
  private readonly refreshTokenKey = 'refreshToken';
  private readonly userKey = 'currentUser';
  private readonly emergencyKey = 'hasEmergencyContacts';

  /** Runtime memory cache for high-performance synchronous access & session fallback */
  private readonly memory = new Map<string, string>();

  /** Single-flight initialization promise to prevent redundant hydration calls */
  private initPromise: Promise<void> | null = null;

  constructor() {
    // Hydrate memory cache asynchronously on service creation
    void this.initialize();
  }

  /**
   * Pre-hydrates the in-memory cache from persistent storage (Native Keychain/Keystore or Browser localStorage).
   * Safe to call multiple times; concurrent/subsequent calls join the initial hydration promise.
   */
  initialize(): Promise<void> {
    if (!this.initPromise) {
      this.initPromise = (async () => {
        try {
          await Promise.all([
            this.getItem(this.accessTokenKey),
            this.getItem(this.refreshTokenKey),
            this.getItem(this.userKey),
            this.getItem(this.emergencyKey)
          ]);
        } catch (e) {
          console.warn('[TokenStorage] Initialization failed during cache hydration:', e);
        }
      })();
    }
    return this.initPromise;
  }

  // ─── Platform-Aware Storage Helpers ──────────────────────────────────────────

  /**
   * Saves an item to persistent storage (SecureStorage on native, localStorage on browser)
   * and updates the in-memory cache.
   */
  private async setItem(key: string, value: string): Promise<void> {
    // 1. Update memory cache immediately
    this.memory.set(key, value);

    // 2. Persist to appropriate storage backend
    if (Capacitor.isNativePlatform()) {
      try {
        await SecureStoragePlugin.set({ key, value });
      } catch (e) {
        console.warn(
          `[TokenStorage] SecureStoragePlugin.set failed for "${key}". Value remains active in memory cache.`,
          e
        );
      }
    } else {
      try {
        localStorage.setItem(key, value);
      } catch (e) {
        console.warn(
          `[TokenStorage] localStorage.setItem failed for "${key}". Value remains active in memory cache.`,
          e
        );
      }
    }
  }

  /**
   * Retrieves an item from storage.
   * Checks the in-memory cache first. On miss, reads from persistent storage
   * (SecureStorage on native, localStorage on browser) and populates the memory cache.
   */
  private async getItem(key: string): Promise<string | null> {
    // 1. Memory Cache lookup
    if (this.memory.has(key)) {
      return this.memory.get(key)!;
    }

    // 2. Storage miss: read from persistent storage
    let value: string | null = null;

    if (Capacitor.isNativePlatform()) {
      try {
        const result = await SecureStoragePlugin.get({ key });
        value = result?.value ?? null;
      } catch {
        // SecureStoragePlugin.get rejects if key does not exist. Return null gracefully.
        value = null;
      }
    } else {
      try {
        value = localStorage.getItem(key);
      } catch (e) {
        console.warn(`[TokenStorage] localStorage.getItem failed for "${key}"`, e);
        value = null;
      }
    }

    // 3. Cache non-null result in memory
    if (value !== null) {
      this.memory.set(key, value);
    }

    return value;
  }

  /**
   * Deletes an item from persistent storage and in-memory cache.
   */
  private async removeItem(key: string): Promise<void> {
    // 1. Evict from memory cache immediately
    this.memory.delete(key);

    // 2. Remove from persistent storage
    if (Capacitor.isNativePlatform()) {
      try {
        await SecureStoragePlugin.remove({ key });
      } catch (e) {
        console.warn(`[TokenStorage] SecureStoragePlugin.remove failed for "${key}"`, e);
      }
    } else {
      try {
        localStorage.removeItem(key);
      } catch (e) {
        console.warn(`[TokenStorage] localStorage.removeItem failed for "${key}"`, e);
      }
    }
  }

  /**
   * Internal helper for synchronous cache lookup.
   * On browser, falls back to localStorage if memory cache is not yet populated.
   */
  private getItemSync(key: string): string | null {
    if (this.memory.has(key)) {
      return this.memory.get(key)!;
    }
    if (!Capacitor.isNativePlatform()) {
      try {
        const val = localStorage.getItem(key);
        if (val !== null) {
          this.memory.set(key, val);
        }
        return val;
      } catch {
        return null;
      }
    }
    return null;
  }

  // ─── Public API ────────────────────────────────────────────────────────────

  /**
   * Stores Access Token, Refresh Token, and Emergency Contacts flag.
   * Updates memory cache synchronously and persists to secure storage asynchronously.
   * Returns a Promise that resolves when persistence settles.
   */
  async setTokens(tokens: AuthResponse['data']): Promise<void> {
    const hasEmergency = String(tokens.hasEmergencyContacts);
    this.memory.set(this.accessTokenKey, tokens.accessToken);
    this.memory.set(this.refreshTokenKey, tokens.refreshToken);
    this.memory.set(this.emergencyKey, hasEmergency);

    await Promise.all([
      this.setItem(this.accessTokenKey, tokens.accessToken),
      this.setItem(this.refreshTokenKey, tokens.refreshToken),
      this.setItem(this.emergencyKey, hasEmergency)
    ]);
  }

  getAccessToken(): string | null {
    return this.getItemSync(this.accessTokenKey);
  }

  async getAccessTokenAsync(): Promise<string | null> {
    return this.getItem(this.accessTokenKey);
  }

  getRefreshToken(): string | null {
    return this.getItemSync(this.refreshTokenKey);
  }

  async getRefreshTokenAsync(): Promise<string | null> {
    return this.getItem(this.refreshTokenKey);
  }

  async setUser(user: Account): Promise<void> {
    const raw = JSON.stringify(user);
    this.memory.set(this.userKey, raw);
    await this.setItem(this.userKey, raw);
  }

  getUser(): Account | null {
    const raw = this.getItemSync(this.userKey);
    if (!raw) return null;
    try {
      return JSON.parse(raw) as Account;
    } catch {
      return null;
    }
  }

  async getUserAsync(): Promise<Account | null> {
    const raw = await this.getItem(this.userKey);
    if (!raw) return null;
    try {
      return JSON.parse(raw) as Account;
    } catch {
      return null;
    }
  }

  async markEmergencyCompleted(): Promise<void> {
    this.memory.set(this.emergencyKey, 'true');
    await this.setItem(this.emergencyKey, 'true');
  }

  getEmergencyFlag(): boolean {
    return this.getItemSync(this.emergencyKey) === 'true';
  }

  /**
   * Clears memory cache and removes all keys from persistent storage.
   * Returns a Promise that resolves when storage deletion is complete.
   */
  async clear(): Promise<void> {
    this.memory.delete(this.accessTokenKey);
    this.memory.delete(this.refreshTokenKey);
    this.memory.delete(this.userKey);
    this.memory.delete(this.emergencyKey);

    if (Capacitor.isNativePlatform()) {
      try {
        await SecureStoragePlugin.clear();
      } catch {
        await Promise.all([
          this.removeItem(this.accessTokenKey),
          this.removeItem(this.refreshTokenKey),
          this.removeItem(this.userKey),
          this.removeItem(this.emergencyKey)
        ]);
      }
    } else {
      await Promise.all([
        this.removeItem(this.accessTokenKey),
        this.removeItem(this.refreshTokenKey),
        this.removeItem(this.userKey),
        this.removeItem(this.emergencyKey)
      ]);
    }
  }

  isLoggedIn(): boolean {
    return this.getAccessToken() !== null;
  }

  async isLoggedInAsync(): Promise<boolean> {
    const token = await this.getAccessTokenAsync();
    return token !== null;
  }
}