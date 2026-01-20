import { HttpInterceptorFn } from '@angular/common/http';
import { inject } from '@angular/core';
import { AuthService } from '../services/auth.service';

/**
 * Authentication interceptor that attaches JWT Bearer tokens to outgoing requests.
 *
 * Best Practices:
 * - Automatically injects token for all API requests
 * - Excludes auth endpoints (login/refresh) to prevent circular dependencies
 * - Uses functional interceptor pattern (Angular 17+)
 */
export const authInterceptor: HttpInterceptorFn = (req, next) => {
  const authService = inject(AuthService);

  // Skip token injection for auth endpoints
  const isAuthEndpoint =
    req.url.includes('/api/v1/auth/login') || req.url.includes('/api/v1/auth/refresh');

  // Skip for external URLs
  const isExternalUrl = !req.url.startsWith('/api') && !req.url.startsWith(window.location.origin);

  if (isAuthEndpoint || isExternalUrl) {
    return next(req);
  }

  const token = authService.getAccessToken();

  if (token) {
    // Clone the request and add the authorization header
    const authReq = req.clone({
      setHeaders: {
        Authorization: `Bearer ${token}`
      }
    });
    return next(authReq);
  }

  return next(req);
};
