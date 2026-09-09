import { TestBed } from '@angular/core/testing';
import { provideHttpClient } from '@angular/common/http';
import { HttpTestingController, provideHttpClientTesting } from '@angular/common/http/testing';
import { AuthService } from './auth.service';

describe('AuthService', () => {
  let service: AuthService;
  let http: HttpTestingController;

  beforeEach(() => {
    localStorage.clear();
    TestBed.configureTestingModule({
      providers: [provideHttpClient(), provideHttpClientTesting()]
    });
    service = TestBed.inject(AuthService);
    http = TestBed.inject(HttpTestingController);
  });

  afterEach(() => {
    http.verify();
    localStorage.clear();
  });

  it('stores the JWT on login', () => {
    service.login({ email: 'ana@example.com', password: 'Pass123!' }).subscribe();

    const req = http.expectOne('http://localhost:5283/api/auth/login');
    expect(req.request.method).toBe('POST');
    req.flush({ token: 'jwt-token', expiration: '2026-01-01' });

    expect(service.getToken()).toBe('jwt-token');
    expect(service.isLoggedIn()).toBe(true);
  });

  it('clears the JWT on logout', () => {
    localStorage.setItem('auth_token', 'jwt-token');
    service.logout();
    expect(service.isLoggedIn()).toBe(false);
  });
});
