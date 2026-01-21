import { HttpInterceptorFn } from '@angular/common/http';

const CLIENT_ID_KEY = 'threadforge_client_id';

function getOrCreateClientId(): string {
  const existing = localStorage.getItem(CLIENT_ID_KEY);
  if (existing) return existing;

  const newId = (crypto?.randomUUID?.() ?? `${Date.now()}-${Math.random()}`).toString();
  localStorage.setItem(CLIENT_ID_KEY, newId);
  return newId;
}

/**
 * Adds an anonymous client identifier to API requests.
 * This enables server-side per-user rate limiting without requiring auth for MVP.
 */
export const clientIdInterceptor: HttpInterceptorFn = (req, next) => {
  // Skip for external URLs
  const isExternalUrl = !req.url.startsWith('/api') && !req.url.startsWith(window.location.origin);
  if (isExternalUrl) {
    return next(req);
  }

  const clientId = getOrCreateClientId();
  const clientReq = req.clone({
    setHeaders: {
      'X-Client-Id': clientId
    }
  });

  return next(clientReq);
};
