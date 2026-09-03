import { provideHttpClient } from '@angular/common/http';
import { HttpTestingController, provideHttpClientTesting } from '@angular/common/http/testing';
import { ComponentFixture, TestBed } from '@angular/core/testing';
import { By } from '@angular/platform-browser';
import { MatPaginator } from '@angular/material/paginator';

import { ApplicationListItemDto, ApplicationStatus } from '../../core/models/application-list-item-dto';
import { CandidateApplicationsComponent } from './candidate-applications-component';

describe('CandidateApplicationsComponent', () => {
  let fixture: ComponentFixture<CandidateApplicationsComponent>;
  let http: HttpTestingController;
  const firstUrl = '/api/applications/me?pageNumber=1&pageSize=20';
  const item = (status: ApplicationStatus = 'Submitted', id = 'application-1'): ApplicationListItemDto => ({
    id, jobId: 'aaaaaaaaaaaaaaaaaaaaaaaa', status,
    submittedAtUtc: '2026-09-02T10:00:00Z', updatedAtUtc: '2026-09-02T11:00:00Z',
  });
  const content = () => fixture.nativeElement.textContent as string;
  const clickButton = (label: string) => {
    const buttons = Array.from(fixture.nativeElement.querySelectorAll('button')) as HTMLButtonElement[];
    const button = buttons.find(button => button.textContent?.trim() === label);
    expect(button).toBeDefined();
    button!.click();
    fixture.detectChanges();
  };

  beforeEach(async () => {
    await TestBed.configureTestingModule({
      imports: [CandidateApplicationsComponent],
      providers: [provideHttpClient(), provideHttpClientTesting()],
    }).compileComponents();
    http = TestBed.inject(HttpTestingController);
    fixture = TestBed.createComponent(CandidateApplicationsComponent);
    fixture.detectChanges();
  });

  afterEach(() => http.verify());

  it('shows loading, then real job IDs, dates and all five read-only status labels', () => {
    expect(content()).toContain('Loading applications');
    expect(fixture.nativeElement.querySelector('section').getAttribute('aria-busy')).toBe('true');
    const statuses: ApplicationStatus[] = ['Submitted', 'InReview', 'Interview', 'Rejected', 'Accepted'];
    http.expectOne(firstUrl).flush({
      items: statuses.map((status, index) => item(status, `application-${index}`)),
      totalCount: 5, pageNumber: 1, pageSize: 20,
    });
    fixture.detectChanges();
    expect(content()).toContain('5 applications in total');
    expect(content()).toContain('Job ID: aaaaaaaaaaaaaaaaaaaaaaaa');
    for (const label of ['Submitted', 'In review', 'Interview', 'Rejected', 'Accepted']) {
      expect(content()).toContain(`Status: ${label}`);
    }
    expect(fixture.nativeElement.querySelectorAll('mat-card').length).toBe(5);
    expect(fixture.nativeElement.querySelector('time').getAttribute('datetime')).toBe(item().submittedAtUtc);
    expect(fixture.nativeElement.querySelector('section').getAttribute('aria-busy')).toBe('false');
    expect(fixture.nativeElement.querySelector('select')).toBeNull();
  });

  it('distinguishes no applications from an error', () => {
    http.expectOne(firstUrl).flush({ items: [], totalCount: 0, pageNumber: 1, pageSize: 20 });
    fixture.detectChanges();
    expect(content()).toContain('You have not submitted any applications yet.');
    expect(fixture.nativeElement.querySelector('[role="alert"]')).toBeNull();
    expect(fixture.nativeElement.querySelector('mat-paginator')).toBeNull();
  });

  it.each([
    [401, 'Please sign in again', false],
    [403, 'only available to candidates', false],
    [404, 'candidate profile could not be found', true],
    [503, 'temporarily unavailable', true],
    [500, 'could not be loaded', true],
  ] as const)('handles HTTP %s without exposing internal details', (status, message, retry) => {
    http.expectOne(firstUrl).flush({ detail: 'PRIVATE INTERNAL DETAIL' }, { status, statusText: 'Error' });
    fixture.detectChanges();
    expect(fixture.nativeElement.querySelector('[role="alert"]')).not.toBeNull();
    expect(content()).toContain(message);
    expect(content()).not.toContain('PRIVATE INTERNAL DETAIL');
    expect(content().includes('Try again')).toBe(retry);
    expect(content()).not.toContain('not submitted');
  });

  it('retries a network failure and recovers', () => {
    http.expectOne(firstUrl).error(new ProgressEvent('error'));
    fixture.detectChanges();
    expect(content()).toContain('Check your connection');
    clickButton('Try again');
    expect(content()).toContain('Loading applications');
    http.expectOne(firstUrl).flush({ items: [item()], totalCount: 1, pageNumber: 1, pageSize: 20 });
    fixture.detectChanges();
    expect(content()).toContain('1 application in total');
    expect(fixture.nativeElement.querySelector('[role="alert"]')).toBeNull();
  });

  it('converts paginator indexes to API pages and recovers from an empty later page', () => {
    http.expectOne(firstUrl).flush({ items: [item()], totalCount: 21, pageNumber: 1, pageSize: 20 });
    fixture.detectChanges();
    const paginator = fixture.debugElement.query(By.directive(MatPaginator)).componentInstance as MatPaginator;
    expect(paginator.length).toBe(21);
    paginator.page.emit({ pageIndex: 1, pageSize: 20, length: 21 });
    fixture.detectChanges();
    expect(fixture.nativeElement.querySelectorAll('mat-card').length).toBe(0);
    http.expectOne('/api/applications/me?pageNumber=2&pageSize=20').flush({
      items: [], totalCount: 1, pageNumber: 2, pageSize: 20,
    });
    fixture.detectChanges();
    expect(content()).toContain('There are no applications on this page.');
    clickButton('Back to first page');
    http.expectOne(firstUrl).flush({ items: [item()], totalCount: 1, pageNumber: 1, pageSize: 20 });
    fixture.detectChanges();
    expect(content()).toContain('Job ID:');
  });

  it('cancels an older page request and keeps only the newest page size', () => {
    http.expectOne(firstUrl).flush({ items: [item()], totalCount: 100, pageNumber: 1, pageSize: 20 });
    fixture.detectChanges();
    const paginator = fixture.debugElement.query(By.directive(MatPaginator)).componentInstance as MatPaginator;
    paginator.page.emit({ pageIndex: 1, pageSize: 20, length: 100 });
    const oldRequest = http.expectOne('/api/applications/me?pageNumber=2&pageSize=20');
    paginator.page.emit({ pageIndex: 0, pageSize: 50, length: 100 });
    expect(oldRequest.cancelled).toBe(true);
    http.expectOne('/api/applications/me?pageNumber=1&pageSize=50').flush({
      items: [item('Accepted')], totalCount: 100, pageNumber: 1, pageSize: 50,
    });
    fixture.detectChanges();
    expect(content()).toContain('Status: Accepted');
    const current = fixture.debugElement.query(By.directive(MatPaginator)).componentInstance as MatPaginator;
    expect(current.pageSize).toBe(50);
    expect(current.pageIndex).toBe(0);
  });

  it('cancels pending HTTP work when destroyed', () => {
    const request = http.expectOne(firstUrl);
    fixture.destroy();
    expect(request.cancelled).toBe(true);
  });
});
