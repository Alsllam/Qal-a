import { Injectable, computed, inject, signal } from '@angular/core';
import { OAuthService } from 'angular-oauth2-oidc';
import { ALL_PERMISSIONS } from '../permissions/permission-names';
import { PermissionService } from '../permissions/permission.service';
import { APP_SETTINGS } from '../settings/app-settings';
import { MOCK_API_ENABLED } from './auth.tokens';

const DEV_LOGIN_KEY = 'qala.devLogin';

/**
 * Sign-in state. Real mode: OpenIddict authorization code flow with PKCE
 * (angular-oauth2-oidc). Mock mode (dev only): a fake login with every
 * permission, so the console runs without the Auth host.
 */
@Injectable({ providedIn: 'root' })
export class AuthService {
  private readonly oauth = inject(OAuthService);
  private readonly settings = inject(APP_SETTINGS);
  private readonly permissions = inject(PermissionService);
  readonly mockMode = inject(MOCK_API_ENABLED);

  private readonly authenticated = signal(false);
  private readonly name = signal<string | null>(null);
  readonly isAuthenticated = computed(() => this.authenticated());
  readonly userName = computed(() => this.name());

  async init(): Promise<void> {
    if (this.mockMode) {
      if (readSession(DEV_LOGIN_KEY)) this.applyDevLogin();
      return;
    }
    const cfg = this.settings.oAuthConfig;
    this.oauth.configure({
      issuer: cfg.issuer,
      clientId: cfg.clientId,
      responseType: cfg.responseType,
      scope: cfg.scope,
      redirectUri: cfg.redirectUri ?? `${window.location.origin}/`,
      postLogoutRedirectUri: `${window.location.origin}/login`,
      requireHttps: cfg.requireHttps,
      showDebugInformation: cfg.showDebugInformation ?? false,
      // PKCE is on by default for the code flow in angular-oauth2-oidc.
      disablePKCE: false,
      clearHashAfterLogin: true,
    });
    this.oauth.setupAutomaticSilentRefresh();
    try {
      await this.oauth.loadDiscoveryDocumentAndTryLogin();
    } catch (error) {
      console.error('[auth] cannot reach the issuer', error);
    }
    this.refreshFromToken();
    this.oauth.events.subscribe(() => this.refreshFromToken());
  }

  login(returnUrl = '/'): void {
    if (this.mockMode) return;
    this.oauth.initCodeFlow(returnUrl);
  }

  /** Dev-only fake login with all permissions. No-op outside mock mode. */
  devLogin(): void {
    if (!this.mockMode) return;
    writeSession(DEV_LOGIN_KEY, '1');
    this.applyDevLogin();
  }

  logout(): void {
    this.permissions.clear();
    this.authenticated.set(false);
    this.name.set(null);
    if (this.mockMode) {
      writeSession(DEV_LOGIN_KEY, null);
      return;
    }
    this.oauth.logOut();
  }

  private applyDevLogin(): void {
    this.permissions.setGrantedPolicies(ALL_PERMISSIONS);
    this.name.set('Dev Admin');
    this.authenticated.set(true);
  }

  private refreshFromToken(): void {
    const valid = this.oauth.hasValidAccessToken();
    this.authenticated.set(valid);
    if (!valid) {
      this.permissions.clear();
      return;
    }
    const claims = (this.oauth.getIdentityClaims() ?? {}) as Record<
      string,
      unknown
    >;
    this.name.set(
      String(
        claims['name'] ?? claims['preferred_username'] ?? claims['email'] ?? ''
      )
    );
    this.permissions.setGrantedPolicies(
      readPermissionClaims(this.oauth.getAccessToken())
    );
  }
}

/**
 * Reads `permission`/`permissions` claims from a JWT access token.
 * TODO(backend): confirm the claim name the Auth host issues.
 */
export function readPermissionClaims(accessToken: string | null): string[] {
  if (!accessToken) return [];
  const payload = accessToken.split('.')[1];
  if (!payload) return [];
  try {
    const json = JSON.parse(
      decodeURIComponent(
        atob(payload.replace(/-/g, '+').replace(/_/g, '/'))
          .split('')
          .map((c) => '%' + ('00' + c.charCodeAt(0).toString(16)).slice(-2))
          .join('')
      )
    ) as Record<string, unknown>;
    const raw = json['permission'] ?? json['permissions'] ?? [];
    return (Array.isArray(raw) ? raw : [raw]).filter(
      (p): p is string => typeof p === 'string'
    );
  } catch {
    return [];
  }
}

function readSession(key: string): string | null {
  try {
    return sessionStorage.getItem(key);
  } catch {
    return null;
  }
}

function writeSession(key: string, value: string | null): void {
  try {
    if (value === null) sessionStorage.removeItem(key);
    else sessionStorage.setItem(key, value);
  } catch {
    /* storage unavailable */
  }
}
