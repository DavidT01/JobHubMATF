import { TestBed } from '@angular/core/testing';
import { ActivatedRouteSnapshot, Router, RouterStateSnapshot, UrlTree, provideRouter } from '@angular/router';
import { Observable, firstValueFrom, of, throwError } from 'rxjs';
import { AuthService } from '../services/auth.service';
import { applicationRoleGuard } from './application-role.guard';

describe('applicationRoleGuard', () => {
  const auth = { isLoggedIn: () => true, me: () => of({ roles: ['Candidate'] }) };
  beforeEach(() => {
    auth.isLoggedIn = () => true;
    auth.me = () => of({ roles: ['Candidate'] });
    TestBed.configureTestingModule({ providers: [provideRouter([]), { provide: AuthService, useValue: auth }] });
  });
  async function check(roles = ['Candidate']) {
    const result = TestBed.runInInjectionContext(() => applicationRoleGuard(
      { data: { roles } } as unknown as ActivatedRouteSnapshot, {} as RouterStateSnapshot));
    return result instanceof Observable ? firstValueFrom(result) : result;
  }
  it('allows a verified candidate', async () => expect(await check()).toBe(true));
  it('redirects a visitor to login', async () => {
    auth.isLoggedIn = () => false;
    expect(TestBed.inject(Router).serializeUrl(await check() as UrlTree)).toBe('/login');
  });
  it('denies the wrong role and missing role configuration', async () => {
    auth.me = () => of({ roles: ['Employer'] });
    expect(TestBed.inject(Router).serializeUrl(await check() as UrlTree)).toBe('/');
    expect(TestBed.inject(Router).serializeUrl(await check([]) as UrlTree)).toBe('/');
  });
  it('redirects failed identity verification to login', async () => {
    auth.me = () => throwError(() => new Error('Invalid token'));
    expect(TestBed.inject(Router).serializeUrl(await check() as UrlTree)).toBe('/login');
  });
});
