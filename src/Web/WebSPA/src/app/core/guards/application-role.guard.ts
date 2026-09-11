import { inject } from '@angular/core';
import { CanActivateFn, Router } from '@angular/router';
import { catchError, map, of } from 'rxjs';
import { AuthService } from '../services/auth.service';

export const applicationRoleGuard: CanActivateFn = route => {
  const auth = inject(AuthService);
  const router = inject(Router);
  if (!auth.isLoggedIn()) return router.createUrlTree(['/login']);

  const roles = route.data['roles'] as string[] | undefined;
  return auth.me().pipe(
    map(profile => roles?.some(role => profile.roles.includes(role))
      ? true : router.createUrlTree(['/'])),
    catchError(() => of(router.createUrlTree(['/login']))),
  );
};
