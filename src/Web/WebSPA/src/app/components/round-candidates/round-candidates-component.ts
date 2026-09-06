import { Component, inject, OnInit, signal } from '@angular/core';
import { CommonModule } from '@angular/common';
import { HttpErrorResponse } from '@angular/common/http';
import { ActivatedRoute } from '@angular/router';
import { MatButtonModule } from '@angular/material/button';
import { MatCardModule } from '@angular/material/card';
import { MatDialog, MatDialogModule } from '@angular/material/dialog';
import { finalize } from 'rxjs';

import { CandidateEvaluationDialogComponent } from '../candidate-evaluation-dialog/candidate-evaluation-dialog.component';
import { ScheduleInterviewDialogComponent } from '../schedule-interview-dialog/schedule-interview-dialog.component';
import { ConfirmDialogComponent } from '../confirm-dialog/confirm-dialog-component';
import { CandidateProgressDto } from '../../core/models/candidate-progress-dto';
import { InterviewScheduleDto } from '../../core/models/interview-schedule-dto';
import { RecruitmentProcessService } from '../../core/services/recruitment-process/recruitment-process-service';

@Component({
  selector: 'app-round-candidates',
  standalone: true,
  imports: [CommonModule, MatButtonModule, MatCardModule, MatDialogModule],
  templateUrl: './round-candidates-component.html',
  styleUrl: './round-candidates-component.scss'
})
export class RoundCandidatesComponent implements OnInit {
  private readonly route = inject(ActivatedRoute);
  private readonly recruitmentService = inject(RecruitmentProcessService);
  private readonly dialog = inject(MatDialog);

  readonly candidates = signal<CandidateProgressDto[]>([]);
  readonly schedules = signal<Record<string, InterviewScheduleDto>>({});
  readonly loading = signal(true);
  readonly error = signal<string | null>(null);
  private selectionRoundId = '';

  ngOnInit(): void {
    this.selectionRoundId = this.route.snapshot.paramMap.get('selectionRoundId') ?? '';
    if (!this.selectionRoundId) {
      this.error.set('The selection round could not be identified.');
      this.loading.set(false);
      return;
    }

    this.loadCandidates();
  }

  openScheduleInterview(candidateProfileId: string): void {
    const dialogRef = this.dialog.open(ScheduleInterviewDialogComponent, {
      width: '600px',
      data: { selectionRoundId: this.selectionRoundId, candidateProfileId }
    });

    dialogRef.afterClosed().subscribe(result => {
      if (result) this.loadInterviewSchedule(candidateProfileId);
    });
  }

  editInterview(candidateProfileId: string, schedule: InterviewScheduleDto): void {
    const dialogRef = this.dialog.open(ScheduleInterviewDialogComponent, {
      width: '600px',
      data: { selectionRoundId: this.selectionRoundId, candidateProfileId, schedule }
    });

    dialogRef.afterClosed().subscribe(result => {
      if (result) this.loadInterviewSchedule(candidateProfileId);
    });
  }

  cancelInterview(candidateProfileId: string, interviewScheduleId: string): void {
    const dialogRef = this.dialog.open(ConfirmDialogComponent, {
      data: { message: 'Cancel this interview and remove its Google Calendar event?' }
    });

    dialogRef.afterClosed().subscribe(confirmed => {
      if (!confirmed) return;

      this.recruitmentService.cancelInterviewSchedule(interviewScheduleId).subscribe({
        next: () => this.schedules.update(schedules => {
          const { [candidateProfileId]: _, ...remainingSchedules } = schedules;
          return remainingSchedules;
        }),
        error: () => this.error.set('Could not cancel the interview.')
      });
    });
  }

  openCandidateEvaluation(candidateProfileId: string): void {
    this.recruitmentService.getCandidateEvaluations(candidateProfileId).subscribe({
      next: evaluations => {
        const existingEvaluation = evaluations.find(evaluation => evaluation.selectionRoundId === this.selectionRoundId) ?? null;
        this.dialog.open(CandidateEvaluationDialogComponent, {
          width: '600px',
          data: { selectionRoundId: this.selectionRoundId, candidateProfileId, existingEvaluation }
        });
      },
      error: () => this.error.set('Could not load the candidate evaluation.')
    });
  }

  rejectCandidate(candidate: CandidateProgressDto): void {
    this.recruitmentService.rejectCandidate({
      candidateProfileId: candidate.candidateProfileId,
      recruitmentProcessId: candidate.recruitmentProcessId
    }).subscribe({
      next: progress => this.replaceCandidate(progress),
      error: () => this.error.set('Could not reject the candidate.')
    });
  }

  hireCandidate(candidate: CandidateProgressDto): void {
    this.recruitmentService.hireCandidate({
      candidateProfileId: candidate.candidateProfileId,
      recruitmentProcessId: candidate.recruitmentProcessId
    }).subscribe({
      next: progress => this.replaceCandidate(progress),
      error: () => this.error.set('Could not mark the candidate as hired.')
    });
  }

  private loadCandidates(): void {
    this.loading.set(true);
    this.error.set(null);

    this.recruitmentService.getCandidatesInRound(this.selectionRoundId).pipe(
      finalize(() => this.loading.set(false))
    ).subscribe({
      next: candidates => {
        this.candidates.set(candidates);
        this.schedules.set({});
        candidates.forEach(candidate => this.loadInterviewSchedule(candidate.candidateProfileId));
      },
      error: () => this.error.set('Could not load candidates for this selection round.')
    });
  }

  private loadInterviewSchedule(candidateProfileId: string): void {
    this.recruitmentService.getInterviewSchedule({ candidateProfileId, selectionRoundId: this.selectionRoundId }).subscribe({
      next: schedule => this.schedules.update(schedules => ({ ...schedules, [candidateProfileId]: schedule })),
      error: (error: unknown) => {
        if (!(error instanceof HttpErrorResponse) || error.status !== 404) {
          this.error.set('Could not load the scheduled interview.');
        }
      }
    });
  }

  private replaceCandidate(progress: CandidateProgressDto): void {
    this.candidates.update(candidates => candidates.map(candidate =>
      candidate.id === progress.id ? progress : candidate));
  }
}
