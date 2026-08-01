import { inject } from '@angular/core';
import { CanActivateFn, Router } from '@angular/router';
import { map } from 'rxjs';
import { AuthService } from '../Services/auth-service';

export const adminGuard: CanActivateFn = () => {
  const authService = inject(AuthService);
  const router = inject(Router);

  return authService.initializeSession().pipe(
    map(user => {
      if (!user) {
        return router.createUrlTree(['/login']);
      }

      return user.role === 'Admin'
        ? true
        : router.createUrlTree(['/dashboard']);
    })
  );
};
