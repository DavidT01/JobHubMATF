import { DecimalPipe } from '@angular/common';
import { HttpErrorResponse } from '@angular/common/http';
import { ChangeDetectionStrategy, Component, inject, signal } from '@angular/core';
import { toSignal } from '@angular/core/rxjs-interop';
import { FormControl, FormGroup, ReactiveFormsModule } from '@angular/forms';
import { MatButtonModule } from '@angular/material/button';
import { MatCardModule } from '@angular/material/card';
import { MatFormFieldModule } from '@angular/material/form-field';
import { MatInputModule } from '@angular/material/input';
import { MatProgressBarModule } from '@angular/material/progress-bar';
import { BehaviorSubject, catchError, map, of, startWith, switchMap } from 'rxjs';

import {
  ApplicationStatisticsDto,
  ApplicationStatisticsPeriod,
  DailyApplicationCountDto,
} from '../../core/models/application-management-dto';
import { ApplicationStatus } from '../../core/models/application-list-item-dto';
import { ApplicationsService } from '../../core/services/applications/applications-service';

type ViewState =
  | { kind: 'loading'; period: ApplicationStatisticsPeriod }
  | { kind: 'loaded'; period: ApplicationStatisticsPeriod; result: ApplicationStatisticsDto }
  | { kind: 'error'; period: ApplicationStatisticsPeriod; message: string; canRetry: boolean };

@Component({
  selector: 'app-application-statistics',
  imports: [DecimalPipe, MatButtonModule, MatCardModule, MatFormFieldModule, MatInputModule,
    MatProgressBarModule, ReactiveFormsModule],
  templateUrl: './application-statistics-component.html',
  styleUrl: './application-statistics-component.scss',
  changeDetection: ChangeDetectionStrategy.OnPush,
})
export class ApplicationStatisticsComponent {
  private readonly applications = inject(ApplicationsService);
  private readonly periods = new BehaviorSubject<ApplicationStatisticsPeriod>({});
  protected readonly periodError = signal<string | null>(null);
  protected readonly periodForm = new FormGroup({
    from: new FormControl('', { nonNullable: true }),
    to: new FormControl('', { nonNullable: true }),
  });

  protected readonly state = toSignal(this.periods.pipe(
    switchMap(period => this.applications.getStatistics(period).pipe(
      map((result): ViewState => ({ kind: 'loaded', period, result })),
      catchError((error: unknown) => of<ViewState>({
        kind: 'error', period, message: this.errorMessage(error),
        canRetry: !(error instanceof HttpErrorResponse && [401, 403].includes(error.status)),
      })),
      startWith<ViewState>({ kind: 'loading', period }),
    )),
  ), { requireSync: true });

  protected applyPeriod(): void {
    const { from, to } = this.periodForm.getRawValue();
    if (from && to && from > to) {
      this.periodError.set('From date must be earlier than or equal to the to date.');
      return;
    }

    this.periodError.set(null);
    this.periods.next({ from: from || undefined, to: to || undefined });
  }

  protected clearPeriod(): void {
    this.periodForm.reset({ from: '', to: '' });
    this.periodError.set(null);
    this.periods.next({});
  }

  protected retry(): void {
    this.periods.next({ ...this.periods.value });
  }

  protected barWidth(point: DailyApplicationCountDto, trend: readonly DailyApplicationCountDto[]): string {
    const maximum = Math.max(...trend.map(item => item.count), 1);
    return `${Math.round(point.count * 100 / maximum)}%`;
  }

  protected statusLabel(status: ApplicationStatus): string {
    return ({ Submitted: 'Submitted', InReview: 'In review', Interview: 'Interview',
      Rejected: 'Rejected', Accepted: 'Accepted' } as Record<ApplicationStatus, string>)[status]
      ?? 'Unknown status';
  }

  private errorMessage(error: unknown): string {
    if (error instanceof HttpErrorResponse) {
      switch (error.status) {
        case 400: return 'Select a valid statistics period.';
        case 401: return 'Please sign in again to view application statistics.';
        case 403: return 'Application statistics are only available to administrators.';
        case 0: return 'Unable to connect. Check your connection and try again.';
        case 503: return 'Application statistics are temporarily unavailable. Try again later.';
      }
    }
    return 'Application statistics could not be loaded. Try again.';
  }
}
