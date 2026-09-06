import { Component, OnInit, inject, signal } from '@angular/core';
import { CommonModule } from '@angular/common';
import { Job } from '../../models/job.model';
import { JobService } from '../../services/job.service';
import { CurrentUser } from '../../core/current-user';
import { MatCardModule } from '@angular/material/card';
import { MatChipsModule } from '@angular/material/chips';
import { MatProgressSpinnerModule } from '@angular/material/progress-spinner';
import { MatButtonModule } from '@angular/material/button';
import { MatIconModule } from '@angular/material/icon';
import { Router, RouterLink } from '@angular/router';


@Component({
  selector: 'app-job-list',
  imports: [CommonModule , MatCardModule , MatChipsModule , MatProgressSpinnerModule , MatButtonModule , MatIconModule , RouterLink],
  templateUrl: './job-list.html',
  styleUrl: './job-list.scss',
})
export class JobList implements OnInit {
  private jobService = inject(JobService);
  private currentUser = inject(CurrentUser);
  private router = inject(Router);


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
        this.error.set('Greška pri učitavanju oglasa.');
        this.loading.set(false);
        console.error(err);
      }
    });

    this.jobService.getBookmarks(this.currentUser.getUserId()).subscribe({
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
    const userId = this.currentUser.getUserId();

    if (this.isBookmarked(jobId)) {
      this.jobService.removeBookmark(userId, jobId).subscribe({
        next: () => {
          const updated = new Set(this.bookmarkedIds());
          updated.delete(jobId);
          this.bookmarkedIds.set(updated);
        },
        error: (err) => console.error(err),
      });
    } else {
      this.jobService.addBookmark(userId, jobId).subscribe({
        next: () => {
          const updated = new Set(this.bookmarkedIds());
          updated.add(jobId);
          this.bookmarkedIds.set(updated);
        },
        error: (err) => console.error(err),
      });
    }
  }

  openDetails(id: string): void {
    this.router.navigate(['/jobs', id]);
  }

  openCreate(): void {
    this.router.navigate(['/jobs/new']);
  }
}