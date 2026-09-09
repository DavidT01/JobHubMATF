import { Component, inject, signal } from '@angular/core';
import { CommonModule } from '@angular/common';
import { HttpErrorResponse } from '@angular/common/http';
import { MatDialogRef, MAT_DIALOG_DATA, MatDialogModule } from '@angular/material/dialog';
import { MatButtonModule } from '@angular/material/button';
import { FormBuilder, FormGroup, ReactiveFormsModule, Validators } from '@angular/forms';
import { MatFormFieldModule } from '@angular/material/form-field';
import { MatInputModule } from '@angular/material/input';
import { finalize } from 'rxjs';
import { RecruitmentProcessService } from '../../core/services/recruitment-process/recruitment-process-service';
import { EvaluateCandidateCommand } from '../../core/models/evaluate-candidate-command';
import { MatSliderModule } from '@angular/material/slider';

@Component({
  selector: 'app-candidate-evaluation-dialog',
  standalone: true,
  imports: [
    CommonModule, 
    ReactiveFormsModule, 
    MatDialogModule, 
    MatButtonModule, 
    MatFormFieldModule, 
    MatInputModule,
    MatSliderModule
  ],
  templateUrl: './candidate-evaluation-dialog.component.html',
  styleUrls: ['./candidate-evaluation-dialog.component.scss']
})
export class CandidateEvaluationDialogComponent {
  private fb = inject(FormBuilder);
  private dialogRef = inject(MatDialogRef<CandidateEvaluationDialogComponent>);
  private recruitmentService = inject(RecruitmentProcessService);
  public data: { selectionRoundId: string, candidateProfileId: string, existingEvaluation: any } = inject(MAT_DIALOG_DATA);

  form!: FormGroup;
  loading = signal<boolean>(false);
  error = signal<string | null>(null);

  constructor() {
    this.form = this.fb.group({
      score: [this.data.existingEvaluation?.score || 5, [Validators.required, Validators.min(1), Validators.max(10)]],
      notes: [this.data.existingEvaluation?.notes || '']
    });
  }

  save() {
    if (this.form.invalid) return;
    this.loading.set(true);
    this.error.set(null);

    const formValue = this.form.value;

    const command: EvaluateCandidateCommand = {
      selectionRoundId: this.data.selectionRoundId,
      candidateProfileId: this.data.candidateProfileId,
      score: formValue.score,
      notes: formValue.notes
    };

    this.recruitmentService.evaluateCandidate(command).pipe(
      finalize(() => this.loading.set(false))
    ).subscribe({
      next: result => this.dialogRef.close(result),
      error: (error: unknown) => this.error.set(this.resolveErrorMessage(error))
    });
  }

  private resolveErrorMessage(error: unknown): string {
    if (error instanceof HttpErrorResponse) {
      switch (error.status) {
        case 400: return 'The candidate profile or selection round could not be found.';
        case 0: return 'We could not confirm whether the evaluation was saved. Please check before retrying.';
      }
    }
    return 'Could not save the evaluation. Please try again.';
  }
}
