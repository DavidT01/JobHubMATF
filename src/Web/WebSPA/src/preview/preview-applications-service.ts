import { HttpErrorResponse } from '@angular/common/http';
import { Injectable } from '@angular/core';
import { defer, delay, of, throwError } from 'rxjs';
import { ApplicationListItemDto, ApplicationStatus, PagedResult } from '../app/core/models/application-list-item-dto';
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
    submittedAtUtc: '2026-09-02T10:00:00Z', updatedAtUtc: '2026-09-03T12:00:00Z',
    cvStatus: (['Available', 'Missing', 'ProfileMissing', 'ProfileReferenceMissing'] as const)[index % 4],
    currentCvUrl: index % 4 === 0 ? new URL('/preview-cv.txt', window.location.origin).href : null,
  }));
  private readonly candidateItems: ApplicationListItemDto[] = this.statuses.map((status, index) => ({
    id: `demo-own-${index + 1}`, jobId: `${index + 1}`.repeat(24), status,
    submittedAtUtc: '2026-09-02T10:00:00Z', updatedAtUtc: '2026-09-03T12:00:00Z',
  }));

  getMyApplications(pageNumber = 1, pageSize = 20) {
    return defer(() => of(this.page(this.candidateItems, pageNumber, pageSize))).pipe(delay(300));
  }

  getForJob(jobId: string, pageNumber = 1, pageSize = 20) {
    return defer(() => of(this.page(this.employerItems.filter(item => item.jobId === jobId), pageNumber, pageSize))).pipe(delay(300));
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

  private page<T>(items: readonly T[], pageNumber: number, pageSize: number): PagedResult<T> {
    return { items: items.slice((pageNumber - 1) * pageSize, pageNumber * pageSize), totalCount: items.length, pageNumber, pageSize };
  }
}
