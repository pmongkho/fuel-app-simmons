import { inject } from '@angular/core';
import { CanActivateFn, Router } from '@angular/router';
import { AppRole, AuthService } from './auth.service';

const routeFor = (role: AppRole) => {
  if (role === 'Employee') return '/reports/new';
  if (role === 'Supervisor') return '/supervisor/entries';
  return '/admin/dashboard';
};

export const authGuard: CanActivateFn = () => {
  const auth = inject(AuthService);
  const router = inject(Router);

  if (auth.user()) return true;

  router.navigateByUrl('/login');
  return false;
};

export const roleGuard = (roles: AppRole[]): CanActivateFn => {
  return () => {
    const auth = inject(AuthService);
    const router = inject(Router);
    const currentUser = auth.user();

    if (!currentUser) {
      router.navigateByUrl('/login');
      return false;
    }

    if (roles.includes(currentUser.role)) return true;

    router.navigateByUrl(routeFor(currentUser.role));
    return false;
  };
};
