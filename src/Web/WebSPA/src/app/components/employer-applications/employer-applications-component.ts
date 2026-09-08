import { DatePipe } from '@angular/common';
import { HttpErrorResponse } from '@angular/common/http';
import { ChangeDetectionStrategy, Component, DestroyRef, inject, input, OnChanges, signal } from '@angular/core';
import { takeUntilDestroyed, toSignal } from '@angular/core/rxjs-interop';
import { MatButtonModule } from '@angular/material/button';
import { MatCardModule } from '@angular/material/card';
import { MatFormFieldModule } from '@angular/material/form-field';
import { MatPaginatorModule, PageEvent } from '@angular/material/paginator';
import { MatProgressBarModule } from '@angular/material/progress-bar';
import { MatSelectModule } from '@angular/material/select';
import { BehaviorSubject, catchError, map, of, startWith, switchMap } from 'rxjs';

import { ApplicationStatus, PagedResult } from '../../core/models/application-list-item-dto';
import { ApplicationSortBy, SortDirection } from '../../core/models/application-management-dto';
import { EmployerApplicationDto } from '../../core/models/employer-application-dto';
import { ApplicationsService } from '../../core/services/applications/applications-service';

interface Query {
  jobId: string;
  pageIndex: number;
  pageSize: number;
  status?: ApplicationStatus;
  sortBy?: ApplicationSortBy;
  sortDirection?: SortDirection;
}
type ViewState =
  | { kind: 'invalid' }
  | { kind: 'loading'; query: Query }
  | { kind: 'loaded'; query: Query; result: PagedResult<EmployerApplicationDto> }
  | { kind: 'error'; query: Query; message: string; canRetry: boolean };

const STATUS_TRANSITIONS: Readonly<Record<ApplicationStatus, readonly ApplicationStatus[]>> = {
  Submitted: ['InReview', 'Rejected'],
  InReview: ['Interview', 'Accepted', 'Rejected'],
  Interview: ['Accepted', 'Rejected'],
  Rejected: [],
  Accepted: [],
};

@Component({
  selector: 'app-employer-applications',
  imports: [DatePipe, MatButtonModule, MatCardModule, MatFormFieldModule, MatPaginatorModule,
    MatProgressBarModule, MatSelectModule],
  templateUrl: './employer-applications-component.html',
  styleUrl: './employer-applications-component.scss',
  changeDetection: ChangeDetectionStrategy.OnPush,
})
export class EmployerApplicationsComponent implements OnChanges {
  readonly jobId = input.required<string>();
  private readonly applications = inject(ApplicationsService);
  private readonly destroyRef = inject(DestroyRef);
  private readonly queries = new BehaviorSubject<Query | null>(null);
  private readonly updating = signal<Readonly<Record<string, boolean>>>({});
  private readonly updateErrors = signal<Readonly<Record<string, string>>>({});
  protected readonly statuses: readonly ApplicationStatus[] =
    ['Submitted', 'InReview', 'Interview', 'Rejected', 'Accepted'];

  protected readonly state = toSignal(this.queries.pipe(
    switchMap(query => query === null ? of<ViewState>({ kind: 'invalid' }) :
      this.applications.getForJob(query.jobId, query.pageIndex + 1, query.pageSize, {
        status: query.status, sortBy: query.sortBy, sortDirection: query.sortDirection,
      }).pipe(
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

  protected currentQuery(): Query | null {
    return this.queries.value;
  }

  protected changeStatusFilter(status: ApplicationStatus | ''): void {
    this.changeFilters({ status: status || undefined });
  }

  protected changeSortBy(sortBy: ApplicationSortBy): void {
    this.changeFilters({ sortBy });
  }

  protected changeSortDirection(sortDirection: SortDirection): void {
    this.changeFilters({ sortDirection });
  }

  protected hasStatusFilter(query: Query): boolean {
    return query.status !== undefined;
  }

  protected statusActions(status: ApplicationStatus): readonly ApplicationStatus[] {
    return STATUS_TRANSITIONS[status] ?? [];
  }

  protected statusActionLabel(status: ApplicationStatus): string {
    return ({ InReview: 'Move to review', Interview: 'Move to interview',
      Rejected: 'Reject', Accepted: 'Accept' } as Partial<Record<ApplicationStatus, string>>)[status]
      ?? status;
  }

  protected isUpdating(applicationId: string): boolean {
    return this.updating()[applicationId] === true;
  }

  protected updateError(applicationId: string): string | null {
    return this.updateErrors()[applicationId] ?? null;
  }

  protected changeStatus(application: EmployerApplicationDto, status: ApplicationStatus): void {
    if (this.isUpdating(application.id) || !this.statusActions(application.status).includes(status)) return;

    this.setUpdating(application.id, true);
    this.setUpdateError(application.id, null);
    this.applications.changeStatus(application.id, status).pipe(
      takeUntilDestroyed(this.destroyRef),
    ).subscribe({
      next: () => {
        this.setUpdating(application.id, false);
        if (this.queries.value?.jobId === application.jobId) this.reload();
      },
      error: error => {
        this.setUpdating(application.id, false);
        this.setUpdateError(application.id, this.statusChangeErrorMessage(error));
      },
    });
  }

  private setUpdating(applicationId: string, value: boolean): void {
    this.updating.update(current => {
      const next = { ...current };
      if (value) next[applicationId] = true;
      else delete next[applicationId];
      return next;
    });
  }

  private changeFilters(changes: Partial<Query>): void {
    const query = this.queries.value;
    if (query) this.queries.next({ ...query, ...changes, pageIndex: 0 });
  }

  private setUpdateError(applicationId: string, message: string | null): void {
    this.updateErrors.update(current => {
      const next = { ...current };
      if (message) next[applicationId] = message;
      else delete next[applicationId];
      return next;
    });
  }

  private statusChangeErrorMessage(error: unknown): string {
    if (error instanceof HttpErrorResponse) {
      switch (error.status) {
        case 400: return 'That status change is not valid.';
        case 401: return 'Please sign in again before changing the status.';
        case 403: return 'You do not have permission to change this application.';
        case 404: return 'The application or job could not be found.';
        case 409: return 'This application changed. Refresh the list and try again.';
        case 0: return 'Unable to connect. Check your connection and try again.';
        case 503: return 'Status changes are temporarily unavailable. Try again later.';
      }
    }
    return 'The application status could not be changed. Try again.';
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
