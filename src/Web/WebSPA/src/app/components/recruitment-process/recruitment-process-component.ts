import { Component, inject, OnInit, signal } from '@angular/core';
import { CommonModule } from '@angular/common';
import { ActivatedRoute, Router } from '@angular/router';
import { MatButtonModule } from '@angular/material/button';
import { MatCardModule } from '@angular/material/card';
import { MatDialog, MatDialogModule } from '@angular/material/dialog';
import { RoundEditorDialogComponent } from '../round-editor-dialog/round-editor-dialog.component';
import { RecruitmentProcessService } from '../../core/services/recruitment-process/recruitment-process-service';
import { RecruitmentProcessDto } from '../../core/models/recruitment-process-dto';

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
    
    // TODO: Load the recruitment process by job ID from the Recruitment API and handle a missing process.
  }

  initializeProcess(): void {
    const command = { companyId: this.companyId, jobId: this.jobId };
    
    // TODO: Create the recruitment process through the Recruitment API and reload it after success.
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
}

