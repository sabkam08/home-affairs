import {
  computed,
  inject,
  Injectable,
  signal
} from '@angular/core';
import {
  Observable,
  catchError,
  forkJoin,
  map,
  of,
  tap
} from 'rxjs';

import { AuthApiService } from './auth-api.service';
import {
  AuthSession,
  CurrentUser,
  LoginRequest,
  LoginResponse,
  PermissionResponse
} from './auth.models';

const SESSION_KEY = 'home-affairs.auth.session';

@Injectable({
  providedIn: 'root'
})
export class AuthService {
  private readonly api = inject(AuthApiService);

  private readonly sessionSignal =
    signal<AuthSession | null>(this.readStoredSession());

  private readonly userSignal =
    signal<CurrentUser | null>(null);

  private readonly permissionsSignal =
    signal<ReadonlySet<string>>(new Set<string>());

  readonly session = this.sessionSignal.asReadonly();
  readonly currentUser = this.userSignal.asReadonly();

  readonly authenticated = computed(() => {
    const session = this.sessionSignal();

    if (!session?.accessToken) {
      return false;
    }

    if (!session.expiresAtUtc) {
      return true;
    }

    return new Date(session.expiresAtUtc).getTime() > Date.now();
  });

  readonly permissions = computed(
    () => [...this.permissionsSignal()]
  );

  login(request: LoginRequest): Observable<void> {
    return this.api.login(request).pipe(
      tap(response => this.storeLoginResponse(response)),
      map(() => undefined)
    );
  }

  initializeAuthenticatedUser(): Observable<boolean> {
    if (!this.authenticated()) {
      this.logout();
      return of(false);
    }

    return forkJoin({
      user: this.api.getCurrentUser(),
      permissions: this.api.getPermissions()
    }).pipe(
      tap(({ user, permissions }) => {
        this.userSignal.set(user);
        this.permissionsSignal.set(
          new Set(this.extractPermissions(permissions))
        );
      }),
      map(() => true),
      catchError(() => {
        this.logout();
        return of(false);
      })
    );
  }

  accessToken(): string | null {
    if (!this.authenticated()) {
      return null;
    }

    return this.sessionSignal()?.accessToken ?? null;
  }

  hasPermission(permission: string): boolean {
    return this.permissionsSignal().has(permission);
  }

  hasAnyPermission(...permissions: string[]): boolean {
    return permissions.some(permission =>
      this.hasPermission(permission)
    );
  }

  logout(): void {
    localStorage.removeItem(SESSION_KEY);
    this.sessionSignal.set(null);
    this.userSignal.set(null);
    this.permissionsSignal.set(new Set<string>());
  }

  private storeLoginResponse(response: LoginResponse): void {
    const expiresAtUtc =
      response.accessTokenExpiresAtUtc ??
      response.expiresAtUtc ??
      this.calculateExpiry(response);

    const roles = response.roles ??
      (response.role ? [response.role] : []);

    const session: AuthSession = {
      userId: response.userId,
      accessToken: response.accessToken,
      expiresAtUtc,
      roles
    };

    localStorage.setItem(
      SESSION_KEY,
      JSON.stringify(session)
    );

    this.sessionSignal.set(session);
  }

  private calculateExpiry(
    response: LoginResponse
  ): string | null {
    const seconds =
      response.expiresInSeconds ??
      response.expiresIn;

    if (seconds == null) {
      return null;
    }

    return new Date(
      Date.now() + seconds * 1000
    ).toISOString();
  }

  private extractPermissions(
    response: PermissionResponse
  ): string[] {
    if (Array.isArray(response)) {
      return response;
    }

    return response.permissions ?? [];
  }

  private readStoredSession(): AuthSession | null {
    try {
      const raw = localStorage.getItem(SESSION_KEY);

      if (!raw) {
        return null;
      }

      return JSON.parse(raw) as AuthSession;
    } catch {
      localStorage.removeItem(SESSION_KEY);
      return null;
    }
  }
}