import { Component, OnInit, inject, signal } from '@angular/core';
import { CommonModule } from '@angular/common';
import { Job } from '../../models/job.model';
import { JobService } from '../../services/job.service';
import { MatCardModule } from '@angular/material/card';
import { MatChipsModule } from '@angular/material/chips';
import { MatProgressSpinnerModule } from '@angular/material/progress-spinner';
import { Router } from '@angular/router';


@Component({
  selector: 'app-job-list',
  imports: [CommonModule , MatCardModule , MatChipsModule , MatProgressSpinnerModule],
  templateUrl: './job-list.html',
  styleUrl: './job-list.scss',
})
export class JobList implements OnInit {
  private jobService = inject(JobService);
  private router = inject(Router);


  jobs = signal<Job[]>([]);
  loading = signal(true);
  error = signal<string | null>(null);

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
  }

  openDetails(id: string): void {
    this.router.navigate(['/jobs', id]);
  }
}