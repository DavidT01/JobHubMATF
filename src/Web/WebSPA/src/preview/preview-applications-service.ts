import { HttpErrorResponse } from '@angular/common/http';
import { Injectable } from '@angular/core';
import { defer, delay, of, throwError } from 'rxjs';
import { ApplicationListItemDto, ApplicationStatus, PagedResult } from '../app/core/models/application-list-item-dto';
import {
  ApplicationListOptions,
  ApplicationStatisticsDto,
  ApplicationStatisticsPeriod,
} from '../app/core/models/application-management-dto';
import { EmployerApplicationDto } from '../app/core/models/employer-application-dto';
import { SubmitApplicationRequest } from '../app/core/models/submit-application-request';

export const PREVIEW_JOB_ID = 'aaaaaaaaaaaaaaaaaaaaaaaa';
export const PREVIEW_APPLY_JOB_ID = 'bbbbbbbbbbbbbbbbbbbbbbbb';

@Injectable()
export class PreviewApplicationsService {
  private readonly statuses: ApplicationStatus[] = ['Submitted', 'InReview', 'Interview', 'Rejected', 'Accepted'];
  private readonly employerItems: EmployerApplicationDto[] = Array.from({ length: 24 }, (_, index) => ({
    id: `demo-application-${index + 1}`, jobId: PREVIEW_JOB_ID,
    candidateId: `demo-profile-${index + 1}`, candidateName: `Demo candidate ${index + 1}`,
    coverLetter: index % 3 === 0 ? 'I enjoy building reliable applications.\nThis is a sample cover letter for the local preview.' : null,
    status: this.statuses[index % this.statuses.length],
    submittedAtUtc: new Date(Date.UTC(2026, 8, index % 6 + 1, 10)).toISOString(),
    updatedAtUtc: new Date(Date.UTC(2026, 8, index % 6 + 2, 12)).toISOString(),
    cvStatus: (['Available', 'Missing', 'ProfileMissing', 'ProfileReferenceMissing'] as const)[index % 4],
    currentCvUrl: index % 4 === 0 ? new URL('/preview-cv.txt', window.location.origin).href : null,
  }));
  private readonly candidateItems: ApplicationListItemDto[] = this.statuses.map((status, index) => ({
    id: `demo-own-${index + 1}`, jobId: `${index + 1}`.repeat(24), status,
    submittedAtUtc: new Date(Date.UTC(2026, 8, index + 1, 10)).toISOString(),
    updatedAtUtc: new Date(Date.UTC(2026, 8, index + 2, 12)).toISOString(),
  }));

  getMyApplications(pageNumber = 1, pageSize = 20, options: ApplicationListOptions = {}) {
    return defer(() => of(this.page(
      this.filterAndSort(this.candidateItems, options), pageNumber, pageSize,
    ))).pipe(delay(300));
  }

  getForJob(jobId: string, pageNumber = 1, pageSize = 20, options: ApplicationListOptions = {}) {
    return defer(() => of(this.page(this.filterAndSort(
      this.employerItems.filter(item => item.jobId === jobId), options,
    ), pageNumber, pageSize))).pipe(delay(300));
  }

  submitApplication(request: SubmitApplicationRequest) {
    return defer(() => {
      if (this.candidateItems.some(item => item.jobId === request.jobId)) {
        return throwError(() => new HttpErrorResponse({ status: 409, statusText: 'Preview duplicate' }));
      }
      const now = new Date().toISOString();
      const result: ApplicationListItemDto = { id: `demo-submitted-${this.candidateItems.length + 1}`,
        jobId: request.jobId, status: 'Submitted', submittedAtUtc: now, updatedAtUtc: now };
      this.candidateItems.unshift(result);
      this.employerItems.unshift({ ...result, candidateId: 'demo-current-candidate', candidateName: 'Demo current candidate',
        coverLetter: request.coverLetter ?? null, cvStatus: 'Available',
        currentCvUrl: new URL('/preview-cv.txt', window.location.origin).href });
      return of(result).pipe(delay(300));
    });
  }

  changeStatus(applicationId: string, status: ApplicationStatus) {
    return defer(() => {
      const index = this.employerItems.findIndex(item => item.id === applicationId);
      if (index < 0) return throwError(() => new HttpErrorResponse({ status: 404 }));
      const application = this.employerItems[index];
      if (!this.allowedTransitions(application.status).includes(status)) {
        return throwError(() => new HttpErrorResponse({ status: 409 }));
      }

      const updatedAtUtc = new Date().toISOString();
      this.employerItems[index] = { ...application, status, updatedAtUtc };
      const candidateIndex = this.candidateItems.findIndex(item => item.id === applicationId);
      if (candidateIndex >= 0) {
        this.candidateItems[candidateIndex] = { ...this.candidateItems[candidateIndex], status, updatedAtUtc };
      }
      return of(void 0).pipe(delay(300));
    });
  }

  getStatistics(period: ApplicationStatisticsPeriod = {}) {
    return defer(() => {
      if (period.from && period.to && period.from > period.to) {
        return throwError(() => new HttpErrorResponse({ status: 400 }));
      }

      const items = this.employerItems.filter(item => {
        const date = item.submittedAtUtc.slice(0, 10);
        return (!period.from || date >= period.from) && (!period.to || date <= period.to);
      });
      const byStatus = this.statuses.map(status => {
        const count = items.filter(item => item.status === status).length;
        return { status, count, ratePercent: items.length === 0 ? 0 : Math.round(count * 10000 / items.length) / 100 };
      });
      const dailyCounts = new Map<string, number>();
      for (const item of items) {
        const date = item.submittedAtUtc.slice(0, 10);
        dailyCounts.set(date, (dailyCounts.get(date) ?? 0) + 1);
      }
      const result: ApplicationStatisticsDto = {
        totalCount: items.length,
        from: period.from ?? null,
        to: period.to ?? null,
        byStatus,
        dailyTrend: [...dailyCounts.entries()].sort(([left], [right]) => left.localeCompare(right))
          .map(([date, count]) => ({ date, count })),
      };
      return of(result).pipe(delay(300));
    });
  }

  private filterAndSort<T extends ApplicationListItemDto>(
    items: readonly T[], options: ApplicationListOptions,
  ): T[] {
    const filtered = options.status ? items.filter(item => item.status === options.status) : [...items];
    const field: 'submittedAtUtc' | 'updatedAtUtc' =
      options.sortBy === 'UpdatedAtUtc' ? 'updatedAtUtc' : 'submittedAtUtc';
    const direction = options.sortDirection === 'Asc' ? 1 : -1;
    return filtered.sort((left, right) => direction * (
      left[field].localeCompare(right[field]) || left.id.localeCompare(right.id)
    ));
  }

  private allowedTransitions(status: ApplicationStatus): readonly ApplicationStatus[] {
    return ({
      Submitted: ['InReview', 'Rejected'],
      InReview: ['Interview', 'Accepted', 'Rejected'],
      Interview: ['Accepted', 'Rejected'],
      Rejected: [],
      Accepted: [],
    } as const)[status];
  }

  private page<T>(items: readonly T[], pageNumber: number, pageSize: number): PagedResult<T> {
    return { items: items.slice((pageNumber - 1) * pageSize, pageNumber * pageSize), totalCount: items.length, pageNumber, pageSize };
  }
}
