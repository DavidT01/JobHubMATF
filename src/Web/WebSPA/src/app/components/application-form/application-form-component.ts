import { HttpErrorResponse } from '@angular/common/http';
import { ChangeDetectionStrategy, Component, DestroyRef, inject, input, OnChanges, output, signal } from '@angular/core';
import { takeUntilDestroyed } from '@angular/core/rxjs-interop';
import { FormControl, FormGroup, ReactiveFormsModule } from '@angular/forms';
import { MatButtonModule } from '@angular/material/button';
import { MatCardModule } from '@angular/material/card';
import { MatFormFieldModule } from '@angular/material/form-field';
import { MatInputModule } from '@angular/material/input';
import { MatProgressBarModule } from '@angular/material/progress-bar';
import { finalize, Subscription } from 'rxjs';

import { ApplicationListItemDto } from '../../core/models/application-list-item-dto';
import { ApplicationsService } from '../../core/services/applications/applications-service';

@Component({
  selector: 'app-application-form',
  imports: [ReactiveFormsModule, MatButtonModule, MatCardModule, MatFormFieldModule, MatInputModule, MatProgressBarModule],
  templateUrl: './application-form-component.html',
  styleUrl: './application-form-component.scss',
  changeDetection: ChangeDetectionStrategy.OnPush,
})
export class ApplicationFormComponent implements OnChanges {
  readonly jobId = input.required<string>();
  readonly applied = output<ApplicationListItemDto>();
  private readonly applications = inject(ApplicationsService);
  private readonly destroyRef = inject(DestroyRef);
  private request?: Subscription;

  protected readonly sending = signal(false);
  protected readonly result = signal<ApplicationListItemDto | null>(null);
  protected readonly message = signal<string | null>(null);
  protected readonly form = new FormGroup({
    coverLetter: new FormControl('', {
      nonNullable: true,
      validators: [control => (control.value as string).trim().length > 5000 ? { maxlength: true } : null],
    }),
  });

  ngOnChanges(): void {
    this.request?.unsubscribe();
    this.form.reset();
    this.result.set(null);
    this.message.set(null);
  }

  protected validJob(): boolean {
    return /^[a-fA-F0-9]{24}$/.test(this.jobId());
  }

  protected submit(): void {
    if (this.sending() || this.result()) return;
    this.form.markAllAsTouched();
    if (!this.validJob() || this.form.invalid) return;

    const coverLetter = this.form.controls.coverLetter.value.trim();
    this.message.set(null);
    this.sending.set(true);
    this.form.disable({ emitEvent: false });
    this.request = this.applications.submitApplication({
      jobId: this.jobId().toLowerCase(), coverLetter: coverLetter || null,
    }).pipe(
      takeUntilDestroyed(this.destroyRef),
      finalize(() => {
        this.sending.set(false);
        this.form.enable({ emitEvent: false });
      }),
    ).subscribe({
      next: application => {
        this.result.set(application);
        this.applied.emit(application);
      },
      error: (error: unknown) => this.message.set(this.errorMessage(error)),
    });
  }

  private errorMessage(error: unknown): string {
    if (error instanceof HttpErrorResponse) {
      switch (error.status) {
        case 400:
          if (error.error?.errors?.cv) return 'Upload a CV to your candidate profile before applying.';
          return 'Please check the job and cover letter, then try again.';
        case 401: return 'Please sign in again before applying.';
        case 403: return 'Only candidates can submit job applications.';
        case 404: return 'The job or your candidate profile could not be found.';
        case 409: return 'You may already have applied, or this job is no longer accepting applications. Check your applications before trying again.';
        case 503: return 'A required service is temporarily unavailable. Please try again later.';
        case 0: return 'We could not confirm whether your application was received. Check your applications before trying again.';
      }
    }
    return 'We could not confirm the application result. Check your applications before trying again.';
  }
}
