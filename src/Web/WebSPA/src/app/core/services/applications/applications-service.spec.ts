import { HttpErrorResponse, provideHttpClient, withInterceptors } from '@angular/common/http';
import { HttpTestingController, provideHttpClientTesting } from '@angular/common/http/testing';
import { TestBed } from '@angular/core/testing';

import { ApplicationListItemDto, PagedResult } from '../../models/application-list-item-dto';
import { CurrentCvStatus, EmployerApplicationDto } from '../../models/employer-application-dto';
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

  it('submits only the job and cover letter and returns the created application', () => {
    const input = {
      jobId: 'aaaaaaaaaaaaaaaaaaaaaaaa', coverLetter: 'My cover letter',
      candidateId: 'must-not-be-sent', companyId: 'must-not-be-sent', cvUrl: 'must-not-be-sent',
    };
    const response: ApplicationListItemDto = {
      id: '11111111-1111-1111-1111-111111111111', jobId: input.jobId, status: 'Submitted',
      submittedAtUtc: '2026-09-03T12:00:00Z', updatedAtUtc: '2026-09-03T12:00:00Z',
    };
    let actual: ApplicationListItemDto | undefined;
    service.submitApplication(input).subscribe(result => actual = result);

    const request = http.expectOne('/api/applications');
    expect(request.request.method).toBe('POST');
    expect(request.request.body).toEqual({ jobId: input.jobId, coverLetter: input.coverLetter });
    expect(request.request.headers.has('Authorization')).toBe(false);
    request.flush(response, { status: 201, statusText: 'Created' });
    expect(actual).toEqual(response);
  });

  it('sends null when the optional cover letter is absent', () => {
    service.submitApplication({ jobId: 'aaaaaaaaaaaaaaaaaaaaaaaa' }).subscribe();
    const request = http.expectOne('/api/applications');
    expect(request.request.body).toEqual({ jobId: 'aaaaaaaaaaaaaaaaaaaaaaaa', coverLetter: null });
    request.flush({});
  });

  it.each([400, 401, 403, 404, 409, 503])('preserves submission HTTP %s without retrying', status => {
    const problem = { status, title: 'Submission failed', errors: { cv: ['Upload a CV first.'] } };
    let actual: HttpErrorResponse | undefined;
    service.submitApplication({ jobId: 'aaaaaaaaaaaaaaaaaaaaaaaa' }).subscribe({
      next: () => { throw new Error('A failed submission must not succeed.'); },
      error: error => actual = error,
    });
    http.expectOne('/api/applications').flush(problem, { status, statusText: 'Error' });
    expect(actual?.status).toBe(status);
    expect(actual?.error).toEqual(problem);
    http.expectNone('/api/applications');
  });

  it('does not retry a submission after a network failure with an uncertain outcome', () => {
    let actual: HttpErrorResponse | undefined;
    service.submitApplication({ jobId: 'aaaaaaaaaaaaaaaaaaaaaaaa' }).subscribe({
      error: error => actual = error,
    });
    http.expectOne('/api/applications').error(new ProgressEvent('error'));
    expect(actual?.status).toBe(0);
    http.expectNone('/api/applications');
  });

  it('requests employer applications by job and preserves all CV availability states', () => {
    const cvStates: CurrentCvStatus[] = ['Available', 'Missing', 'ProfileMissing', 'ProfileReferenceMissing'];
    const response: PagedResult<EmployerApplicationDto> = {
      items: cvStates.map((cvStatus, index) => ({
        id: `application-${index}`, jobId: 'aaaaaaaaaaaaaaaaaaaaaaaa', candidateId: `candidate-${index}`,
        candidateName: index === 0 ? 'Test Candidate' : null, coverLetter: index === 0 ? 'Hello' : null,
        status: 'Submitted', submittedAtUtc: '2026-09-03T12:00:00Z', updatedAtUtc: '2026-09-03T12:00:00Z',
        currentCvUrl: index === 0 ? 'https://profiles.example.test/uploads/cvs/current.pdf' : null, cvStatus,
      })),
      totalCount: 4, pageNumber: 1, pageSize: 20,
    };
    let actual: PagedResult<EmployerApplicationDto> | undefined;
    service.getForJob('aaaaaaaaaaaaaaaaaaaaaaaa').subscribe(result => actual = result);
    const request = http.expectOne('/api/applications/jobs/aaaaaaaaaaaaaaaaaaaaaaaa?pageNumber=1&pageSize=20');
    expect(request.request.method).toBe('GET');
    expect(request.request.params.keys()).toEqual(['pageNumber', 'pageSize']);
    expect(request.request.body).toBeNull();
    request.flush(response);
    expect(actual).toEqual(response);
  });

  it('encodes the job as one path segment and passes custom pagination', () => {
    service.getForJob('not/a?valid#id', 3, 10).subscribe();
    http.expectOne('/api/applications/jobs/not%2Fa%3Fvalid%23id?pageNumber=3&pageSize=10')
      .flush({ items: [], totalCount: 0, pageNumber: 3, pageSize: 10 });
  });

  it.each([400, 401, 403, 404, 503])('preserves employer query HTTP %s', status => {
    let actual: HttpErrorResponse | undefined;
    service.getForJob('aaaaaaaaaaaaaaaaaaaaaaaa').subscribe({
      next: () => { throw new Error('A failed query must not become an empty success.'); },
      error: error => actual = error,
    });
    const problem = { status, title: 'Query failed', traceId: 'test-trace' };
    http.expectOne('/api/applications/jobs/aaaaaaaaaaaaaaaaaaaaaaaa?pageNumber=1&pageSize=20')
      .flush(problem, { status, statusText: 'Error' });
    expect(actual?.status).toBe(status);
    expect(actual?.error).toEqual(problem);
  });

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
    const service = TestBed.inject(ApplicationsService);
    service.submitApplication({ jobId: 'aaaaaaaaaaaaaaaaaaaaaaaa' }).subscribe();
    const post = http.expectOne('/deployment/applications');
    expect(post.request.headers.get('X-Test-Pipeline')).toBe('passed');
    post.flush({});
    service.getForJob('aaaaaaaaaaaaaaaaaaaaaaaa').subscribe();
    const employer = http.expectOne('/deployment/applications/jobs/aaaaaaaaaaaaaaaaaaaaaaaa?pageNumber=1&pageSize=20');
    expect(employer.request.headers.get('X-Test-Pipeline')).toBe('passed');
    employer.flush({ items: [], totalCount: 0, pageNumber: 1, pageSize: 20 });
    http.verify();
  });
});
