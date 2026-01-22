import { Injectable, signal, computed } from '@angular/core';
import { HttpClient } from '@angular/common/http';
import { Observable, tap, catchError, of } from 'rxjs';
import { environment } from '../../../environments/environment';

/**
 * Authentication response from the API.
 */
export interface AuthResponse {
  accessToken: string;
  refreshToken: string;
  expiresIn: number;
  tokenType: string;
}

/**
 * User information decoded from JWT or API.
 */
export interface User {
  userId: string;
  email: string;
  roles: string[];
}

/**
 * Authentication service using Angular Signals for reactive state management.
 *
 * Best Practices:
 * - Uses signals for lightweight reactive state (Angular 17+)
 * - Stores tokens in localStorage for persistence across sessions
 * - Provides computed properties for derived state
 * - Single source of truth for authentication state
 */
@Injectable({
  providedIn: 'root'
})
export class AuthService {
  private readonly apiUrl = `${environment.apiBaseUrl}/v1/auth`;
  private readonly ACCESS_TOKEN_KEY = 'access_token';

  // Reactive state using signals
  private currentUserSignal = signal<User | null>(null);
  private isLoadingSignal = signal<boolean>(false);

  // Public computed properties
  readonly currentUser = this.currentUserSignal.asReadonly();
  readonly isAuthenticated = computed(() => this.currentUserSignal() !== null);
  readonly isLoading = this.isLoadingSignal.asReadonly();

  constructor(private http: HttpClient) {
    // Initialize auth state from stored token on app startup
    this.initializeAuthState();
  }

  /**
   * Attempts to restore authentication state from stored tokens.
   */
  private initializeAuthState(): void {
    const token = this.getAccessToken();
    if (token) {
      this.loadCurrentUser().subscribe();
      return;
    }

    // Attempt a silent refresh via HttpOnly refresh token cookie
    this.refreshToken().subscribe();
  }

  /**
   * Authenticates user with email and password.
   */
  login(email: string, password: string): Observable<AuthResponse> {
    this.isLoadingSignal.set(true);

    return this.http
      .post<AuthResponse>(`${this.apiUrl}/login`, { email, password }, { withCredentials: true })
      .pipe(
        tap(response => {
          this.storeTokens(response);
          this.loadCurrentUser().subscribe();
          this.isLoadingSignal.set(false);
        }),
        catchError(error => {
          this.isLoadingSignal.set(false);
          throw error;
        })
      );
  }

  /**
   * Refreshes the access token using the stored refresh token.
   */
  refreshToken(): Observable<AuthResponse | null> {
    return this.http
      .post<AuthResponse>(
        `${this.apiUrl}/refresh`,
        { refreshToken: null },
        { withCredentials: true }
      )
      .pipe(
        tap(response => {
          this.storeTokens(response);
        }),
        catchError(() => {
          this.logout();
          return of(null);
        })
      );
  }

  /**
   * Loads current user information from the API.
   */
  loadCurrentUser(): Observable<User | null> {
    return this.http.get<User>(`${this.apiUrl}/me`).pipe(
      tap(user => {
        this.currentUserSignal.set(user);
      }),
      catchError(() => {
        this.currentUserSignal.set(null);
        return of(null);
      })
    );
  }

  /**
   * Logs out the current user and clears all stored tokens.
   */
  logout(): void {
    sessionStorage.removeItem(this.ACCESS_TOKEN_KEY);
    this.currentUserSignal.set(null);
  }

  /**
   * Gets the stored access token.
   */
  getAccessToken(): string | null {
    return sessionStorage.getItem(this.ACCESS_TOKEN_KEY);
  }

  /**
   * Stores authentication tokens securely.
   */
  private storeTokens(response: AuthResponse): void {
    sessionStorage.setItem(this.ACCESS_TOKEN_KEY, response.accessToken);
  }

  /**
   * Checks if the current user has a specific role.
   */
  hasRole(role: string): boolean {
    const user = this.currentUserSignal();
    return user?.roles.includes(role) ?? false;
  }
}
