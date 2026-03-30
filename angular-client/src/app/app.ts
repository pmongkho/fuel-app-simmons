import { Component, computed, inject, signal } from '@angular/core';
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
  readonly canAccessEmployee = computed(() => this.auth.hasRole('Employee', 'Admin'));
  readonly canAccessTrailers = computed(() => this.auth.hasRole('Employee', 'Supervisor', 'Admin'));
  readonly canAccessSupervisor = computed(() => this.auth.hasRole('Supervisor', 'Admin'));
  readonly canAccessAdmin = computed(() => this.auth.hasRole('Supervisor', 'Admin'));
  readonly mobileNavOpen = signal(false);

  toggleMobileNav(): void {
    this.mobileNavOpen.update((isOpen) => !isOpen);
  }

  closeMobileNav(): void {
    this.mobileNavOpen.set(false);
  }

  logout() {
    this.closeMobileNav();
    this.auth.logout();
  }
}
