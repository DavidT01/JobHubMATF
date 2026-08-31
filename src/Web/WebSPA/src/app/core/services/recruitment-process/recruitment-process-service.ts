import { Injectable } from '@angular/core';
import { HttpClient } from '@angular/common/http';
import { Observable } from 'rxjs';
import { environment } from '../../../../environments/environment';

import { RecruitmentProcessDto } from '../../models/recruitment-process-dto';
import { SelectionRoundDto } from '../../models/selection-round-dto';
import { CreateProcessCommandDto } from '../../models/create-process-command-dto';
import { InterviewScheduleDto } from '../../models/interview-schedule-dto';
import { ScheduleInterviewCommand } from '../../models/schedule-interview-command';

@Injectable({
  providedIn: 'root',
})
export class RecruitmentProcessService {
  private apiUrl = `${environment.apiUrl}/Recruitment`;
  private interviewUrl = `${environment.apiUrl}/Interview`;

  constructor(private http: HttpClient) { }

  getProcessByJobId(jobId: string): Observable<RecruitmentProcessDto> {
    return this.http.get<RecruitmentProcessDto>(`${this.apiUrl}/job/${jobId}`);
  }

  createProcess(command: CreateProcessCommandDto): Observable<string> {
    return this.http.post<string>(this.apiUrl, command);
  }

  updateRounds(processId: string, rounds: SelectionRoundDto[]): Observable<void> {
    return this.http.put<void>(`${this.apiUrl}/${processId}/rounds`, rounds);
  }

  scheduleInterview(command: ScheduleInterviewCommand): Observable<InterviewScheduleDto> {
    return this.http.post<InterviewScheduleDto>(`${this.interviewUrl}/schedule`, command);
  }
}
