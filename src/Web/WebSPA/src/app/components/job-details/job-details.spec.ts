import { provideHttpClient } from '@angular/common/http';
import { provideHttpClientTesting, HttpTestingController } from '@angular/common/http/testing';
import { ComponentFixture, TestBed } from '@angular/core/testing';
import { ActivatedRoute, convertToParamMap, provideRouter } from '@angular/router';
import { of, throwError } from 'rxjs';
import { JobDetails } from './job-details';
import { JobService } from '../../services/job.service';
import { AuthService } from '../../core/services/auth.service';
import { CompanyProfileService } from '../../core/services/company-profile/company-profile-service';

describe('JobDetails application integration', () => {
  let fixture: ComponentFixture<JobDetails>;
  let http: HttpTestingController;
  const jobId = 'aaaaaaaaaaaaaaaaaaaaaaaa';
  const auth = { isLoggedIn: () => true, me: () => of({ roles: ['Candidate'] }) };
  let job: Record<string, unknown>;
  beforeEach(async () => {
    job = { id: jobId, title: 'Developer', isActive: true, skills: [], requirements: [], responsibilities: [] };
    auth.isLoggedIn = () => true;
    auth.me = () => of({ roles: ['Candidate'] });
    await TestBed.configureTestingModule({
      imports: [JobDetails],
      providers: [provideRouter([]), provideHttpClient(), provideHttpClientTesting(),
        { provide: ActivatedRoute, useValue: { snapshot: { paramMap: convertToParamMap({ id: jobId }) } } },
        { provide: AuthService, useValue: auth },
        { provide: JobService, useValue: { getById: () => of(job), getBookmarks: () => of([]) } },
        { provide: CompanyProfileService, useValue: { getMine: () => of({ id: 'company-1' }) } },
      ],
    }).compileComponents();
    http = TestBed.inject(HttpTestingController);
  });
  afterEach(() => http.verify());
  function render() {
    fixture = TestBed.createComponent(JobDetails);
    fixture.detectChanges();
    return fixture.nativeElement as HTMLElement;
  }
  it('submits the displayed job and links to my applications', () => {
    const page = render();
    expect(page.querySelector('a[href="/applications"]')).not.toBeNull();
    expect(page.textContent).toContain('Your current profile CV will be used');
    page.querySelector('form')!.dispatchEvent(new Event('submit', { bubbles: true, cancelable: true }));
    const request = http.expectOne('/api/applications');
    expect(request.request.method).toBe('POST');
    expect(request.request.body).toEqual({ jobId, coverLetter: null });
    request.flush({ id: 'application-1', jobId, status: 'Submitted' });
  });
  it('does not show the form for an employer', () => {
    auth.me = () => of({ roles: ['Employer'] });
    expect(render().querySelector('app-application-form')).toBeNull();
  });
  it('gives the owning employer management links instead of candidate actions', () => {
    auth.me = () => of({ roles: ['Employer'] });
    job['companyId'] = 'company-1';
    const page = render();
    expect(page.querySelector(`a[href="/recruitment-processes/${jobId}"]`)).not.toBeNull();
    expect(page.querySelector(`a[href="/jobs/${jobId}/edit"]`)).not.toBeNull();
    expect(page.textContent).toContain('Delete job');
    expect(page.textContent).not.toContain('Check match');
  });
  it('shows no management links to an employer who does not own the job', () => {
    auth.me = () => of({ roles: ['Employer'] });
    job['companyId'] = 'another-company';
    const page = render();
    expect(page.querySelector(`a[href="/jobs/${jobId}/edit"]`)).toBeNull();
    expect(page.textContent).not.toContain('Delete job');
  });
  it('asks a visitor to log in', () => {
    auth.isLoggedIn = () => false;
    const page = render();
    expect(page.querySelector('app-application-form')).toBeNull();
    expect(page.querySelector('a[href="/login"]')).not.toBeNull();
  });
  it('does not show the form when identity verification fails', () => {
    auth.me = () => throwError(() => new Error('Unavailable'));
    expect(render().querySelector('app-application-form')).toBeNull();
  });
  it('does not allow applying to inactive or expired jobs', () => {
    job['isActive'] = false;
    const page = render();
    expect(page.querySelector('app-application-form')).toBeNull();
    job['isActive'] = true;
    job['expirationDate'] = '2020-01-01T00:00:00Z';
    fixture.detectChanges();
    expect(page.querySelector('app-application-form')).toBeNull();
    expect(page.textContent).toContain('no longer accepting applications');
  });
});
