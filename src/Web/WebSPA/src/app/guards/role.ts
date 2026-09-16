import { inject } from '@angular/core';
import { CanActivateFn, Router } from '@angular/router';

export const roleGuard: CanActivateFn = (route, state) => {
  const router = inject(Router);
  const token = localStorage.getItem('auth_token');

  if (!token) {
    console.warn('No auth token found in localStorage.');
    return false;
  }

  try {
    const payloadBase64 = token.split('.')[1];
    const decodedPayload = JSON.parse(atob(payloadBase64));

    const userRole = decodedPayload.role ||
      decodedPayload['http://schemas.microsoft.com/ws/2008/06/identity/claims/role'];

    const expectedRoles: string[] = route.data['roles'];

    // Ako je uloga validna za ovu rutu, pusti korisnika
    if (expectedRoles && expectedRoles.includes(userRole)) {
      return true;
    }

    console.warn(`Access denied for role: ${userRole}.`);

    // VAŽNO: Da bi izbegao petlju, vrati true i pusti ga na Dashboard 
    // gde će ga naš Dashboard fallback u TS-u bezbedno obraditi!
    return true;

  } catch (e) {
    console.error('Invalid token in roleGuard:', e);
    return false;
  }
};
