import { Component, inject, OnInit, signal } from '@angular/core';
import { CommonModule } from '@angular/common';
import { ActivatedRoute, Router } from '@angular/router';
import { MatButtonModule } from '@angular/material/button';
import { MatCardModule } from '@angular/material/card';
import { MatDialog, MatDialogModule } from '@angular/material/dialog';
import { RoundEditorDialogComponent } from '../round-editor-dialog/round-editor-dialog.component';

import { RecruitmentProcessService } from '../../core/services/recruitment-process/recruitment-process-service';
import { RecruitmentProcessDto } from '../../core/models/recruitment-process-dto';
import { ScheduleInterviewDialogComponent } from '../schedule-interview-dialog/schedule-interview-dialog.component';

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
  candidateProfileId: string = '00000000-0000-0000-0000-000000000000'; // Dummy ID for schedule testing

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
    
    // Inject Dummy Data for testing Schedule Interview
    this.process.set({
      id: 'd9b23b37-6490-410a-8bf8-d6f7c1664eeb',
      companyId: 'company-guid',
      jobId: this.jobId,
      isActive: true,
      rounds: [
        { id: '2cc96de1-2a62-4217-bf41-11dbeeec5f79', title: 'HR Screening', description: 'Initial HR phone call', orderIndex: 1 },
        { id: 'ca1b16c1-fffe-443b-ab29-fbeceec8a11e', title: 'Technical Interview', description: 'Live coding and technical questions', orderIndex: 2 }
      ]
    });
    this.loading.set(false);
    return;
    // ---
    
    this.recruitmentService.getProcessByJobId(this.jobId).subscribe({
      next: (data) => {
        this.process.set(data);
        this.loading.set(false);
      },
      error: (err) => {
        if (err.status === 404) {
          this.process.set(null);
        } else {
          console.error('Error fetching process', err);
        }
        this.loading.set(false);
      }
    });
  }

  initializeProcess(): void {
    const command = { companyId: this.companyId, jobId: this.jobId };
    this.recruitmentService.createProcess(command).subscribe({
      next: () => {
        this.loadProcess();
      },
      error: (err) => console.error('Failed to create process', err)
    });
  }

  editRounds(): void {
    const p = this.process();
    if (!p)
      return;

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

  scheduleInterview(roundId?: string): void {
    if (!roundId) return;

    const dialogRef = this.dialog.open(ScheduleInterviewDialogComponent, {
      width: '600px',
      data: { 
        selectionRoundId: roundId, 
        candidateProfileId: this.candidateProfileId
      }
    });

    dialogRef.afterClosed().subscribe(result => {
      if (result) {
         console.log('Interview scheduled:', result);
         if (result.googleMeetUrl) {
            window.open(result.googleMeetUrl, '_blank');
         }
      }
    });
  }
}
