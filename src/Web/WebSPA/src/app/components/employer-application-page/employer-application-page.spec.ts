import { provideHttpClient } from '@angular/common/http';
import { HttpTestingController, provideHttpClientTesting } from '@angular/common/http/testing';
import { ComponentFixture, TestBed } from '@angular/core/testing';
import { provideRouter } from '@angular/router';
import { By } from '@angular/platform-browser';
import { MatSelect } from '@angular/material/select';
import { of } from 'rxjs';
import { EmployerApplicationPage } from './employer-application-page';
import { AuthService } from '../../core/services/auth.service';
import { JobService } from '../../services/job.service';

describe('EmployerApplicationPage', () => {
  let fixture: ComponentFixture<EmployerApplicationPage>;
  let http: HttpTestingController;
  const first = { id: 'aaaaaaaaaaaaaaaaaaaaaaaa', companyId: 'company-profile', title: 'First', isActive: true };
  const second = { ...first, id: 'bbbbbbbbbbbbbbbbbbbbbbbb', title: 'Second', isActive: false };
  const getByCompanyId = vi.fn(() => of([first, second]));
  beforeEach(async () => {
    getByCompanyId.mockReset().mockReturnValue(of([first, second]));
    await TestBed.configureTestingModule({
      imports: [EmployerApplicationPage],
      providers: [provideRouter([]), provideHttpClient(), provideHttpClientTesting(),
        { provide: AuthService, useValue: { me: () => of({ id: 'identity-user', roles: ['Employer'] }) } },
        { provide: JobService, useValue: { getByCompanyId } },
      ],
    }).compileComponents();
    http = TestBed.inject(HttpTestingController);
    fixture = TestBed.createComponent(EmployerApplicationPage);
    fixture.detectChanges();
  });
  afterEach(() => http.verify());
  function profile() {
    http.expectOne('/api/company-profiles/identity-user').flush({ id: 'company-profile', userId: 'identity-user' });
    fixture.detectChanges();
  }
  function select(id: string) {
    fixture.debugElement.query(By.directive(MatSelect)).componentInstance.selectionChange.emit({ value: id });
    fixture.detectChanges();
  }
  it('uses the company profile ID and loads applications only after a job is selected', () => {
    profile();
    expect(getByCompanyId).toHaveBeenCalledWith('company-profile');
    http.expectNone(request => request.url.startsWith('/api/applications'));
    select(first.id);
    http.expectOne(`/api/applications/jobs/${first.id}?pageNumber=1&pageSize=20`)
      .flush({ items: [], totalCount: 0, pageNumber: 1, pageSize: 20 });
    select(second.id);
    http.expectOne(`/api/applications/jobs/${second.id}?pageNumber=1&pageSize=20`)
      .flush({ items: [], totalCount: 0, pageNumber: 1, pageSize: 20 });
  });
  it('shows an empty state for a company without jobs', () => {
    getByCompanyId.mockReturnValue(of([]));
    profile();
    expect(fixture.nativeElement.textContent).toContain('Firma još nema oglase');
  });
  it('shows a retryable error instead of querying jobs with a user ID', () => {
    http.expectOne('/api/company-profiles/identity-user').flush({}, { status: 404, statusText: 'Not Found' });
    fixture.detectChanges();
    expect(fixture.nativeElement.querySelector('[role="alert"]')).not.toBeNull();
    expect(getByCompanyId).not.toHaveBeenCalled();
  });
  it('excludes jobs belonging to a different company', () => {
    getByCompanyId.mockReturnValue(of([{ ...first, companyId: 'another-company' }]));
    profile();
    expect(fixture.nativeElement.textContent).toContain('Firma još nema oglase');
  });
});
