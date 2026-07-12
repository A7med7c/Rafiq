import { Injectable } from '@angular/core';

@Injectable({ providedIn: 'root' })
export class ProfileSelectionService {
  private static readonly KEY = 'fp_active_profile';

  get selectedProfileId(): string | null {
    return localStorage.getItem(ProfileSelectionService.KEY);
  }

  select(id: string): void {
    localStorage.setItem(ProfileSelectionService.KEY, id);
  }

  clear(): void {
    localStorage.removeItem(ProfileSelectionService.KEY);
  }
}
