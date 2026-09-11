import { Component, OnInit, inject, signal } from '@angular/core';
import { CommonModule } from '@angular/common';
import { ActivatedRoute, RouterLink } from '@angular/router';
import { MatCardModule } from '@angular/material/card';
import { MatChipsModule } from '@angular/material/chips';
import { MatButtonModule } from '@angular/material/button';
import { MatIconModule } from '@angular/material/icon';
import { MatProgressSpinnerModule } from '@angular/material/progress-spinner';
import { Job } from '../../models/job.model';
import { MatchResult } from '../../models/match-result.model';
import { JobService } from '../../services/job.service';
import { CurrentUser } from '../../core/current-user';
import { AuthService } from '../../core/services/auth.service';
import { ApplicationFormComponent } from '../application-form/application-form-component';
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
    MatProgressSpinnerModule
  ],
  templateUrl: './job-details.html',
  styleUrl: './job-details.scss',
})
export class JobDetails implements OnInit {
  private route = inject(ActivatedRoute);
  private jobService = inject(JobService);
  private currentUser = inject(CurrentUser);
  protected readonly auth = inject(AuthService);
  private readonly destroyRef = inject(DestroyRef);
  readonly isCandidate = signal(false);

  acceptingApplications(): boolean {
    const job = this.job();
    return !!job?.isActive && (!job.expirationDate || Date.parse(job.expirationDate) > Date.now());
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
        next: profile => this.isCandidate.set(profile.roles.includes('Candidate')),
        error: () => this.isCandidate.set(false),
      });
    }
    const id = this.route.snapshot.paramMap.get('id');

    if (!id) {
      this.error.set('Nevalidan ID oglasa.');
      this.loading.set(false);
      return;
    }

    this.jobService.getById(id).subscribe({
      next: (data) => {
        this.job.set(data);
        this.loading.set(false);
      },
      error: (err) => {
        this.error.set('Oglas nije pronađen.');
        this.loading.set(false);
        console.error(err);
      }
    });

    this.jobService.getBookmarks(this.currentUser.getUserId()).subscribe({
      next: (bookmarks) => {
        this.bookmarked.set(bookmarks.some((b) => b.id === id));
      },
      error: (err) => console.error(err),
    });
  }

  toggleBookmark(): void {
    const job = this.job();
    if (!job) return;

    const userId = this.currentUser.getUserId();

    if (this.bookmarked()) {
      this.jobService.removeBookmark(userId, job.id).subscribe({
        next: () => this.bookmarked.set(false),
        error: (err) => console.error(err),
      });
    } else {
      this.jobService.addBookmark(userId, job.id).subscribe({
        next: () => this.bookmarked.set(true),
        error: (err) => console.error(err),
      });
    }
  }

  checkMatch(): void {
    const job = this.job();
    if (!job) return;

    this.matchLoading.set(true);
    this.matchError.set(null);
    this.matchResult.set(null);

    this.jobService.getMatch(job.id, this.currentUser.getUserId()).subscribe({
      next: (result) => {
        this.matchResult.set(result);
        this.matchLoading.set(false);
      },
      error: (err) => {
        this.matchError.set('Podaci o profilu trenutno nisu dostupni.');
        this.matchLoading.set(false);
        console.error(err);
      },
    });
  }
}
