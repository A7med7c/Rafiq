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
        this.profileImageUrl.set(res.data?.profileImageUrl ?? null);
        this.gender.set(res.data?.gender ?? null);
      },
      error: () => {}
    });
  }

  /** Called after a successful photo upload so all pages update immediately. */
  setImageUrl(url: string | null): void {
    this.profileImageUrl.set(url);
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
    const g = (this.gender() ?? '').toLowerCase();
    return g === 'female' || g === '2' ? 'images/sarah_avatar.png' : 'images/ahmed_avatar.png';
  }
}
