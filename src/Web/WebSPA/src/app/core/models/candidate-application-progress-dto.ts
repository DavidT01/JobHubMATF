import { CandidateProgressDto } from './candidate-progress-dto';
import { InterviewScheduleDto } from './interview-schedule-dto';
import { RecruitmentProcessDto } from './recruitment-process-dto';

export interface CandidateApplicationProgressDto {
  readonly progress: CandidateProgressDto;
  readonly process: RecruitmentProcessDto;
  readonly interviews: readonly InterviewScheduleDto[];
}
