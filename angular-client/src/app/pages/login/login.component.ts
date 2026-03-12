import { Component } from '@angular/core';
import { FormsModule } from '@angular/forms';
import { Router } from '@angular/router';
import { AuthService } from '../../core/auth.service';

@Component({
  selector: 'app-login',
  standalone: true,
  imports: [FormsModule],
  template: `
    <section class="page">
      <h2>Fuel App Login</h2>
      <form (ngSubmit)="submit()">
        <input [(ngModel)]="email" name="email" placeholder="Email" required />
        <input [(ngModel)]="password" name="password" type="password" placeholder="Password" required />
        <button type="submit">Login</button>
      </form>
      <p>{{ message }}</p>
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
      error: () => (this.message = 'Login failed'),
    });
  }
}
