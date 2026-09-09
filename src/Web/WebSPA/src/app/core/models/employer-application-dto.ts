import { ApplicationListItemDto } from './application-list-item-dto';

export type CurrentCvStatus = 'Available' | 'Missing' | 'ProfileMissing' | 'ProfileReferenceMissing';

export interface EmployerApplicationDto extends ApplicationListItemDto {
  readonly candidateId: string;
  readonly candidateName: string | null;
  readonly coverLetter: string | null;
  readonly currentCvUrl: string | null;
  readonly cvStatus: CurrentCvStatus;
}
