import { ComponentFixture, TestBed } from '@angular/core/testing';
import { provideHttpClient } from '@angular/common/http';
import { provideHttpClientTesting } from '@angular/common/http/testing';
import { HttpErrorResponse } from '@angular/common/http';
import { ActivatedRoute, convertToParamMap, provideRouter, Router } from '@angular/router';
import { Observable, of, throwError } from 'rxjs';

import { JobCreate } from './job-create';
import { JobService } from '../../services/job.service';
import { CompanyProfileService } from '../../core/services/company-profile/company-profile-service';

describe('JobCreate', () => {
  const company = { id: 'company-1', companyName: 'Nordlight Software' };
  let getMine: () => Observable<unknown>;
  let routeParams: Record<string, string>;
  let jobService: { getById: ReturnType<typeof vi.fn>; create: ReturnType<typeof vi.fn>; update: ReturnType<typeof vi.fn> };

  async function render(beforeCreate?: () => void): Promise<ComponentFixture<JobCreate>> {
    await TestBed.configureTestingModule({
      imports: [JobCreate],
      providers: [
        provideRouter([]), provideHttpClient(), provideHttpClientTesting(),
        { provide: ActivatedRoute, useValue: { snapshot: { paramMap: convertToParamMap(routeParams) } } },
        { provide: CompanyProfileService, useValue: { getMine: () => getMine() } },
        { provide: JobService, useValue: jobService },
      ],
    }).compileComponents();
    beforeCreate?.();
    const fixture = TestBed.createComponent(JobCreate);
    fixture.detectChanges();
    await fixture.whenStable();
    return fixture;
  }

  beforeEach(() => {
    getMine = () => of(company);
    routeParams = {};
    jobService = { getById: vi.fn(), create: vi.fn(), update: vi.fn() };
  });

  it("posts under the signed-in employer's company without asking for its id", async () => {
    const fixture = await render();
    const page = fixture.nativeElement as HTMLElement;
    expect(page.textContent).not.toContain('Company ID');
    expect(page.textContent).toContain('Posting as Nordlight Software');
    expect(fixture.componentInstance.form.value.companyId).toBe('company-1');
  });

  it('asks for a company profile first when the employer has none', async () => {
    getMine = () => throwError(() => new HttpErrorResponse({ status: 404 }));
    const page = (await render()).nativeElement as HTMLElement;
    expect(page.querySelector('form')).toBeNull();
    expect(page.querySelector('a[href="/profile/company/me"]')).not.toBeNull();
  });

  it('refuses to edit a job that belongs to another company', async () => {
    routeParams = { id: 'job-1' };
    jobService.getById.mockReturnValue(of({ id: 'job-1', companyId: 'another-company' }));
    let navigate!: ReturnType<typeof vi.spyOn>;
    await render(() => {
      navigate = vi.spyOn(TestBed.inject(Router), 'navigate').mockResolvedValue(true);
    });
    expect(navigate).toHaveBeenCalledWith(['/jobs', 'job-1']);
  });
});
