import { Component, OnInit, computed, inject, signal } from '@angular/core';
import { CommonModule } from '@angular/common';
import { ActivatedRoute, Router, RouterLink } from '@angular/router';
import { MatDialog, MatDialogModule } from '@angular/material/dialog';
import { MatCardModule } from '@angular/material/card';
import { MatChipsModule } from '@angular/material/chips';
import { MatButtonModule } from '@angular/material/button';
import { MatIconModule } from '@angular/material/icon';
import { MatProgressSpinnerModule } from '@angular/material/progress-spinner';
import { switchMap } from 'rxjs';
import { Job } from '../../models/job.model';
import { MatchResult } from '../../models/match-result.model';
import { JobService } from '../../services/job.service';
import { CurrentUser } from '../../core/current-user';
import { AuthService } from '../../core/services/auth.service';
import { PageHeaderComponent } from '../../shared/page-header/page-header.component';
import { JobLabelPipe } from '../../shared/job-label.pipe';
import { ApplicationFormComponent } from '../application-form/application-form-component';
import { ConfirmDialogComponent } from '../confirm-dialog/confirm-dialog-component';
import { CompanyProfileService } from '../../core/services/company-profile/company-profile-service';
import { takeUntilDestroyed } from '@angular/core/rxjs-interop';
import { DestroyRef } from '@angular/core';

@Component({
  selector: 'app-job-details',
  imports: [
    CommonModule,
    ApplicationFormComponent,
    RouterLink,
    MatCardModule,
    MatChipsModule,
    MatButtonModule,
    MatIconModule,
    MatProgressSpinnerModule,
    MatDialogModule,
    PageHeaderComponent,
    JobLabelPipe
  ],
  templateUrl: './job-details.html',
  styleUrl: './job-details.scss',
})
export class JobDetails implements OnInit {
  private route = inject(ActivatedRoute);
  private jobService = inject(JobService);
  private currentUser = inject(CurrentUser);
  private companyProfiles = inject(CompanyProfileService);
  private dialog = inject(MatDialog);
  private router = inject(Router);
  protected readonly auth = inject(AuthService);
  private readonly destroyRef = inject(DestroyRef);
  readonly isCandidate = signal(false);

  /** Profile id of the signed-in employer's company; jobs reference their company by it. */
  private readonly ownCompanyId = signal<string | null>(null);
  readonly isOwner = computed(() => {
    const companyId = this.ownCompanyId();
    return !!companyId && this.job()?.companyId === companyId;
  });
  deleting = signal(false);

  acceptingApplications(): boolean {
    const job = this.job();
    return !!job?.isActive && (!job.expirationDate || Date.parse(job.expirationDate) > Date.now());
  }

  locationLabel(job: Job): string {
    const place = [job.city, job.country].filter(Boolean).join(', ');
    return place ? `${job.companyName} · ${place}` : job.companyName;
  }

  job = signal<Job | null>(null);
  loading = signal(true);
  error = signal<string | null>(null);
  bookmarked = signal(false);

  matchResult = signal<MatchResult | null>(null);
  matchLoading = signal(false);
  matchError = signal<string | null>(null);

  ngOnInit(): void {
    if (this.auth.isLoggedIn()) {
      this.auth.me().pipe(takeUntilDestroyed(this.destroyRef)).subscribe({
        next: profile => {
          this.isCandidate.set(profile.roles.includes('Candidate'));
          if (profile.roles.includes('Candidate')) {
            this.loadBookmarkState();
          }
          if (profile.roles.includes('Employer')) {
            this.companyProfiles.getMine().subscribe({
              next: company => this.ownCompanyId.set(company.id),
              error: () => this.ownCompanyId.set(null),
            });
          }
        },
        error: () => this.isCandidate.set(false),
      });
    }
    const id = this.route.snapshot.paramMap.get('id');

    if (!id) {
      this.error.set('The job ID is invalid.');
      this.loading.set(false);
      return;
    }

    this.jobService.getById(id).subscribe({
      next: (data) => {
        this.job.set(data);
        this.loading.set(false);
      },
      error: (err) => {
        this.error.set('This job could not be found.');
        this.loading.set(false);
        console.error(err);
      }
    });
  }

  /** Bookmarks belong to candidates, so other roles never ask for them. */
  private loadBookmarkState(): void {
    const id = this.route.snapshot.paramMap.get('id');
    this.currentUser.getUserId().pipe(
      switchMap((userId) => this.jobService.getBookmarks(userId))
    ).subscribe({
      next: (bookmarks) => {
        this.bookmarked.set(bookmarks.some((b) => b.id === id));
      },
      error: (err) => console.error(err),
    });
  }

  deleteJob(): void {
    const job = this.job();
    if (!job || !this.isOwner()) return;

    this.dialog.open(ConfirmDialogComponent, {
      width: '400px',
      data: { message: `Delete "${job.title}"? Candidates will no longer see this job.`, confirmLabel: 'Delete' },
    }).afterClosed().subscribe(confirmed => {
      if (confirmed !== true) return;

      this.deleting.set(true);
      this.jobService.delete(job.id).subscribe({
        next: () => this.router.navigate(['/jobs']),
        error: (err) => {
          this.deleting.set(false);
          this.error.set('The job could not be deleted. Please try again.');
          console.error(err);
        },
      });
    });
  }

  toggleBookmark(): void {
    const job = this.job();
    if (!job) return;

    const wasBookmarked = this.bookmarked();

    this.currentUser.getUserId().pipe(
      switchMap((userId) =>
        wasBookmarked
          ? this.jobService.removeBookmark(userId, job.id)
          : this.jobService.addBookmark(userId, job.id)
      )
    ).subscribe({
      next: () => this.bookmarked.set(!wasBookmarked),
      error: (err) => console.error(err),
    });
  }

  checkMatch(): void {
    const job = this.job();
    if (!job) return;

    this.matchLoading.set(true);
    this.matchError.set(null);
    this.matchResult.set(null);

    this.currentUser.getUserId().pipe(
      switchMap((userId) => this.jobService.getMatch(job.id, userId))
    ).subscribe({
      next: (result) => {
        this.matchResult.set(result);
        this.matchLoading.set(false);
      },
      error: (err) => {
        this.matchError.set('Your profile data is not available right now.');
        this.matchLoading.set(false);
        console.error(err);
      },
    });
  }
}
