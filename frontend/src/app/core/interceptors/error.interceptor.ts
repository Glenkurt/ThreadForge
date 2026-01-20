import { HttpInterceptorFn, HttpErrorResponse } from '@angular/common/http';
import { catchError, throwError } from 'rxjs';

/**
 * Global HTTP error interceptor.
 *
 * Handles common HTTP errors consistently across the application:
 * - 401 Unauthorized: Redirects to login (token expired/invalid)
 * - 403 Forbidden: User lacks permissions
 * - 404 Not Found: Resource doesn't exist
 * - 500+ Server Errors: Backend issues
 *
 * Best Practice: Centralize error handling to avoid duplication in every service.
 */
export const errorInterceptor: HttpInterceptorFn = (req, next) => {
  return next(req).pipe(
    catchError((error: HttpErrorResponse) => {
      let errorMessage = 'An unexpected error occurred';

      switch (error.status) {
        case 0:
          // Network error or CORS issue
          errorMessage = 'Unable to connect to the server. Please check your internet connection.';
          console.error('Network Error:', error);
          break;

        case 401:
          // Unauthorized - token expired or invalid
          errorMessage = 'Your session has expired. Please log in again.';
          console.warn('Unauthorized access - redirecting to login');

          // Clear stored tokens
          localStorage.removeItem('access_token');
          localStorage.removeItem('refresh_token');

          // Redirect to login page
          // Uncomment when auth routes are implemented:
          // router.navigate(['/auth/login'], { queryParams: { returnUrl: router.url } });
          break;

        case 403:
          // Forbidden - user doesn't have permission
          errorMessage = 'You do not have permission to perform this action.';
          console.warn('Forbidden access:', req.url);
          break;

        case 404:
          // Not found
          errorMessage = 'The requested resource was not found.';
          break;

        case 422:
          // Validation error
          errorMessage = error.error?.detail || 'Validation failed. Please check your input.';
          break;

        case 429:
          // Rate limited
          errorMessage = 'Too many requests. Please wait a moment and try again.';
          break;

        case 500:
        case 502:
        case 503:
        case 504:
          // Server errors
          errorMessage = 'A server error occurred. Please try again later.';
          console.error('Server Error:', error);
          break;

        default:
          // Other errors
          errorMessage = error.error?.detail || error.message || errorMessage;
      }

      // TODO: Integrate with a toast/notification service
      // Example: inject(ToastService).showError(errorMessage);

      console.error(`HTTP Error ${error.status}:`, errorMessage);

      // Re-throw with enhanced error message
      return throwError(() => ({
        status: error.status,
        message: errorMessage,
        originalError: error
      }));
    })
  );
};
