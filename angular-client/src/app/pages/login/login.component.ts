import { Component } from '@angular/core';
import { FormsModule } from '@angular/forms';
import { Router } from '@angular/router';
import { AuthService } from '../../core/auth.service';

@Component({
  selector: 'app-login',
  standalone: true,
  imports: [FormsModule],
  template: `
    <section class="mx-auto w-full max-w-md rounded-2xl border border-slate-200 bg-white p-5 shadow-sm sm:p-7">
      <h2 class="text-2xl font-semibold text-slate-800">Login</h2>
      <p class="mt-1 text-sm text-slate-500">Sign in to submit and review fuel reports.</p>

      <form class="mt-6 space-y-3" (ngSubmit)="submit()">
        <label class="block">
          <span class="mb-1 block text-sm font-medium text-slate-600">Email</span>
          <input
            [(ngModel)]="email"
            name="email"
            placeholder="name@company.com"
            required
            class="w-full rounded-lg border border-slate-300 px-3 py-2 text-sm outline-none ring-sky-300 focus:ring"
          />
        </label>

        <label class="block">
          <span class="mb-1 block text-sm font-medium text-slate-600">Password</span>
          <input
            [(ngModel)]="password"
            name="password"
            type="password"
            placeholder="••••••••"
            required
            class="w-full rounded-lg border border-slate-300 px-3 py-2 text-sm outline-none ring-sky-300 focus:ring"
          />
        </label>

        <button type="submit" class="mt-2 w-full rounded-lg bg-sky-600 px-4 py-2.5 font-semibold text-white hover:bg-sky-700">
          Sign In
        </button>
      </form>

      @if (message) {
        <p class="mt-3 rounded-lg bg-rose-50 px-3 py-2 text-sm text-rose-600">{{ message }}</p>
      }
    </section>
  `,
})
export class LoginComponent {
  email = '';
  password = '';
  message = '';

  constructor(private auth: AuthService, private router: Router) {}

  submit() {
    this.auth.login(this.email, this.password).subscribe({
      next: (res) => {
        this.auth.setSession(res.token, res.user);
        this.router.navigateByUrl(this.auth.routeForRole(res.user.role));
      },
      error: () => (this.message = 'Login failed. Please check your credentials.'),
    });
  }
}
