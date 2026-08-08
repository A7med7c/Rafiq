import { inject } from '@angular/core';
import { CanActivateFn, Router, UrlTree } from '@angular/router';
import { map, of, switchMap, Observable } from 'rxjs';
import { AuthService } from '../Services/auth-service';
import { HealthProfileService } from '../Services/health-profile.service';
import { TokenStorageService } from '../Services/token-storage-service';

export const authGuard: CanActivateFn = () => {
  const authService = inject(AuthService);
  const healthProfileSvc = inject(HealthProfileService);
  const tokenStorage = inject(TokenStorageService);
  const router = inject(Router);

  const checkProfile = (): Observable<boolean | UrlTree> => {
    if (authService.currentUser?.role === 'Admin') {
      return of(true);
    }
    return healthProfileSvc.hasProfile().pipe(
      map((hasProfile) => {
        if (hasProfile) {
          return true;
        }
        return router.createUrlTree(['/onboarding/welcome']);
      })
    );
  };

  if (tokenStorage.isLoggedIn()) {
    return checkProfile();
  }

  return authService.initializeSession().pipe(
    switchMap((user) => {
      if (user || tokenStorage.isLoggedIn()) {
        return checkProfile();
      }
      return of(router.createUrlTree(['/login']));
    })
  );
};
