import { inject } from '@angular/core';
import { CanActivateFn, Router } from '@angular/router';
import { map, of } from 'rxjs';
import { AuthService } from '../Services/auth-service';
import { TokenStorageService } from '../Services/token-storage-service';

export const authGuard: CanActivateFn = () => {
  const authService = inject(AuthService);
  const tokenStorage = inject(TokenStorageService);
  const router = inject(Router);

  // Fast path: tokens in storage → user is authenticated (profile may not yet
  // be loaded into currentUser, e.g. new user completing onboarding).
  if (tokenStorage.isLoggedIn()) {
    return of(true);
  }

  return authService.initializeSession().pipe(
    map((user) => {
      // Allow through if we got a user object, OR if tokens appeared during the
      // session init (e.g. the refresh succeeded and restored the tokens).
      if (user || tokenStorage.isLoggedIn()) {
        return true;
      }

      return router.createUrlTree(['/login']);
    })
  );
};
