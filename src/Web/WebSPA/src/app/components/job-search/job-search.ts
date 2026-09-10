import { Component , inject , signal } from '@angular/core';
import { CommonModule } from '@angular/common';
import { FormsModule } from '@angular/forms';
import { MatFormFieldModule } from '@angular/material/form-field';
import { MatInputModule } from '@angular/material/input';
import { MatButtonModule } from '@angular/material/button';
import { MatSelectModule } from '@angular/material/select';
import { MatCardModule } from '@angular/material/card';
import { MatChipsModule } from '@angular/material/chips';
import { Job, JobType, ExperienceLevel, WorkMode } from '../../models/job.model';
import { JobService } from '../../services/job.service';
import { Router } from '@angular/router';


@Component({
  selector: 'app-job-search',
  imports: [
    CommonModule,
    FormsModule,
    MatFormFieldModule,
    MatInputModule,
    MatButtonModule,
    MatSelectModule,
    MatCardModule,
    MatChipsModule
  ],
  templateUrl: './job-search.html',
  styleUrl: './job-search.scss',
})
export class JobSearch {
  private jobService = inject(JobService);
  private router = inject(Router);

  query = signal('');
  jobType = signal<JobType | ''>('');
  experienceLevel = signal<ExperienceLevel | ''>('');
  workMode = signal<WorkMode | ''>('');
  city = signal('');

  results = signal<Job[]>([]);
  loading = signal(false);
  searched = signal(false);

  jobTypes: JobType[] = ['FullTime', 'PartTime', 'Contract', 'Internship'];
  experienceLevels: ExperienceLevel[] = ['Junior', 'Mid', 'Senior', 'Lead'];
  workModes: WorkMode[] = ['OnSite', 'Hybrid', 'Remote'];

  onSearch(): void {
    this.loading.set(true);
    this.searched.set(true);

    const trimmedQuery = this.query().trim();

    const request$ = trimmedQuery
      ? this.jobService.search(trimmedQuery)
      : this.jobService.filter(
          this.jobType() || undefined,
          this.experienceLevel() || undefined,
          this.workMode() || undefined,
          this.city().trim() || undefined
        );

    request$.subscribe({
      next: (data) => {
        this.results.set(data);
        this.loading.set(false);
      },
      error: (err) => {
        this.loading.set(false);
        console.error(err);
      }
    });
  }

  onReset(): void {
    this.query.set('');
    this.jobType.set('');
    this.experienceLevel.set('');
    this.workMode.set('');
    this.city.set('');
    this.results.set([]);
    this.searched.set(false);
  }

  openDetails(id: string): void {
    this.router.navigate(['/jobs', id]);
  }
}
