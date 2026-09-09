import { Component, inject, signal } from '@angular/core';
import { Router } from '@angular/router';
import { FormBuilder, ReactiveFormsModule, Validators } from '@angular/forms';
import { MatFormFieldModule } from '@angular/material/form-field';
import { MatInputModule } from '@angular/material/input';
import { MatSelectModule } from '@angular/material/select';
import { MatButtonModule } from '@angular/material/button';
import { MatCardModule } from '@angular/material/card';
import { Job, JobType, ExperienceLevel, WorkMode } from '../../models/job.model';
import { JobService } from '../../services/job.service';

@Component({
  selector: 'app-job-create',
  imports: [
    ReactiveFormsModule,
    MatFormFieldModule,
    MatInputModule,
    MatSelectModule,
    MatButtonModule,
    MatCardModule,
  ],
  templateUrl: './job-create.html',
  styleUrl: './job-create.scss',
})
export class JobCreate {
  private fb = inject(FormBuilder);
  private jobService = inject(JobService);
  private router = inject(Router);

  jobTypes: JobType[] = ['FullTime', 'PartTime', 'Contract', 'Internship'];
  experienceLevels: ExperienceLevel[] = ['Junior', 'Mid', 'Senior', 'Lead'];
  workModes: WorkMode[] = ['OnSite', 'Hybrid', 'Remote'];

  submitting = signal(false);
  error = signal<string | null>(null);

  form = this.fb.group({
    title: ['', Validators.required],
    description: ['', Validators.required],
    jobType: ['FullTime' as JobType, Validators.required],
    experienceLevel: ['Junior' as ExperienceLevel, Validators.required],
    workMode: ['OnSite' as WorkMode, Validators.required],
    companyId: ['', Validators.required],
    companyName: ['', Validators.required],
    applyUrl: [''],
    contactEmail: [''],
    salaryMin: [null as number | null],
    salaryMax: [null as number | null],
    currency: ['EUR'],
    skills: [''],
    requirements: [''],
    responsibilities: [''],
    yearsOfExperience: [null as number | null],
    educationLevel: [''],
    city: [''],
    country: [''],
  });

  private toList(value: string | null): string[] {
    return (value ?? '')
      .split(',')
      .map((s) => s.trim())
      .filter((s) => s.length > 0);
  }

  submit(): void {
    if (this.form.invalid) {
      this.form.markAllAsTouched();
      return;
    }

    const raw = this.form.getRawValue();

    const job: Partial<Job> = {
      title: raw.title!,
      description: raw.description!,
      jobType: raw.jobType!,
      experienceLevel: raw.experienceLevel!,
      workMode: raw.workMode!,
      companyId: raw.companyId!,
      companyName: raw.companyName!,
      applyUrl: raw.applyUrl || undefined,
      contactEmail: raw.contactEmail || undefined,
      salaryMin: raw.salaryMin ?? undefined,
      salaryMax: raw.salaryMax ?? undefined,
      currency: raw.currency || 'EUR',
      skills: this.toList(raw.skills),
      requirements: this.toList(raw.requirements),
      responsibilities: this.toList(raw.responsibilities),
      yearsOfExperience: raw.yearsOfExperience ?? undefined,
      educationLevel: raw.educationLevel || undefined,
      city: raw.city || undefined,
      country: raw.country || undefined,
    };

    this.submitting.set(true);
    this.error.set(null);

    this.jobService.create(job).subscribe({
      next: (created) => {
        this.submitting.set(false);
        this.router.navigate(['/jobs', created.id]);
      },
      error: (err) => {
        this.submitting.set(false);
        this.error.set('Greška pri kreiranju oglasa.');
        console.error(err);
      },
    });
  }
}
