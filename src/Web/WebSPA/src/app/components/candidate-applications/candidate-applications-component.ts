import { DatePipe } from '@angular/common';
import { HttpErrorResponse } from '@angular/common/http';
import { ChangeDetectionStrategy, Component, inject } from '@angular/core';
import { toSignal } from '@angular/core/rxjs-interop';
import { MatButtonModule } from '@angular/material/button';
import { MatCardModule } from '@angular/material/card';
import { MatFormFieldModule } from '@angular/material/form-field';
import { MatPaginatorModule, PageEvent } from '@angular/material/paginator';
import { MatProgressBarModule } from '@angular/material/progress-bar';
import { MatSelectModule } from '@angular/material/select';
import { BehaviorSubject, catchError, map, of, startWith, switchMap } from 'rxjs';

import { ApplicationListItemDto, ApplicationStatus, PagedResult } from '../../core/models/application-list-item-dto';
import { ApplicationSortBy, SortDirection } from '../../core/models/application-management-dto';
import { ApplicationsService } from '../../core/services/applications/applications-service';

interface PageRequest {
  pageIndex: number;
  pageSize: number;
  status?: ApplicationStatus;
  sortBy?: ApplicationSortBy;
  sortDirection?: SortDirection;
}
type ViewState =
  | { kind: 'loading'; page: PageRequest }
  | { kind: 'loaded'; page: PageRequest; result: PagedResult<ApplicationListItemDto> }
  | { kind: 'error'; page: PageRequest; message: string; canRetry: boolean };

@Component({
  selector: 'app-candidate-applications',
  imports: [DatePipe, MatButtonModule, MatCardModule, MatFormFieldModule, MatPaginatorModule,
    MatProgressBarModule, MatSelectModule],
  templateUrl: './candidate-applications-component.html',
  styleUrl: './candidate-applications-component.scss',
  changeDetection: ChangeDetectionStrategy.OnPush,
})
export class CandidateApplicationsComponent {
  private readonly applications = inject(ApplicationsService);
  private readonly pages = new BehaviorSubject<PageRequest>({ pageIndex: 0, pageSize: 20 });
  protected readonly statuses: readonly ApplicationStatus[] =
    ['Submitted', 'InReview', 'Interview', 'Rejected', 'Accepted'];

  protected readonly state = toSignal(this.pages.pipe(
    switchMap(page => this.applications.getMyApplications(page.pageIndex + 1, page.pageSize, {
      status: page.status, sortBy: page.sortBy, sortDirection: page.sortDirection,
    }).pipe(
      map((result): ViewState => ({ kind: 'loaded', page, result })),
      catchError((error: unknown) => of<ViewState>({
        kind: 'error', page,
        message: this.errorMessage(error),
        canRetry: !(error instanceof HttpErrorResponse && [401, 403].includes(error.status)),
      })),
      startWith<ViewState>({ kind: 'loading', page }),
    )),
  ), { requireSync: true });

  protected changePage(event: PageEvent): void {
    this.pages.next({ pageIndex: event.pageIndex, pageSize: event.pageSize });
  }

  protected retry(): void {
    this.pages.next({ ...this.pages.value });
  }

  protected firstPage(): void {
    this.pages.next({ ...this.pages.value, pageIndex: 0 });
  }

  protected currentPage(): PageRequest {
    return this.pages.value;
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

  protected hasStatusFilter(page: PageRequest): boolean {
    return page.status !== undefined;
  }

  protected statusLabel(status: ApplicationStatus): string {
    const labels: Record<ApplicationStatus, string> = {
      Submitted: 'Submitted', InReview: 'In review', Interview: 'Interview',
      Rejected: 'Rejected', Accepted: 'Accepted',
    };
    return labels[status] ?? 'Unknown status';
  }

  private changeFilters(changes: Partial<PageRequest>): void {
    this.pages.next({ ...this.pages.value, ...changes, pageIndex: 0 });
  }

  private errorMessage(error: unknown): string {
    if (error instanceof HttpErrorResponse) {
      switch (error.status) {
        case 401: return 'Please sign in again to view your applications.';
        case 403: return 'This page is only available to candidates.';
        case 404: return 'Your candidate profile could not be found. Complete your profile and try again.';
        case 0: return 'Unable to connect. Check your connection and try again.';
        case 503: return 'Applications are temporarily unavailable. Please try again later.';
      }
    }
    return 'Your applications could not be loaded. Please try again.';
  }
}
