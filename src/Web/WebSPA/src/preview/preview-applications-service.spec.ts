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
  });

  it('filters and sorts both application views like the real API', async () => {
    const service = new PreviewApplicationsService();
    const employer = await firstValueFrom(service.getForJob(PREVIEW_JOB_ID, 1, 20, {
      status: 'Submitted', sortBy: 'UpdatedAtUtc', sortDirection: 'Asc',
    }));
    expect(employer.totalCount).toBe(5);
    expect(employer.items.every(item => item.status === 'Submitted')).toBe(true);
    expect(employer.items.map(item => item.updatedAtUtc)).toEqual(
      [...employer.items.map(item => item.updatedAtUtc)].sort(),
    );

    const candidate = await firstValueFrom(service.getMyApplications(1, 20, {
      status: 'Accepted', sortBy: 'SubmittedAtUtc', sortDirection: 'Desc',
    }));
    expect(candidate.totalCount).toBe(1);
    expect(candidate.items[0].status).toBe('Accepted');
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
    await firstValueFrom(service.changeStatus(submitted.id, 'InReview'));
    expect((await firstValueFrom(service.getMyApplications())).items[0].status).toBe('InReview');
    expect((await firstValueFrom(service.getForJob(PREVIEW_APPLY_JOB_ID))).items[0].status).toBe('InReview');
    await expect(firstValueFrom(service.changeStatus(submitted.id, 'Submitted')))
      .rejects.toMatchObject({ status: 409 });
    await expect(firstValueFrom(service.submitApplication({ jobId: PREVIEW_APPLY_JOB_ID }))).rejects.toMatchObject({ status: 409 });
    expect((await firstValueFrom(new PreviewApplicationsService().getForJob(PREVIEW_APPLY_JOB_ID))).totalCount).toBe(0);
  });

  it('calculates status rates and daily totals for an inclusive preview period', async () => {
    const service = new PreviewApplicationsService();
    const result = await firstValueFrom(service.getStatistics({
      from: '2026-09-01', to: '2026-09-02',
    }));
    expect(result.totalCount).toBe(8);
    expect(result.from).toBe('2026-09-01');
    expect(result.to).toBe('2026-09-02');
    expect(result.byStatus).toHaveLength(5);
    expect(result.byStatus.reduce((total, status) => total + status.count, 0)).toBe(8);
    expect(result.dailyTrend).toEqual([
      { date: '2026-09-01', count: 4 },
      { date: '2026-09-02', count: 4 },
    ]);
    await expect(firstValueFrom(service.getStatistics({
      from: '2026-09-02', to: '2026-09-01',
    }))).rejects.toMatchObject({ status: 400 });
  });
});
