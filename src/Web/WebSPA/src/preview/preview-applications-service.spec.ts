import { firstValueFrom } from 'rxjs';
import { PREVIEW_APPLY_JOB_ID, PREVIEW_JOB_ID, PreviewApplicationsService } from './preview-applications-service';

describe('PreviewApplicationsService (in-memory only)', () => {
  it('paginates sample data and preserves all CV states', async () => {
    const service = new PreviewApplicationsService();
    const first = await firstValueFrom(service.getForJob(PREVIEW_JOB_ID));
    expect(first.totalCount).toBe(24);
    expect(first.items.length).toBe(20);
    expect(new Set(first.items.map(item => item.cvStatus)).size).toBe(4);
    const second = await firstValueFrom(service.getForJob(PREVIEW_JOB_ID, 2));
    expect(second.items.length).toBe(4);
    expect(second.items[0].id).toBe('demo-application-21');
  });

  it('shares a simulated submission across the candidate and matching company job views', async () => {
    const service = new PreviewApplicationsService();
    expect((await firstValueFrom(service.getForJob(PREVIEW_APPLY_JOB_ID))).totalCount).toBe(0);
    const submitted = await firstValueFrom(service.submitApplication({ jobId: PREVIEW_APPLY_JOB_ID, coverLetter: 'Sample letter' }));
    const candidate = await firstValueFrom(service.getMyApplications());
    expect(candidate.items[0]).toEqual(submitted);
    const company = await firstValueFrom(service.getForJob(PREVIEW_APPLY_JOB_ID));
    expect(company.totalCount).toBe(1);
    expect(company.items[0].coverLetter).toBe('Sample letter');
    await expect(firstValueFrom(service.submitApplication({ jobId: PREVIEW_APPLY_JOB_ID }))).rejects.toMatchObject({ status: 409 });
    expect((await firstValueFrom(new PreviewApplicationsService().getForJob(PREVIEW_APPLY_JOB_ID))).totalCount).toBe(0);
  });
});
