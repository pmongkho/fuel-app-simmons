import { Injectable, signal } from '@angular/core';
import { HttpClient, HttpHeaders } from '@angular/common/http';
import { Router } from '@angular/router';
import { environment } from '../../environments/environment';

export type AppRole = 'Employee' | 'Supervisor' | 'Admin';

export interface LoggedInUser {
  id: number;
  fullName: string;
  email: string;
  role: AppRole;
}

@Injectable({ providedIn: 'root' })
export class AuthService {
  private readonly apiBase = environment.apiBaseUrl;
  private readonly tokenKey = 'fuel_token';
  private readonly userKey = 'fuel_user';
  private readonly inactivityTimeoutMs = 15 * 60 * 1000;
  private logoutTimer: ReturnType<typeof setTimeout> | null = null;
  private inactivityTimer: ReturnType<typeof setTimeout> | null = null;
  private readonly activityEvents: Array<keyof WindowEventMap> = ['click', 'keydown', 'mousemove', 'scroll', 'touchstart'];
  private activityListenersAttached = false;
  readonly user = signal<LoggedInUser | null>(null);

  constructor(private http: HttpClient, private router: Router) {
    this.restoreSession();
  }

  login(email: string, password: string) {
    return this.http.post<{ token: string; user: LoggedInUser }>(`${this.apiBase}/auth/login`, { email, password });
  }

  setSession(token: string, user: LoggedInUser) {
    localStorage.setItem(this.tokenKey, token);
    localStorage.setItem(this.userKey, JSON.stringify(user));
    this.user.set(user);
    this.startSessionTimers(token);
  }

  authHeaders() {
    return new HttpHeaders({ Authorization: `Bearer ${localStorage.getItem(this.tokenKey) || ''}` });
  }

  hasRole(...roles: AppRole[]) {
    const currentUser = this.user();
    return !!currentUser && roles.includes(currentUser.role);
  }

  isAuthenticated(): boolean {
    const token = this.getToken();
    if (!token || this.isTokenExpired(token)) {
      this.clearSession();
      return false;
    }

    return this.user() !== null;
  }

  logout() {
    this.clearTimers();
    this.detachActivityListeners();
    this.clearSession();
    this.router.navigate(['/login']);
  }

  getToken() {
    return localStorage.getItem(this.tokenKey);
  }

  private clearSession() {
    localStorage.removeItem(this.tokenKey);
    localStorage.removeItem(this.userKey);
    this.user.set(null);
  }

  routeForRole(role: AppRole) {
    if (role === 'Employee') return '/reports/new';
    if (role === 'Supervisor') return '/supervisor/entries';
    return '/admin/dashboard';
  }

  private restoreSession() {
    const token = this.getToken();
    const rawUser = localStorage.getItem(this.userKey);
    if (!token || !rawUser) return;

    if (this.isTokenExpired(token)) {
      this.clearSession();
      return;
    }

    try {
      const parsedUser = JSON.parse(rawUser) as LoggedInUser;
      this.user.set(parsedUser);
      this.startSessionTimers(token);
    } catch {
      this.clearSession();
    }
  }

  private startSessionTimers(token: string) {
    this.clearTimers();

    const expirationMs = this.getTokenExpirationMs(token);
    if (expirationMs) {
      const delayMs = Math.max(expirationMs - Date.now(), 0);
      this.logoutTimer = setTimeout(() => this.logout(), delayMs);
    }

    this.attachActivityListeners();
    this.resetInactivityTimer();
  }

  private resetInactivityTimer = () => {
    if (!this.user()) {
      return;
    }

    if (this.inactivityTimer) {
      clearTimeout(this.inactivityTimer);
    }

    const token = this.getToken();
    if (!token) {
      return;
    }

    const expirationMs = this.getTokenExpirationMs(token);
    const remainingUntilExpiry = expirationMs ? Math.max(expirationMs - Date.now(), 0) : this.inactivityTimeoutMs;
    const timeout = Math.min(this.inactivityTimeoutMs, remainingUntilExpiry);

    this.inactivityTimer = setTimeout(() => this.logout(), timeout);
  };

  private clearTimers() {
    if (this.logoutTimer) {
      clearTimeout(this.logoutTimer);
      this.logoutTimer = null;
    }

    if (this.inactivityTimer) {
      clearTimeout(this.inactivityTimer);
      this.inactivityTimer = null;
    }
  }

  private attachActivityListeners() {
    if (this.activityListenersAttached) {
      return;
    }

    this.activityEvents.forEach((eventName) => window.addEventListener(eventName, this.resetInactivityTimer, { passive: true }));
    this.activityListenersAttached = true;
  }

  private detachActivityListeners() {
    if (!this.activityListenersAttached) {
      return;
    }

    this.activityEvents.forEach((eventName) => window.removeEventListener(eventName, this.resetInactivityTimer));
    this.activityListenersAttached = false;
  }

  private getTokenExpirationMs(token: string): number | null {
    const payload = this.decodeJwtPayload(token);
    if (!payload || typeof payload.exp !== 'number') {
      return null;
    }

    return payload.exp * 1000;
  }

  private isTokenExpired(token: string): boolean {
    const expirationMs = this.getTokenExpirationMs(token);
    return expirationMs !== null && expirationMs <= Date.now();
  }

  private decodeJwtPayload(token: string): { exp?: number } | null {
    try {
      const tokenParts = token.split('.');
      if (tokenParts.length < 2) {
        return null;
      }

      const base64 = tokenParts[1].replace(/-/g, '+').replace(/_/g, '/');
      const normalized = base64.padEnd(base64.length + ((4 - (base64.length % 4)) % 4), '=');
      return JSON.parse(atob(normalized));
    } catch {
      return null;
    }
  }
}
