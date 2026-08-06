import { Injectable, inject, signal } from '@angular/core';
import { HealthProfileService } from './health-profile.service';
import { TokenStorageService } from './token-storage-service';
import { environment } from '../Environments/Environment';

@Injectable({ providedIn: 'root' })
export class ProfileCacheService {
  private readonly healthSvc = inject(HealthProfileService);
  private readonly tokenSvc = inject(TokenStorageService);

  readonly profileImageUrl = signal<string | null>(null);
  readonly gender = signal<string | null>(null);
  private loaded = false;

  /** Fetches and caches the health profile (no-op after first call). */
  ensure(): void {
    if (this.loaded) return;
    this.loaded = true;
    this.healthSvc.getMyProfile().subscribe({
      next: res => {
        this.setImageUrl(res.data?.profileImageUrl ?? null);
        this.gender.set(res.data?.gender ?? null);
      },
      error: () => { }
    });
  }

  /** Called after a successful photo upload so all pages update immediately. */
  setImageUrl(url: string | null): void {
    if (!url) {
      this.profileImageUrl.set(null);
      return;
    }

    // If the caller already supplied a cache-busted URL (contains v=),
    // use it as-is to keep a single canonical URL across services.
    if (/[?&]v=\d+/.test(url)) {
      this.profileImageUrl.set(url);
      return;
    }

    // Append cache-busting query so updated images propagate immediately
    const separator = url.includes('?') ? '&' : '?';
    this.profileImageUrl.set(`${url}${separator}v=${Date.now()}`);
  }

  /**
   * Returns the URL to display in the navbar:
   *   - user's own uploaded photo, or
   *   - user's auth account profile photo, or
   *   - gender-appropriate default avatar
   */
  resolveNavbarAvatar(): string {
    const imgUrl = this.profileImageUrl() || this.tokenSvc.getUser()?.profileImageUrl;
    if (imgUrl) return `${environment.fileBaseUrl}${imgUrl}`;
    
    const u = this.tokenSvc.getUser();
    let initials = '?';
    let seed = 'U';
    
    if (u) {
      const f = (u.firstName ?? '')[0] ?? '';
      const l = (u.lastName ?? '')[0] ?? '';
      initials = (f + l).toUpperCase() || (u.email ?? '?')[0].toUpperCase();
      seed = u.firstName?.trim() || u.email || 'U';
    }

    const palette = ['#0EAFD7', '#7C3AED', '#16A34A', '#EA580C', '#0D9488'];
    let h = 0;
    for (let i = 0; i < seed.length; i++) h = seed.charCodeAt(i) + ((h << 5) - h);
    const bgColor = palette[Math.abs(h) % palette.length];

    const svg = `<svg xmlns="http://www.w3.org/2000/svg" viewBox="0 0 100 100"><rect width="100" height="100" fill="${bgColor}"/><text x="50" y="50" font-family="Arial, sans-serif" font-size="45" font-weight="bold" fill="#ffffff" text-anchor="middle" dominant-baseline="central">${initials}</text></svg>`;
    
    return `data:image/svg+xml;charset=UTF-8,${encodeURIComponent(svg)}`;
  }
}
