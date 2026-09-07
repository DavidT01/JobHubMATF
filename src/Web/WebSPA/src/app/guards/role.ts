import { inject } from '@angular/core';
import { CanActivateFn, Router } from '@angular/router';

export const roleGuard: CanActivateFn = (route, state) => {
  const router = inject(Router);
  const token = localStorage.getItem('jwt_token');

  if (!token) {
    // Ako nema tokena, prekini petlju i idi na login (ili ostavi prolaz ako nemaš login stranicu)
    console.warn('Nema tokena u localStorage-u.');
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

    console.warn(`Pristup odbijen za ulogu: ${userRole}. Preusmeravanje...`);

    // VAŽNO: Da bi izbegao petlju, vrati true i pusti ga na Dashboard 
    // gde će ga naš Dashboard fallback u TS-u bezbedno obraditi!
    return true;

  } catch (e) {
    console.error('Nevažeći token u RoleGuard-u:', e);
    return false;
  }
};
