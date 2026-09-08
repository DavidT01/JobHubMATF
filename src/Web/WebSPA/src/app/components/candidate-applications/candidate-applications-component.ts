import { DatePipe } from '@angular/common';
import { HttpErrorResponse } from '@angular/common/http';
import { ChangeDetectionStrategy, Component, inject } from '@angular/core';
import { toSignal } from '@angular/core/rxjs-interop';
import { MatButtonModule } from '@angular/material/button';
import { MatCardModule } from '@angular/material/card';
import { MatPaginatorModule, PageEvent } from '@angular/material/paginator';
import { MatProgressBarModule } from '@angular/material/progress-bar';
import { BehaviorSubject, catchError, map, of, startWith, switchMap } from 'rxjs';

import { ApplicationListItemDto, ApplicationStatus, PagedResult } from '../../core/models/application-list-item-dto';
import { ApplicationsService } from '../../core/services/applications/applications-service';

interface PageRequest { pageIndex: number; pageSize: number; }
type ViewState =
  | { kind: 'loading'; page: PageRequest }
  | { kind: 'loaded'; page: PageRequest; result: PagedResult<ApplicationListItemDto> }
  | { kind: 'error'; page: PageRequest; message: string; canRetry: boolean };

@Component({
  selector: 'app-candidate-applications',
  imports: [DatePipe, MatButtonModule, MatCardModule, MatPaginatorModule, MatProgressBarModule],
  templateUrl: './candidate-applications-component.html',
  styleUrl: './candidate-applications-component.scss',
  changeDetection: ChangeDetectionStrategy.OnPush,
})
export class CandidateApplicationsComponent {
  private readonly applications = inject(ApplicationsService);
  private readonly pages = new BehaviorSubject<PageRequest>({ pageIndex: 0, pageSize: 20 });

  protected readonly state = toSignal(this.pages.pipe(
    switchMap(page => this.applications.getMyApplications(page.pageIndex + 1, page.pageSize).pipe(
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
    this.pages.next({ pageIndex: 0, pageSize: this.pages.value.pageSize });
  }

  protected statusLabel(status: ApplicationStatus): string {
    const labels: Record<ApplicationStatus, string> = {
      Submitted: 'Submitted', InReview: 'In review', Interview: 'Interview',
      Rejected: 'Rejected', Accepted: 'Accepted',
    };
    return labels[status] ?? 'Unknown status';
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
