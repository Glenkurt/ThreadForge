/**
 * Core module barrel export.
 *
 * The core module contains singleton services, guards, and interceptors
 * that are used throughout the application.
 *
 * Import from this file for cleaner imports:
 * ```typescript
 * import { AuthService, authGuard, errorInterceptor } from './core';
 * ```
 */

// Services
export { AuthService, type AuthResponse, type User } from './services/auth.service';

// Guards
export { authGuard, roleGuard } from './guards/auth.guard';

// Interceptors
export { errorInterceptor, authInterceptor } from './interceptors';
