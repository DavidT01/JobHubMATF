import { Injectable } from '@angular/core';
import { HttpClient } from '@angular/common/http';
import { Observable } from 'rxjs';
import { environment } from '../../../../environments/environment';

import { RecruitmentProcessDto } from '../../models/recruitment-process-dto';
import { SelectionRoundDto } from '../../models/selection-round-dto';
import { CreateProcessCommandDto } from '../../models/create-process-command-dto';
import { InterviewScheduleDto } from '../../models/interview-schedule-dto';
import { ScheduleInterviewCommand } from '../../models/schedule-interview-command';
import { CandidateEvaluationDto } from '../../models/candidate-evaluation-dto';
import { EvaluateCandidateCommand } from '../../models/evaluate-candidate-command';
import { CandidateProgressDto } from '../../models/candidate-progress-dto';
import { AdvanceCandidateCommand } from '../../models/advance-candidate-command';
import { UpdateCandidateStatusCommand } from '../../models/update-candidate-status-command';
import { GetInterviewScheduleQuery } from '../../models/get-interview-schedule-query';

@Injectable({
  providedIn: 'root',
})
export class RecruitmentProcessService {
  private apiUrl = `${environment.apiUrl}/Recruitment`;
  private interviewUrl = `${environment.apiUrl}/Interview`;
  private candidateUrl = `${environment.apiUrl}/Candidate`;

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

  evaluateCandidate(command: EvaluateCandidateCommand): Observable<CandidateEvaluationDto> {
    return this.http.post<CandidateEvaluationDto>(`${this.candidateUrl}/evaluate`, command);
  }

  advanceCandidate(command: AdvanceCandidateCommand): Observable<CandidateProgressDto> {
    return this.http.post<CandidateProgressDto>(`${this.candidateUrl}/advance`, command);
  }

  rejectCandidate(command: UpdateCandidateStatusCommand): Observable<CandidateProgressDto> {
    return this.http.post<CandidateProgressDto>(`${this.candidateUrl}/reject`, null, {
      params: {
        candidateProfileId: command.candidateProfileId,
        recruitmentProcessId: command.recruitmentProcessId
      }
    });
  }

  hireCandidate(command: UpdateCandidateStatusCommand): Observable<CandidateProgressDto> {
    return this.http.post<CandidateProgressDto>(`${this.candidateUrl}/hire`, null, {
      params: {
        candidateProfileId: command.candidateProfileId,
        recruitmentProcessId: command.recruitmentProcessId
      }
    });
  }

  getCandidatesInRound(selectionRoundId: string): Observable<CandidateProgressDto[]> {
    return this.http.get<CandidateProgressDto[]>(`${this.candidateUrl}/round/${selectionRoundId}`);
  }

  getCandidateEvaluations(candidateId: string): Observable<CandidateEvaluationDto[]> {
    return this.http.get<CandidateEvaluationDto[]>(`${this.candidateUrl}/${candidateId}/evaluations`);
  }

  getCandidateProgress(candidateId: string, processId: string): Observable<CandidateProgressDto> {
    return this.http.get<CandidateProgressDto>(`${this.candidateUrl}/${candidateId}/process/${processId}/progress`);
  }

  getInterviewSchedule(query: GetInterviewScheduleQuery): Observable<InterviewScheduleDto> {
    return this.http.get<InterviewScheduleDto>(`${this.interviewUrl}/${query.candidateProfileId}/round/${query.selectionRoundId}`);
  }
}
