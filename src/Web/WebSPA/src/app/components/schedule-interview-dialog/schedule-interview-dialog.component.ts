import { Component, inject, signal } from '@angular/core';
import { CommonModule } from '@angular/common';
import { MatDialogRef, MAT_DIALOG_DATA, MatDialogModule } from '@angular/material/dialog';
import { MatButtonModule } from '@angular/material/button';
import { FormBuilder, FormGroup, ReactiveFormsModule, Validators } from '@angular/forms';
import { MatFormFieldModule } from '@angular/material/form-field';
import { MatInputModule } from '@angular/material/input';
import { MatDatepickerModule } from '@angular/material/datepicker';
import { MatNativeDateModule, provideNativeDateAdapter } from '@angular/material/core';
import { RecruitmentProcessService } from '../../core/services/recruitment-process/recruitment-process-service';
import { ScheduleInterviewCommand } from '../../core/models/schedule-interview-command';

@Component({
  selector: 'app-schedule-interview-dialog',
  standalone: true,
  imports: [
    CommonModule, 
    ReactiveFormsModule, 
    MatDialogModule, 
    MatButtonModule, 
    MatFormFieldModule, 
    MatInputModule,
    MatDatepickerModule,
    MatNativeDateModule
  ],
  providers: [provideNativeDateAdapter()],
  templateUrl: './schedule-interview-dialog.component.html',
  styleUrls: ['./schedule-interview-dialog.component.scss']
})
export class ScheduleInterviewDialogComponent {
  private fb = inject(FormBuilder);
  private dialogRef = inject(MatDialogRef<ScheduleInterviewDialogComponent>);
  private recruitmentService = inject(RecruitmentProcessService);
  public data: { selectionRoundId: string, candidateProfileId: string } = inject(MAT_DIALOG_DATA);

  form!: FormGroup;
  loading = signal<boolean>(false);

  constructor() {
    this.form = this.fb.group({
      title: ['', Validators.required],
      description: [''],
      startDate: [null, Validators.required],
      startTime: [null, Validators.required],
      endDate: [null, Validators.required],
      endTime: [null, Validators.required],
      attendeeEmails: ['', Validators.required]
    });
  }

  // helper to combine date and time strings into a datetime object
  private parseDateTime(date: Date, timeStr: string): Date {
    const d = new Date(date);
    const [hours, minutes] = timeStr.split(':');
    d.setHours(parseInt(hours, 10), parseInt(minutes, 10));
    return d;
  }

  save() {
    if (this.form.invalid) return;
    this.loading.set(true);

    const formValue = this.form.value;
    const emails = formValue.attendeeEmails.split(',').map((e: string) => e.trim()).filter((e: string) => e);

    const startDateTime = this.parseDateTime(formValue.startDate, formValue.startTime);
    const endDateTime = this.parseDateTime(formValue.endDate, formValue.endTime);

    const command: ScheduleInterviewCommand = {
      selectionRoundId: this.data.selectionRoundId,
      candidateProfileId: this.data.candidateProfileId,
      title: formValue.title,
      description: formValue.description,
      startTime: startDateTime,
      endTime: endDateTime,
      attendeeEmails: emails
    };

    // TODO: Call this.recruitmentService.scheduleInterview(command), close the dialog, and display errors.
  }
}
