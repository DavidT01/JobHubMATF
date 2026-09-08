export type ApplicationStatus = 'Submitted' | 'InReview' | 'Interview' | 'Rejected' | 'Accepted';

export interface ApplicationListItemDto {
  readonly id: string;
  readonly jobId: string;
  readonly status: ApplicationStatus;
  readonly submittedAtUtc: string;
  readonly updatedAtUtc: string;
}

export interface PagedResult<T> {
  readonly items: readonly T[];
  readonly totalCount: number;
  readonly pageNumber: number;
  readonly pageSize: number;
}
