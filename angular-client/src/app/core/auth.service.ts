import { Injectable, signal } from '@angular/core';
import { HttpClient, HttpHeaders } from '@angular/common/http';
import { Router } from '@angular/router';

export type AppRole = 'Employee' | 'Supervisor' | 'Admin';

export interface LoggedInUser {
  id: number;
  fullName: string;
  email: string;
  role: AppRole;
}

@Injectable({ providedIn: 'root' })
export class AuthService {
  private readonly apiBase = 'http://localhost:5152/api';
  private readonly tokenKey = 'fuel_token';
  readonly user = signal<LoggedInUser | null>(null);

  constructor(private http: HttpClient, private router: Router) {}

  login(email: string, password: string) {
    return this.http.post<{ token: string; user: LoggedInUser }>(`${this.apiBase}/auth/login`, { email, password });
  }

  setSession(token: string, user: LoggedInUser) {
    localStorage.setItem(this.tokenKey, token);
    this.user.set(user);
  }

  authHeaders() {
    return new HttpHeaders({ Authorization: `Bearer ${localStorage.getItem(this.tokenKey) || ''}` });
  }

  logout() {
    localStorage.removeItem(this.tokenKey);
    this.user.set(null);
    this.router.navigate(['/login']);
  }

  routeForRole(role: AppRole) {
    if (role === 'Employee') return '/reports/new';
    if (role === 'Supervisor') return '/supervisor/entries';
    return '/admin/dashboard';
  }
}
