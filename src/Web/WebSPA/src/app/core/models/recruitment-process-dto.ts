import { SelectionRoundDto } from './selection-round-dto';

export interface RecruitmentProcessDto {
  id: string;
  companyId: string;
  jobId: string;
  isActive: boolean;
  rounds: SelectionRoundDto[];
}
