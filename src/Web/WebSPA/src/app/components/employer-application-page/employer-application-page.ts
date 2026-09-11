import { ChangeDetectionStrategy, Component, inject, signal } from '@angular/core';
import { toSignal } from '@angular/core/rxjs-interop';
import { RouterLink } from '@angular/router';
import { MatButtonModule } from '@angular/material/button';
import { MatFormFieldModule } from '@angular/material/form-field';
import { MatSelectModule } from '@angular/material/select';
import { MatProgressBarModule } from '@angular/material/progress-bar';
import { Subject, catchError, map, of, startWith, switchMap } from 'rxjs';
import { AuthService } from '../../core/services/auth.service';
import { CompanyProfileService } from '../../core/services/company-profile/company-profile-service';
import { JobService } from '../../services/job.service';
import { Job } from '../../models/job.model';
import { EmployerApplicationsComponent } from '../employer-applications/employer-applications-component';

type State = { kind: 'loading' } | { kind: 'error' } | { kind: 'loaded'; jobs: Job[] };

@Component({
  selector: 'app-employer-application-page',
  imports: [RouterLink, MatButtonModule, MatFormFieldModule, MatSelectModule,
    MatProgressBarModule, EmployerApplicationsComponent],
  templateUrl: './employer-application-page.html',
  styles: [':host { display: block; padding: 24px; } mat-form-field { width: min(100%, 480px); }'],
  changeDetection: ChangeDetectionStrategy.OnPush,
})
export class EmployerApplicationPage {
  private readonly auth = inject(AuthService);
  private readonly profiles = inject(CompanyProfileService);
  private readonly catalog = inject(JobService);
  private readonly refresh = new Subject<void>();
  protected readonly selectedJobId = signal('');
  protected readonly state = toSignal(this.refresh.pipe(
    startWith(undefined),
    switchMap(() => this.auth.me().pipe(
      switchMap(user => this.profiles.getProfile(user.id)),
      switchMap(profile => this.catalog.getByCompanyId(profile.id).pipe(
        map(jobs => ({ kind: 'loaded', jobs: jobs.filter(job => job.companyId === profile.id) }) as State),
      )),
      catchError(() => of<State>({ kind: 'error' })),
      startWith<State>({ kind: 'loading' }),
    )),
  ), { requireSync: true });

  protected reload(): void {
    this.selectedJobId.set('');
    this.refresh.next();
  }
}
