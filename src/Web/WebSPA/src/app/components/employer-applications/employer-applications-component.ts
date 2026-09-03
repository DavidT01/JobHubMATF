import { DatePipe } from '@angular/common';
import { HttpErrorResponse } from '@angular/common/http';
import { ChangeDetectionStrategy, Component, inject, input, OnChanges } from '@angular/core';
import { toSignal } from '@angular/core/rxjs-interop';
import { MatButtonModule } from '@angular/material/button';
import { MatCardModule } from '@angular/material/card';
import { MatPaginatorModule, PageEvent } from '@angular/material/paginator';
import { MatProgressBarModule } from '@angular/material/progress-bar';
import { BehaviorSubject, catchError, map, of, startWith, switchMap } from 'rxjs';

import { ApplicationStatus, PagedResult } from '../../core/models/application-list-item-dto';
import { EmployerApplicationDto } from '../../core/models/employer-application-dto';
import { ApplicationsService } from '../../core/services/applications/applications-service';

interface Query { jobId: string; pageIndex: number; pageSize: number; }
type ViewState =
  | { kind: 'invalid' }
  | { kind: 'loading'; query: Query }
  | { kind: 'loaded'; query: Query; result: PagedResult<EmployerApplicationDto> }
  | { kind: 'error'; query: Query; message: string; canRetry: boolean };

@Component({
  selector: 'app-employer-applications',
  imports: [DatePipe, MatButtonModule, MatCardModule, MatPaginatorModule, MatProgressBarModule],
  templateUrl: './employer-applications-component.html',
  styleUrl: './employer-applications-component.scss',
  changeDetection: ChangeDetectionStrategy.OnPush,
})
export class EmployerApplicationsComponent implements OnChanges {
  readonly jobId = input.required<string>();
  private readonly applications = inject(ApplicationsService);
  private readonly queries = new BehaviorSubject<Query | null>(null);

  protected readonly state = toSignal(this.queries.pipe(
    switchMap(query => query === null ? of<ViewState>({ kind: 'invalid' }) :
      this.applications.getForJob(query.jobId, query.pageIndex + 1, query.pageSize).pipe(
        map((result): ViewState => ({ kind: 'loaded', query, result })),
        catchError((error: unknown) => of<ViewState>({
          kind: 'error', query, message: this.errorMessage(error),
          canRetry: !(error instanceof HttpErrorResponse && [400, 401, 403, 404].includes(error.status)),
        })),
        startWith<ViewState>({ kind: 'loading', query }),
      )),
  ), { requireSync: true });

  ngOnChanges(): void {
    const jobId = this.jobId().trim().toLowerCase();
    this.queries.next(/^[a-f0-9]{24}$/.test(jobId) ? { jobId, pageIndex: 0, pageSize: 20 } : null);
  }

  protected changePage(event: PageEvent): void {
    const query = this.queries.value;
    if (query) this.queries.next({ ...query, pageIndex: event.pageIndex, pageSize: event.pageSize });
  }

  protected reload(firstPage = false): void {
    const query = this.queries.value;
    if (query) this.queries.next({ ...query, pageIndex: firstPage ? 0 : query.pageIndex });
  }

  protected cvLink(application: EmployerApplicationDto): string | null {
    if (application.cvStatus !== 'Available' || !application.currentCvUrl) return null;
    try {
      const url = new URL(application.currentCvUrl);
      return ['https:', 'http:'].includes(url.protocol) && !url.username && !url.password ? url.href : null;
    } catch { return null; }
  }

  protected cvMessage(application: EmployerApplicationDto): string {
    switch (application.cvStatus) {
      case 'Missing': return 'The candidate has no current CV.';
      case 'ProfileMissing': return 'The candidate profile is no longer available.';
      case 'ProfileReferenceMissing': return 'This older application has no profile reference for the current CV.';
      default: return 'The current CV link is unavailable. Refresh the list and try again.';
    }
  }

  protected statusLabel(status: ApplicationStatus): string {
    return ({ Submitted: 'Submitted', InReview: 'In review', Interview: 'Interview',
      Rejected: 'Rejected', Accepted: 'Accepted' } as Record<ApplicationStatus, string>)[status] ?? 'Unknown status';
  }

  private errorMessage(error: unknown): string {
    if (error instanceof HttpErrorResponse) {
      switch (error.status) {
        case 400: return 'The job or page selection is invalid.';
        case 401: return 'Please sign in again to view these applications.';
        case 403: return 'You do not have permission to view applications for this job.';
        case 404: return 'The job or your company profile could not be found.';
        case 0: return 'Unable to connect. Check your connection and try again.';
        case 503: return 'Applications are temporarily unavailable. Please try again later.';
      }
    }
    return 'Applications could not be loaded. Please try again.';
  }
}
