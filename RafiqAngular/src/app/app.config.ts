import { ApplicationConfig, APP_INITIALIZER, provideBrowserGlobalErrorListeners } from '@angular/core';
import { provideHttpClient, withInterceptors } from '@angular/common/http';
import { provideRouter } from '@angular/router';
import { provideMarkdown } from 'ngx-markdown';
import { provideLottieOptions } from 'ngx-lottie';

import { routes } from './app.routes';
import { authInterceptor } from './Interceptors/auth.interceptor';
import { AuthService } from './Services/auth-service';

function initializeAuth(authService: AuthService) {
  return () => authService.initializeSession().subscribe();
}

export const appConfig: ApplicationConfig = {
  providers: [
    provideBrowserGlobalErrorListeners(),
    provideRouter(routes),
    provideHttpClient(withInterceptors([authInterceptor])),
    provideMarkdown(),
    provideLottieOptions({
      player: () => import('lottie-web')
    }),
    {
      provide: APP_INITIALIZER,
      useFactory: initializeAuth,
      deps: [AuthService],
      multi: true
    }
  ]
};
