import { HttpErrorResponse, provideHttpClient, withInterceptors } from '@angular/common/http';
import { HttpTestingController, provideHttpClientTesting } from '@angular/common/http/testing';
import { TestBed } from '@angular/core/testing';

import { ApplicationListItemDto, PagedResult } from '../../models/application-list-item-dto';
import { APPLICATIONS_API_URL, ApplicationsService } from './applications-service';

describe('ApplicationsService', () => {
  let service: ApplicationsService;
  let http: HttpTestingController;

  beforeEach(() => {
    TestBed.configureTestingModule({
      providers: [provideHttpClient(), provideHttpClientTesting()],
    });
    service = TestBed.inject(ApplicationsService);
    http = TestBed.inject(HttpTestingController);
  });

  afterEach(() => http.verify());

  it('requests the current candidate with default pagination and no supplied identity', () => {
    const response: PagedResult<ApplicationListItemDto> = {
      items: [{
        id: '11111111-1111-1111-1111-111111111111',
        jobId: 'aaaaaaaaaaaaaaaaaaaaaaaa',
        status: 'Submitted',
        submittedAtUtc: '2026-09-02T10:00:00+00:00',
        updatedAtUtc: '2026-09-02T10:00:00+00:00',
      }],
      totalCount: 1,
      pageNumber: 1,
      pageSize: 20,
    };
    let actual: PagedResult<ApplicationListItemDto> | undefined;
    service.getMyApplications().subscribe(result => actual = result);

    const request = http.expectOne('/api/applications/me?pageNumber=1&pageSize=20');
    expect(request.request.method).toBe('GET');
    expect(request.request.body).toBeNull();
    expect(request.request.params.keys()).toEqual(['pageNumber', 'pageSize']);
    expect(request.request.headers.has('Authorization')).toBe(false);
    request.flush(response);
    expect(actual).toEqual(response);
  });

  it('passes custom pagination and preserves an empty page with its total count', () => {
    const response = { items: [], totalCount: 21, pageNumber: 4, pageSize: 10 };
    let actual: PagedResult<ApplicationListItemDto> | undefined;
    service.getMyApplications(4, 10).subscribe(result => actual = result);

    http.expectOne('/api/applications/me?pageNumber=4&pageSize=10').flush(response);
    expect(actual).toEqual(response);
  });

  it.each([400, 401, 403, 404, 503])('preserves HTTP %s and ProblemDetails for the view', status => {
    const problem = {
      status,
      title: 'Request failed.',
      traceId: 'test-trace',
      ...(status === 400 ? { errors: { pageNumber: ['Page number must be at least 1.'] } } : {}),
    };
    let actual: HttpErrorResponse | undefined;
    service.getMyApplications().subscribe({
      next: () => { throw new Error('An HTTP failure must not become an empty success.'); },
      error: error => actual = error,
    });

    http.expectOne('/api/applications/me?pageNumber=1&pageSize=20').flush(problem, {
      status,
      statusText: 'Request failed',
    });
    expect(actual?.status).toBe(status);
    expect(actual?.error).toEqual(problem);
  });
});

describe('ApplicationsService configuration', () => {
  it('supports a configured URL and the shared HTTP interceptor pipeline', () => {
    TestBed.configureTestingModule({
      providers: [
        { provide: APPLICATIONS_API_URL, useValue: '/deployment/applications/' },
        provideHttpClient(withInterceptors([
          (request, next) => next(request.clone({ setHeaders: { 'X-Test-Pipeline': 'passed' } })),
        ])),
        provideHttpClientTesting(),
      ],
    });
    const http = TestBed.inject(HttpTestingController);
    TestBed.inject(ApplicationsService).getMyApplications().subscribe();

    const request = http.expectOne('/deployment/applications/me?pageNumber=1&pageSize=20');
    expect(request.request.headers.get('X-Test-Pipeline')).toBe('passed');
    request.flush({ items: [], totalCount: 0, pageNumber: 1, pageSize: 20 });
    http.verify();
  });
});
