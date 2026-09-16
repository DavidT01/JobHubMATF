import { Component, inject, signal } from '@angular/core';
import { CommonModule } from '@angular/common';
import { FormsModule } from '@angular/forms';
import { Router } from '@angular/router';
import { MatFormFieldModule } from '@angular/material/form-field';
import { MatInputModule } from '@angular/material/input';
import { MatButtonModule } from '@angular/material/button';
import { MatCardModule } from '@angular/material/card';
import { MatChipsModule } from '@angular/material/chips';
import { CandidateProfile } from '../../models/candidate.model';
import { JobService } from '../../services/job.service';
import { PageHeaderComponent } from '../../shared/page-header/page-header.component';

@Component({
  selector: 'app-candidate-search',
  imports: [
    CommonModule,
    FormsModule,
    MatFormFieldModule,
    MatInputModule,
    MatButtonModule,
    MatCardModule,
    MatChipsModule,
    PageHeaderComponent,
  ],
  templateUrl: './candidate-search.html',
  styleUrl: './candidate-search.scss',
})
export class CandidateSearch {
  private jobService = inject(JobService);
  private router = inject(Router);

  skills = signal('');
  location = signal('');

  results = signal<CandidateProfile[]>([]);
  loading = signal(false);
  error = signal<string | null>(null);
  searched = signal(false);

  onSearch(): void {
    this.loading.set(true);
    this.error.set(null);
    this.searched.set(true);

    const skillsList = this.skills()
      .split(',')
      .map((s) => s.trim())
      .filter((s) => s.length > 0);

    this.jobService.searchCandidates(skillsList, this.location().trim() || undefined).subscribe({
      next: (data) => {
        this.results.set(data);
        this.loading.set(false);
      },
      error: (err) => {
        this.error.set('Could not search candidates. Please try again.');
        this.loading.set(false);
        console.error(err);
      },
    });
  }

  yearsOfExperience(candidate: CandidateProfile): number | null {
    if (candidate.experience.length === 0) return null;

    const starts = candidate.experience.map((e) => new Date(e.startDate).getTime());
    const ends = candidate.experience.map((e) => (e.endDate ? new Date(e.endDate).getTime() : Date.now()));

    const earliestStart = Math.min(...starts);
    const latestEnd = Math.max(...ends);

    return Math.round((latestEnd - earliestStart) / (1000 * 60 * 60 * 24 * 365.25));
  }

  openProfile(userId: string): void {
    this.router.navigate(['/profile/candidate', userId]);
  }
}
