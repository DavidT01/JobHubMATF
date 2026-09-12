import { Component, OnInit, inject, signal } from '@angular/core';
import { CommonModule } from '@angular/common';
import { Router } from '@angular/router';
import { Job } from '../../models/job.model';
import { JobService } from '../../services/job.service';
import { CurrentUser } from '../../core/current-user';
import { MatCardModule } from '@angular/material/card';
import { MatChipsModule } from '@angular/material/chips';
import { MatButtonModule } from '@angular/material/button';
import { MatIconModule } from '@angular/material/icon';
import { MatProgressSpinnerModule } from '@angular/material/progress-spinner';

@Component({
  selector: 'app-saved-jobs',
  imports: [
    CommonModule,
    MatCardModule,
    MatChipsModule,
    MatButtonModule,
    MatIconModule,
    MatProgressSpinnerModule,
  ],
  templateUrl: './saved-jobs.html',
  styleUrl: './saved-jobs.scss',
})
export class SavedJobs implements OnInit {
  private jobService = inject(JobService);
  private currentUser = inject(CurrentUser);
  private router = inject(Router);

  jobs = signal<Job[]>([]);
  loading = signal(true);
  error = signal<string | null>(null);

  ngOnInit(): void {
    this.load();
  }

  private load(): void {
    this.loading.set(true);
    this.jobService.getBookmarks(this.currentUser.getUserId()).subscribe({
      next: (data) => {
        this.jobs.set(data);
        this.loading.set(false);
      },
      error: (err) => {
        this.error.set('Greška pri učitavanju sačuvanih oglasa.');
        this.loading.set(false);
        console.error(err);
      },
    });
  }

  removeBookmark(event: Event, jobId: string): void {
    event.stopPropagation();
    const userId = this.currentUser.getUserId();

    this.jobService.removeBookmark(userId, jobId).subscribe({
      next: () => {
        this.jobs.set(this.jobs().filter((j) => j.id !== jobId));
      },
      error: (err) => console.error(err),
    });
  }

  openDetails(id: string): void {
    this.router.navigate(['/jobs', id]);
  }
}
