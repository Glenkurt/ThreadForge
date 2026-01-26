import { Routes } from '@angular/router';

/**
 * Application routes configuration.
 *
 * Best Practices:
 * - Use lazy loading for feature modules (loadComponent)
 * - Group related routes under feature paths
 * - Apply guards at the route level
 */
export const routes: Routes = [
  {
    path: '',
    pathMatch: 'full',
    redirectTo: 'generator'
  },
  {
    path: 'generator',
    loadComponent: () =>
      import('./features/generator/generator.component').then(m => m.GeneratorComponent)
  },
  {
    path: 'analyze',
    loadComponent: () =>
      import('./features/profile-analysis/profile-analysis.component').then(m => m.ProfileAnalysisComponent)
  },
  {
    path: 'dashboard',
    loadComponent: () =>
      import('./features/dashboard/dashboard.component').then(m => m.DashboardComponent)
    // Uncomment to protect route:
    // canActivate: [authGuard]
  },
  {
    path: 'auth/login',
    loadComponent: () => import('./features/auth/login/login.component').then(m => m.LoginComponent)
  },
  {
    path: 'unauthorized',
    loadComponent: () =>
      import('./features/unauthorized/unauthorized.component').then(m => m.UnauthorizedComponent)
  },
  // Example: Protected admin route with role guard
  // {
  //   path: 'admin',
  //   loadComponent: () => import('./features/admin/admin.component').then(m => m.AdminComponent),
  //   canActivate: [roleGuard],
  //   data: { roles: ['Admin'] }
  // },
  {
    path: '**',
    redirectTo: 'generator'
  }
];
