import { Component, OnInit, inject, signal } from '@angular/core';
import { CommonModule } from '@angular/common';
import { ActivatedRoute, RouterLink } from '@angular/router';
import { MatCardModule } from '@angular/material/card';
import { MatChipsModule } from '@angular/material/chips';
import { MatButtonModule } from '@angular/material/button';
import { MatProgressSpinnerModule } from '@angular/material/progress-spinner';
import { Job } from '../../models/job.model';
import { JobService } from '../../services/job.service';

@Component({
  selector: 'app-job-details',
  imports: [
    CommonModule,
    RouterLink,
    MatCardModule,
    MatChipsModule,
    MatButtonModule,
    MatProgressSpinnerModule
  ],
  templateUrl: './job-details.html',
  styleUrl: './job-details.scss',
})
export class JobDetails implements OnInit {
  private route = inject(ActivatedRoute);
  private jobService = inject(JobService);

  job = signal<Job | null>(null);
  loading = signal(true);
  error = signal<string | null>(null);

  ngOnInit(): void {
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
  }
}