import { inject } from '@angular/core';
import { CanActivateFn, Router } from '@angular/router';
import { map, of } from 'rxjs';
import { AuthService } from '../Services/auth-service';

export const authGuard: CanActivateFn = () => {
  const authService = inject(AuthService);
  const router = inject(Router);

  if (authService.isLoggedIn) {
    return of(true);
  }

  return authService.initializeSession().pipe(
    map((user) => {
      if (user) {
        return true;
      }

      return router.createUrlTree(['/login']);
    })
  );
};
