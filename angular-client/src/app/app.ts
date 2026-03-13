import { Component, computed, inject } from '@angular/core';
import { RouterLink, RouterLinkActive, RouterOutlet } from '@angular/router';
import { AuthService } from './core/auth.service';

@Component({
  selector: 'app-root',
  standalone: true,
  imports: [RouterOutlet, RouterLink, RouterLinkActive],
  templateUrl: './app.html',
  styleUrl: './app.css',
})
export class App {
  private readonly auth = inject(AuthService);
  readonly showLogout = computed(() => !!this.auth.user());
  readonly canAccessEmployee = computed(() => this.auth.hasRole('Employee', 'Supervisor', 'Admin'));
  readonly canAccessSupervisor = computed(() => this.auth.hasRole('Supervisor', 'Admin'));
  readonly canAccessAdmin = computed(() => this.auth.hasRole('Admin'));

  logout() {
    this.auth.logout();
  }
}
