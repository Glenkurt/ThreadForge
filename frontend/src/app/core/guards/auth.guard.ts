import { inject } from '@angular/core';
import { Router, CanActivateFn } from '@angular/router';
import { AuthService } from '../services/auth.service';

/**
 * Route guard that protects routes requiring authentication.
 *
 * Best Practice: Use functional guards (Angular 14+) for simpler, more testable code.
 *
 * Usage in routes:
 * ```typescript
 * {
 *   path: 'dashboard',
 *   loadComponent: () => import('./features/dashboard/dashboard.component'),
 *   canActivate: [authGuard]
 * }
 * ```
 */
export const authGuard: CanActivateFn = (route, state) => {
  const authService = inject(AuthService);
  const router = inject(Router);

  if (authService.isAuthenticated()) {
    return true;
  }

  // Store the attempted URL for redirecting after login
  router.navigate(['/auth/login'], {
    queryParams: { returnUrl: state.url }
  });

  return false;
};

/**
 * Route guard that protects routes requiring specific roles.
 *
 * Usage in routes:
 * ```typescript
 * {
 *   path: 'admin',
 *   loadComponent: () => import('./features/admin/admin.component'),
 *   canActivate: [roleGuard],
 *   data: { roles: ['Admin'] }
 * }
 * ```
 */
export const roleGuard: CanActivateFn = (route, state) => {
  const authService = inject(AuthService);
  const router = inject(Router);

  if (!authService.isAuthenticated()) {
    router.navigate(['/auth/login'], {
      queryParams: { returnUrl: state.url }
    });
    return false;
  }

  const requiredRoles = route.data['roles'] as string[] | undefined;

  if (!requiredRoles || requiredRoles.length === 0) {
    return true;
  }

  const hasRequiredRole = requiredRoles.some(role => authService.hasRole(role));

  if (!hasRequiredRole) {
    // User is authenticated but lacks required role
    router.navigate(['/unauthorized']);
    return false;
  }

  return true;
};
