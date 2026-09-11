import { provideHttpClient } from '@angular/common/http';
import { HttpTestingController, provideHttpClientTesting } from '@angular/common/http/testing';
import { TestBed } from '@angular/core/testing';
import { provideRouter } from '@angular/router';
import { RouterTestingHarness } from '@angular/router/testing';
import { of } from 'rxjs';
import { routes } from '../../app.routes';
import { AuthService } from '../../core/services/auth.service';
import { ApplicationStatisticsComponent } from './application-statistics-component';

describe('Application statistics route', () => {
  it('loads the statistics page through the real admin route and calls the Application API', async () => {
    TestBed.configureTestingModule({
      providers: [provideRouter(routes), provideHttpClient(), provideHttpClientTesting(),
        { provide: AuthService, useValue: { isLoggedIn: () => true, me: () => of({ roles: ['Admin'] }) } },
      ],
    });
    const harness = await RouterTestingHarness.create();
    await harness.navigateByUrl('/admin/statistics', ApplicationStatisticsComponent);
    const http = TestBed.inject(HttpTestingController);
    http.expectOne('/api/applications/statistics').flush({
      totalCount: 0, from: null, to: null, byStatus: [], dailyTrend: [],
    });
    harness.detectChanges();
    expect(harness.routeNativeElement?.textContent).toContain('Application statistics');
    http.verify();
  });
});
