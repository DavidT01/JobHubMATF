import { ApplicationStatus } from './application-list-item-dto';

export type ApplicationSortBy = 'SubmittedAtUtc' | 'UpdatedAtUtc';
export type SortDirection = 'Asc' | 'Desc';

export interface ApplicationListOptions {
  readonly status?: ApplicationStatus;
  readonly sortBy?: ApplicationSortBy;
  readonly sortDirection?: SortDirection;
}

export interface ApplicationStatisticsPeriod {
  readonly from?: string;
  readonly to?: string;
}

export interface ApplicationStatusStatisticsDto {
  readonly status: ApplicationStatus;
  readonly count: number;
  readonly ratePercent: number;
}

export interface DailyApplicationCountDto {
  readonly date: string;
  readonly count: number;
}

export interface ApplicationStatisticsDto {
  readonly totalCount: number;
  readonly from: string | null;
  readonly to: string | null;
  readonly byStatus: readonly ApplicationStatusStatisticsDto[];
  readonly dailyTrend: readonly DailyApplicationCountDto[];
}
