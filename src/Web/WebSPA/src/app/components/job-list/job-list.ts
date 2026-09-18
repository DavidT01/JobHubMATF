import { Component, OnInit, inject, signal } from '@angular/core';
import { CommonModule } from '@angular/common';
import { switchMap } from 'rxjs';
import { Job } from '../../models/job.model';
import { JobService } from '../../services/job.service';
import { CurrentUser } from '../../core/current-user';
import { SessionService } from '../../core/services/session.service';
import { PageHeaderComponent } from '../../shared/page-header/page-header.component';
import { JobLabelPipe } from '../../shared/job-label.pipe';
import { MatCardModule } from '@angular/material/card';
import { MatChipsModule } from '@angular/material/chips';
import { MatProgressSpinnerModule } from '@angular/material/progress-spinner';
import { MatButtonModule } from '@angular/material/button';
import { MatIconModule } from '@angular/material/icon';
import { Router, RouterLink } from '@angular/router';


@Component({
  selector: 'app-job-list',
  imports: [CommonModule , MatCardModule , MatChipsModule , MatProgressSpinnerModule , MatButtonModule , MatIconModule , RouterLink,
    PageHeaderComponent, JobLabelPipe],
  templateUrl: './job-list.html',
  styleUrl: './job-list.scss',
})
export class JobList implements OnInit {
  private jobService = inject(JobService);
  private currentUser = inject(CurrentUser);
  private router = inject(Router);
  protected readonly session = inject(SessionService);


  jobs = signal<Job[]>([]);
  loading = signal(true);
  error = signal<string | null>(null);
  bookmarkedIds = signal<Set<string>>(new Set());

  ngOnInit(): void {
    this.jobService.getAll().subscribe({
      next: (data) => {
        this.jobs.set(data);
        this.loading.set(false);
      },
      error: (err) => {
        this.error.set('Could not load job openings.');
        this.loading.set(false);
        console.error(err);
      }
    });

    this.currentUser.getUserId().pipe(
      switchMap((userId) => this.jobService.getBookmarks(userId))
    ).subscribe({
      next: (bookmarks) => {
        this.bookmarkedIds.set(new Set(bookmarks.map((b) => b.id)));
      },
      error: (err) => console.error(err),
    });
  }

  isBookmarked(jobId: string): boolean {
    return this.bookmarkedIds().has(jobId);
  }

  toggleBookmark(event: Event, jobId: string): void {
    event.stopPropagation();
    const wasBookmarked = this.isBookmarked(jobId);

    this.currentUser.getUserId().pipe(
      switchMap((userId) =>
        wasBookmarked
          ? this.jobService.removeBookmark(userId, jobId)
          : this.jobService.addBookmark(userId, jobId)
      )
    ).subscribe({
      next: () => {
        const updated = new Set(this.bookmarkedIds());
        if (wasBookmarked) {
          updated.delete(jobId);
        } else {
          updated.add(jobId);
        }
        this.bookmarkedIds.set(updated);
      },
      error: (err) => console.error(err),
    });
  }

  openDetails(id: string): void {
    this.router.navigate(['/jobs', id]);
  }
}
