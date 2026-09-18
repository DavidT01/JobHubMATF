import { Component, inject, OnInit, signal } from '@angular/core';
import { HttpErrorResponse } from '@angular/common/http';
import { ActivatedRoute, Router, RouterLink } from '@angular/router';
import { FormBuilder, ReactiveFormsModule, Validators } from '@angular/forms';
import { MatFormFieldModule } from '@angular/material/form-field';
import { MatInputModule } from '@angular/material/input';
import { MatSelectModule } from '@angular/material/select';
import { MatButtonModule } from '@angular/material/button';
import { MatCardModule } from '@angular/material/card';
import { Job, JobType, ExperienceLevel, WorkMode } from '../../models/job.model';
import { JobService } from '../../services/job.service';
import { PageHeaderComponent } from '../../shared/page-header/page-header.component';
import { JobLabelPipe } from '../../shared/job-label.pipe';
import { CompanyProfileService } from '../../core/services/company-profile/company-profile-service';
import { CompanyProfileDto } from '../../core/models/company-profile-dto';

@Component({
  selector: 'app-job-create',
  imports: [
    ReactiveFormsModule,
    MatFormFieldModule,
    MatInputModule,
    MatSelectModule,
    MatButtonModule,
    MatCardModule,
    PageHeaderComponent,
    JobLabelPipe,
    RouterLink,
  ],
  templateUrl: './job-create.html',
  styleUrl: './job-create.scss',
})
export class JobCreate implements OnInit {
  private fb = inject(FormBuilder);
  private jobService = inject(JobService);
  private companyProfiles = inject(CompanyProfileService);
  private route = inject(ActivatedRoute);
  private router = inject(Router);

  jobTypes: JobType[] = ['FullTime', 'PartTime', 'Contract', 'Internship'];
  experienceLevels: ExperienceLevel[] = ['Junior', 'Mid', 'Senior', 'Lead'];
  workModes: WorkMode[] = ['OnSite', 'Hybrid', 'Remote'];

  submitting = signal(false);
  error = signal<string | null>(null);

  /** The company the job is posted under; it always comes from the signed-in employer's profile. */
  company = signal<CompanyProfileDto | null>(null);
  companyMissing = signal(false);
  loading = signal(true);

  /** Set when the form edits an existing job instead of posting a new one. */
  editedJob = signal<Job | null>(null);

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

  ngOnInit(): void {
    this.companyProfiles.getMine().subscribe({
      next: company => {
        this.company.set(company);
        this.form.patchValue({ companyId: company.id, companyName: company.companyName });

        const jobId = this.route.snapshot.paramMap.get('id');
        if (jobId) {
          this.loadJob(jobId, company);
        } else {
          this.loading.set(false);
        }
      },
      error: (err: HttpErrorResponse) => {
        this.loading.set(false);
        if (err.status === 404) {
          this.companyMissing.set(true);
        } else {
          this.error.set('Your company profile could not be loaded. Please try again.');
        }
      },
    });
  }

  private loadJob(jobId: string, company: CompanyProfileDto): void {
    this.jobService.getById(jobId).subscribe({
      next: job => {
        // Only the company that posted a job may change it.
        if (job.companyId !== company.id) {
          this.router.navigate(['/jobs', jobId]);
          return;
        }
        this.editedJob.set(job);
        this.form.patchValue({
          ...job,
          skills: job.skills?.join(', ') ?? '',
          requirements: job.requirements?.join(', ') ?? '',
          responsibilities: job.responsibilities?.join(', ') ?? '',
        });
        this.loading.set(false);
      },
      error: () => {
        this.loading.set(false);
        this.error.set('The job could not be loaded.');
      },
    });
  }

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

    const edited = this.editedJob();
    const request = edited
      ? this.jobService.update(edited.id, { ...edited, ...job })
      : this.jobService.create(job);

    request.subscribe({
      next: (saved) => {
        this.submitting.set(false);
        this.router.navigate(['/jobs', saved?.id ?? edited?.id]);
      },
      error: (err) => {
        this.submitting.set(false);
        this.error.set(edited
          ? 'The job could not be saved. Please try again.'
          : 'The job could not be posted. Please try again.');
        console.error(err);
      },
    });
  }
}
