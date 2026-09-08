import { provideHttpClient } from '@angular/common/http';
import { HttpTestingController, provideHttpClientTesting } from '@angular/common/http/testing';
import { ComponentFixture, TestBed } from '@angular/core/testing';

import { ApplicationFormComponent } from './application-form-component';

describe('ApplicationFormComponent', () => {
  let fixture: ComponentFixture<ApplicationFormComponent>;
  let http: HttpTestingController;
  const jobId = 'aaaaaaaaaaaaaaaaaaaaaaaa';
  const created = {
    id: '11111111-1111-1111-1111-111111111111', jobId, status: 'Submitted',
    submittedAtUtc: '2026-09-03T12:00:00Z', updatedAtUtc: '2026-09-03T12:00:00Z',
  };
  const text = () => fixture.nativeElement.textContent as string;
  const fill = (value: string) => {
    const field = fixture.nativeElement.querySelector('textarea') as HTMLTextAreaElement;
    field.value = value;
    field.dispatchEvent(new Event('input'));
    field.dispatchEvent(new Event('blur'));
    fixture.detectChanges();
  };
  const submit = () => {
    fixture.nativeElement.querySelector('form').dispatchEvent(new Event('submit', { bubbles: true, cancelable: true }));
    fixture.detectChanges();
  };

  beforeEach(async () => {
    await TestBed.configureTestingModule({
      imports: [ApplicationFormComponent],
      providers: [provideHttpClient(), provideHttpClientTesting()],
    }).compileComponents();
    http = TestBed.inject(HttpTestingController);
    fixture = TestBed.createComponent(ApplicationFormComponent);
    fixture.componentRef.setInput('jobId', jobId.toUpperCase());
    fixture.detectChanges();
  });
  afterEach(() => http.verify());

  it('uses the profile CV without another upload or an initial HTTP call', () => {
    expect(text()).toContain('Your current profile CV will be used');
    expect(fixture.nativeElement.querySelector('input[type="file"]')).toBeNull();
    http.expectNone('/api/applications');
  });

  it('normalizes input, disables pending submission and emits one success', () => {
    const applied = vi.fn();
    fixture.componentInstance.applied.subscribe(applied);
    fill('  My motivation  ');
    submit();
    const request = http.expectOne('/api/applications');
    expect(request.request.body).toEqual({ jobId, coverLetter: 'My motivation' });
    expect(text()).toContain('Submitting application');
    expect(fixture.nativeElement.querySelector('textarea').disabled).toBe(true);
    expect(fixture.nativeElement.querySelector('button').disabled).toBe(true);
    submit();
    http.expectNone('/api/applications');
    request.flush(created, { status: 201, statusText: 'Created' });
    fixture.detectChanges();
    expect(text()).toContain('Application submitted');
    expect(text()).toContain(created.id);
    expect(applied).toHaveBeenCalledExactlyOnceWith(created);
    expect(fixture.nativeElement.querySelector('form')).toBeNull();
  });

  it('submits an empty optional cover letter as null', () => {
    fill('   ');
    submit();
    const request = http.expectOne('/api/applications');
    expect(request.request.body).toEqual({ jobId, coverLetter: null });
    request.flush(created);
  });

  it('blocks an invalid selected job without sending anything', () => {
    fixture.componentRef.setInput('jobId', 'invalid-job');
    fixture.detectChanges();
    expect(text()).toContain('A valid job must be selected');
    submit();
    http.expectNone('/api/applications');
  });

  it('blocks overlong cover letters and shows a Material field error', () => {
    fill('x'.repeat(5001));
    submit();
    expect(text()).toContain('Use at most 5000 characters');
    http.expectNone('/api/applications');
  });

  it('accepts 5000 characters plus trimmed whitespace, matching backend validation', () => {
    fill('  ' + 'x'.repeat(5000) + '  ');
    submit();
    const request = http.expectOne('/api/applications');
    expect(request.request.body.coverLetter.length).toBe(5000);
    request.flush(created);
  });

  it.each([
    [400, 'Please check the job and cover letter'],
    [401, 'Please sign in again'],
    [403, 'Only candidates'],
    [404, 'job or your candidate profile could not be found'],
    [409, 'already have applied'],
    [503, 'temporarily unavailable'],
    [500, 'could not confirm the application result'],
  ])('handles HTTP %s and retains the cover letter', (status, message) => {
    fill('Keep this text');
    submit();
    http.expectOne('/api/applications').flush({ detail: 'PRIVATE INTERNAL DETAIL' }, {
      status: status as number, statusText: 'Error',
    });
    fixture.detectChanges();
    expect(text()).toContain(message);
    expect(text()).not.toContain('PRIVATE INTERNAL DETAIL');
    expect(fixture.nativeElement.querySelector('[role="alert"]')).not.toBeNull();
    expect(fixture.nativeElement.querySelector('textarea').value).toBe('Keep this text');
    expect(fixture.nativeElement.querySelector('textarea').disabled).toBe(false);
    http.expectNone('/api/applications');
  });

  it('explains missing profile CV and allows an explicit retry after correction', () => {
    submit();
    http.expectOne('/api/applications').flush({ errors: { cv: ['Upload CV'] } }, { status: 400, statusText: 'Bad Request' });
    fixture.detectChanges();
    expect(text()).toContain('Upload a CV to your candidate profile');
    submit();
    expect(text()).not.toContain('Upload a CV');
    http.expectOne('/api/applications').flush(created);
    fixture.detectChanges();
    expect(text()).toContain('Application submitted');
  });

  it('reports an uncertain network outcome without automatically resubmitting', () => {
    fill('Keep this text');
    submit();
    http.expectOne('/api/applications').error(new ProgressEvent('error'));
    fixture.detectChanges();
    expect(text()).toContain('Check your applications before trying again');
    expect(fixture.nativeElement.querySelector('textarea').value).toBe('Keep this text');
    http.expectNone('/api/applications');
  });

  it('cancels the old UI request and resets state when the selected job changes', () => {
    submit();
    const old = http.expectOne('/api/applications');
    fixture.componentRef.setInput('jobId', 'bbbbbbbbbbbbbbbbbbbbbbbb');
    fixture.detectChanges();
    expect(old.cancelled).toBe(true);
    expect(fixture.nativeElement.querySelector('textarea').disabled).toBe(false);
    submit();
    const current = http.expectOne('/api/applications');
    expect(current.request.body.jobId).toBe('bbbbbbbbbbbbbbbbbbbbbbbb');
    current.flush({ ...created, jobId: 'bbbbbbbbbbbbbbbbbbbbbbbb' });
  });

  it('cancels pending HTTP work on destruction', () => {
    submit();
    const request = http.expectOne('/api/applications');
    fixture.destroy();
    expect(request.cancelled).toBe(true);
  });
});
