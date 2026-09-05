import { Component, inject, OnInit, signal } from '@angular/core';
import { CommonModule } from '@angular/common';
import { HttpErrorResponse } from '@angular/common/http';
import { ActivatedRoute, Router } from '@angular/router';
import { MatButtonModule } from '@angular/material/button';
import { MatCardModule } from '@angular/material/card';
import { MatDialog, MatDialogModule } from '@angular/material/dialog';
import { finalize } from 'rxjs';
import { RoundEditorDialogComponent } from '../round-editor-dialog/round-editor-dialog.component';
import { RecruitmentProcessService } from '../../core/services/recruitment-process/recruitment-process-service';
import { RecruitmentProcessDto } from '../../core/models/recruitment-process-dto';
import { CreateProcessCommandDto } from '../../core/models/create-process-command-dto';

@Component({
  selector: 'app-recruitment-process-component',
  standalone: true,
  imports: [CommonModule, MatButtonModule, MatCardModule, MatDialogModule],
  templateUrl: './recruitment-process-component.html',
  styleUrls: ['./recruitment-process-component.scss']
})
export class RecruitmentProcessComponent implements OnInit {
  private route = inject(ActivatedRoute);
  private router = inject(Router);
  private recruitmentService = inject(RecruitmentProcessService);
  private dialog = inject(MatDialog);

  jobId: string = '';
  companyId: string = '';

  process = signal<RecruitmentProcessDto | null>(null);
  loading = signal<boolean>(true);
  error = signal<string | null>(null);

  ngOnInit(): void {
    this.jobId = this.route.snapshot.paramMap.get('jobId') || '';
    
    if (this.jobId) {
      this.loadProcess();
    } else {
      this.router.navigate(['/']);
    }
  }

  loadProcess(): void {
    this.loading.set(true);
    this.error.set(null);

    this.recruitmentService.getProcessByJobId(this.jobId).pipe(
      finalize(() => this.loading.set(false))
    ).subscribe({
      next: process => this.process.set(process),
      error: (error: unknown) => {
        this.process.set(null);
        if (!(error instanceof HttpErrorResponse) || error.status !== 404) {
          this.error.set(this.resolveErrorMessage(error));
        }
      }
    });
  }

  initializeProcess(): void {
    this.loading.set(true);
    this.error.set(null);

    const command: CreateProcessCommandDto = { companyId: this.companyId, jobId: this.jobId };

    this.recruitmentService.createProcess(command).pipe(
      finalize(() => this.loading.set(false))
    ).subscribe({
      next: () => this.loadProcess(),
      error: (error: unknown) => this.error.set(this.resolveErrorMessage(error))
    });
  }

  private resolveErrorMessage(error: unknown): string {
    if (error instanceof HttpErrorResponse) {
      switch (error.status) {
        case 400: return 'The recruitment process request is invalid.';
        case 0: return 'We could not reach the recruitment service. Please try again.';
      }
    }
    return 'Could not load the recruitment process. Please try again.';
  }

  editRounds(): void {
    const p = this.process();
    if (!p) return;

    const dialogRef = this.dialog.open(RoundEditorDialogComponent, {
      width: '600px',
      data: { processId: p.id, rounds: p.rounds }
    });

    dialogRef.afterClosed().subscribe(result => {
      if (result === true) {
        this.loadProcess();
      }
    });
  }

  viewRoundCandidates(selectionRoundId: string | undefined): void {
    if (!selectionRoundId) return;

    this.router.navigate(['recruitment-processes', this.jobId, 'rounds', selectionRoundId]);
  }
}
