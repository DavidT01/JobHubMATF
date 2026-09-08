import { provideHttpClient } from '@angular/common/http';
import { HttpTestingController, provideHttpClientTesting } from '@angular/common/http/testing';
import { ComponentFixture, TestBed } from '@angular/core/testing';

import { ApplicationStatisticsDto } from '../../core/models/application-management-dto';
import { ApplicationStatisticsComponent } from './application-statistics-component';

describe('ApplicationStatisticsComponent', () => {
  const response: ApplicationStatisticsDto = {
    totalCount: 4,
    from: null,
    to: null,
    byStatus: [
      { status: 'Submitted', count: 2, ratePercent: 50 },
      { status: 'InReview', count: 1, ratePercent: 25 },
      { status: 'Interview', count: 0, ratePercent: 0 },
      { status: 'Rejected', count: 0, ratePercent: 0 },
      { status: 'Accepted', count: 1, ratePercent: 25 },
    ],
    dailyTrend: [
      { date: '2026-09-01', count: 1 },
      { date: '2026-09-02', count: 3 },
    ],
  };
  let fixture: ComponentFixture<ApplicationStatisticsComponent>;
  let http: HttpTestingController;
  const content = () => fixture.nativeElement.textContent as string;
  const click = (label: string) => {
    const buttons = Array.from(fixture.nativeElement.querySelectorAll('button')) as HTMLButtonElement[];
    const button = buttons.find(item => item.textContent?.trim() === label);
    expect(button).toBeDefined();
    button!.click();
    fixture.detectChanges();
  };
  const setDate = (index: number, value: string) => {
    const input = fixture.nativeElement.querySelectorAll('input')[index] as HTMLInputElement;
    input.value = value;
    input.dispatchEvent(new Event('input'));
    fixture.detectChanges();
  };

  beforeEach(async () => {
    await TestBed.configureTestingModule({
      imports: [ApplicationStatisticsComponent],
      providers: [provideHttpClient(), provideHttpClientTesting()],
    }).compileComponents();
    http = TestBed.inject(HttpTestingController);
    fixture = TestBed.createComponent(ApplicationStatisticsComponent);
    fixture.detectChanges();
  });

  afterEach(() => http.verify());

  it('shows totals, every status rate and an accessible daily trend', () => {
    expect(content()).toContain('Loading application statistics');
    http.expectOne('/api/applications/statistics').flush(response);
    fixture.detectChanges();

    expect(content()).toContain('Total applications');
    expect(content()).toContain('Submitted');
    expect(content()).toContain('In review');
    expect(content()).toContain('50%');
    expect(fixture.nativeElement.querySelectorAll('.status-grid mat-card').length).toBe(5);
    expect(fixture.nativeElement.querySelectorAll('.trend li').length).toBe(2);
    expect(fixture.nativeElement.querySelector('time').getAttribute('datetime')).toBe('2026-09-01');
    expect(content()).toContain('2026-09-02');
    expect(content()).toContain('3');
  });

  it('applies and clears an inclusive date period', () => {
    http.expectOne('/api/applications/statistics').flush(response);
    fixture.detectChanges();
    setDate(0, '2026-09-01');
    setDate(1, '2026-09-30');
    click('Apply period');
    http.expectOne('/api/applications/statistics?from=2026-09-01&to=2026-09-30')
      .flush({ ...response, from: '2026-09-01', to: '2026-09-30' });
    fixture.detectChanges();

    click('Clear');
    http.expectOne('/api/applications/statistics').flush(response);
    fixture.detectChanges();
    const inputs = Array.from(fixture.nativeElement.querySelectorAll('input')) as HTMLInputElement[];
    expect(inputs.map(input => input.value)).toEqual(['', '']);
  });

  it('rejects an inverted period before sending a request', () => {
    http.expectOne('/api/applications/statistics').flush(response);
    fixture.detectChanges();
    setDate(0, '2026-09-30');
    setDate(1, '2026-09-01');
    click('Apply period');

    expect(content()).toContain('From date must be earlier');
    expect(fixture.nativeElement.querySelector('[role="alert"]')).not.toBeNull();
    http.expectNone(request => request.url === '/api/applications/statistics');
  });

  it('renders a distinct empty period without a misleading trend', () => {
    http.expectOne('/api/applications/statistics').flush({
      ...response,
      totalCount: 0,
      byStatus: response.byStatus.map(status => ({ ...status, count: 0, ratePercent: 0 })),
      dailyTrend: [],
    });
    fixture.detectChanges();

    expect(content()).toContain('No applications were submitted in the selected period.');
    expect(fixture.nativeElement.querySelector('.trend')).toBeNull();
    expect(fixture.nativeElement.querySelectorAll('.status-grid mat-card').length).toBe(5);
  });

  it('shows safe authorization and retryable service errors', () => {
    http.expectOne('/api/applications/statistics').flush(
      { detail: 'PRIVATE DETAIL' }, { status: 503, statusText: 'Unavailable' },
    );
    fixture.detectChanges();
    expect(content()).toContain('temporarily unavailable');
    expect(content()).not.toContain('PRIVATE DETAIL');
    click('Try again');
    http.expectOne('/api/applications/statistics').flush(response);
    fixture.detectChanges();
    expect(content()).toContain('Total applications');
  });
});
