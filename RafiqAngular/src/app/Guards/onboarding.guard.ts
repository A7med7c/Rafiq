import { inject } from '@angular/core';
import { CanActivateFn, Router, UrlTree } from '@angular/router';
import { map, of, switchMap, Observable } from 'rxjs';
import { AuthService } from '../Services/auth-service';
import { HealthProfileService } from '../Services/health-profile.service';

export const onboardingGuard: CanActivateFn = () => {
  const authService = inject(AuthService);
  const healthProfileSvc = inject(HealthProfileService);
  const router = inject(Router);

  const checkOnboardingAccess = (): Observable<boolean | UrlTree> => {
    return healthProfileSvc.hasProfile().pipe(
      map((hasProfile) => {
        if (!hasProfile) {
          return true;
        }
        // User already has a patient profile -> redirect to dashboard
        return router.createUrlTree(['/dashboard']);
      })
    );
  };

  if (authService.isLoggedIn) {
    return checkOnboardingAccess();
  }

  return authService.initializeSession().pipe(
    switchMap((user) => {
      if (user) {
        return checkOnboardingAccess();
      }
      return of(router.createUrlTree(['/login']));
    })
  );
};
