import { ApplicationConfig } from '@angular/core';
import { provideRouter, withComponentInputBinding } from '@angular/router';
import { provideHttpClient, withInterceptors } from '@angular/common/http';

import { routes } from './app.routes';
import { errorInterceptor, authInterceptor } from './core';

/**
 * Application configuration using functional providers (Angular 17+).
 *
 * Best Practices:
 * - Use functional interceptors with withInterceptors()
 * - Enable component input binding for route params
 * - Keep configuration centralized
 */
export const appConfig: ApplicationConfig = {
  providers: [
    provideRouter(routes, withComponentInputBinding()),
    provideHttpClient(
      withInterceptors([
        authInterceptor, // Add auth token to requests
        errorInterceptor // Handle HTTP errors globally
      ])
    )
  ]
};
