import { provideHttpClient } from '@angular/common/http';
import { HttpTestingController, provideHttpClientTesting } from '@angular/common/http/testing';
import { ComponentFixture, TestBed } from '@angular/core/testing';
import { MatPaginator } from '@angular/material/paginator';
import { By } from '@angular/platform-browser';
import { EmployerApplicationDto } from '../../core/models/employer-application-dto';
import { EmployerApplicationsComponent } from './employer-applications-component';

describe('EmployerApplicationsComponent', () => {
  const job = 'aaaaaaaaaaaaaaaaaaaaaaaa';
  const url = (id = job, page = 1, size = 20) => `/api/applications/jobs/${id}?pageNumber=${page}&pageSize=${size}`;
  const item = (overrides: Partial<EmployerApplicationDto> = {}): EmployerApplicationDto => ({
    id: 'application-1', jobId: job, candidateId: 'candidate-1', candidateName: 'Ana Test',
    coverLetter: 'First line\nSecond line', status: 'InReview',
    submittedAtUtc: '2026-09-02T10:00:00Z', updatedAtUtc: '2026-09-03T10:00:00Z',
    cvStatus: 'Available', currentCvUrl: 'https://profiles.example/uploads/cvs/current.pdf', ...overrides,
  });
  let fixture: ComponentFixture<EmployerApplicationsComponent>;
  let http: HttpTestingController;
  const content = () => fixture.nativeElement.textContent as string;
  const respond = (items = [item()], totalCount = items.length, requestUrl = url()) => {
    http.expectOne(requestUrl).flush({ items, totalCount, pageNumber: 1, pageSize: 20 });
    fixture.detectChanges();
  };
  const click = (label: string) => {
    const buttons = Array.from(fixture.nativeElement.querySelectorAll('button')) as HTMLButtonElement[];
    const button = buttons.find(button => button.textContent?.trim() === label);
    expect(button).toBeDefined();
    button!.click(); fixture.detectChanges();
  };

  beforeEach(async () => {
    await TestBed.configureTestingModule({
      imports: [EmployerApplicationsComponent], providers: [provideHttpClient(), provideHttpClientTesting()],
    }).compileComponents();
    http = TestBed.inject(HttpTestingController);
    fixture = TestBed.createComponent(EmployerApplicationsComponent);
    fixture.componentRef.setInput('jobId', ` ${job.toUpperCase()} `);
    fixture.detectChanges();
  });
  afterEach(() => http.verify());

  it('loads the normalized job, shows read-only details and a safe current CV link', () => {
    expect(content()).toContain('Loading applications');
    respond();
    expect(content()).toContain('Ana Test');
    expect(content()).toContain('candidate-1');
    expect(content()).toContain('Status: In review');
    expect(content()).toContain('First line\nSecond line');
    expect(fixture.nativeElement.querySelectorAll('time').length).toBe(2);
    const link = fixture.nativeElement.querySelector('a') as HTMLAnchorElement;
    expect(link.href).toBe(item().currentCvUrl);
    expect(link.target).toBe('_blank');
    expect(link.rel).toBe('noopener noreferrer');
    expect(fixture.nativeElement.querySelector('mat-card mat-select')).toBeNull();
  });

  it('renders all CV absence states and null details without fabricated links', () => {
    respond([
      item({ id: '1', cvStatus: 'Missing', candidateName: null, coverLetter: null }),
      item({ id: '2', cvStatus: 'ProfileMissing' }),
      item({ id: '3', cvStatus: 'ProfileReferenceMissing' }),
    ]);
    for (const text of ['no current CV', 'no longer available', 'no profile reference', 'Candidate profile', 'No cover letter provided']) {
      expect(content()).toContain(text);
    }
    expect(fixture.nativeElement.querySelector('a')).toBeNull();
  });

  it.each(['javascript:alert(1)', 'data:text/html,test', '/uploads/cvs/a.pdf', 'https://user:pass@example.com/a.pdf', 'invalid', null])(
    'does not render unsafe or missing CV URL %s', currentCvUrl => {
      respond([item({ currentCvUrl })]);
      expect(fixture.nativeElement.querySelector('a')).toBeNull();
      expect(content()).toContain('current CV link is unavailable');
    });

  it('escapes candidate and letter content instead of rendering HTML', () => {
    respond([item({ candidateName: '<img src=x onerror=alert(1)>', coverLetter: '<script>alert(1)</script>' })]);
    expect(content()).toContain('<script>alert(1)</script>');
    expect(fixture.nativeElement.querySelector('img, script')).toBeNull();
  });

  it('distinguishes the empty state and allows a refresh for a changed current CV', () => {
    respond([]);
    expect(content()).toContain('No applications for this job yet.');
    expect(fixture.nativeElement.querySelector('mat-paginator')).toBeNull();
    click('Refresh applications');
    respond([item({ currentCvUrl: 'http://localhost:5213/uploads/cvs/new.pdf' })]);
    expect(fixture.nativeElement.querySelector('a').href).toContain('new.pdf');
  });

  it.each([
    [400, 'selection is invalid', false], [401, 'sign in again', false],
    [403, 'do not have permission', false], [404, 'could not be found', false],
    [503, 'temporarily unavailable', true], [500, 'could not be loaded', true],
  ] as const)('handles %s without exposing server details or a false empty result', (status, message, retry) => {
    http.expectOne(url()).flush({ detail: 'PRIVATE DETAIL' }, { status, statusText: 'Error' });
    fixture.detectChanges();
    expect(content()).toContain(message);
    expect(content()).not.toContain('PRIVATE DETAIL');
    expect(content()).not.toContain('No applications');
    expect(content().includes('Try again')).toBe(retry);
    expect(fixture.nativeElement.querySelector('[role="alert"]')).not.toBeNull();
  });

  it('recovers from a network error on explicit retry', () => {
    http.expectOne(url()).error(new ProgressEvent('error')); fixture.detectChanges();
    expect(content()).toContain('Check your connection');
    click('Try again'); respond();
    expect(content()).toContain('Ana Test');
  });

  it('paginates and recovers from an empty later page', () => {
    respond([item()], 21);
    const paginator = fixture.debugElement.query(By.directive(MatPaginator)).componentInstance as MatPaginator;
    paginator.page.emit({ pageIndex: 1, pageSize: 20, length: 21 });
    fixture.detectChanges();
    expect(fixture.nativeElement.querySelector('mat-card')).toBeNull();
    respond([], 1, url(job, 2));
    expect(content()).toContain('no applications on this page');
    click('Back to first page'); respond();
  });

  it('resets pagination and cancels the previous job when the input changes', () => {
    respond([item()], 100);
    const paginator = fixture.debugElement.query(By.directive(MatPaginator)).componentInstance as MatPaginator;
    paginator.page.emit({ pageIndex: 1, pageSize: 50, length: 100 });
    const old = http.expectOne(url(job, 2, 50));
    const nextJob = 'bbbbbbbbbbbbbbbbbbbbbbbb';
    fixture.componentRef.setInput('jobId', nextJob); fixture.detectChanges();
    expect(old.cancelled).toBe(true);
    respond([], 0, url(nextJob));
    expect(content()).not.toContain('Ana Test');
  });

  it('cancels old work and sends no request for an invalid job', () => {
    const old = http.expectOne(url());
    fixture.componentRef.setInput('jobId', '../invalid'); fixture.detectChanges();
    expect(old.cancelled).toBe(true);
    expect(content()).toContain('Select a valid job');
    http.expectNone(() => true);
  });

  it('cancels pending work on destruction', () => {
    const request = http.expectOne(url()); fixture.destroy(); expect(request.cancelled).toBe(true);
  });
});
