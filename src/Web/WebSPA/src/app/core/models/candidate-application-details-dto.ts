import { ApplicationStatus } from './application-list-item-dto';

export interface CandidateApplicationDetailsDto {
  readonly id: string;
  readonly candidateProfileId: string;
  readonly jobId: string;
  readonly status: ApplicationStatus;
  readonly submittedAtUtc: string;
  readonly updatedAtUtc: string;
}
