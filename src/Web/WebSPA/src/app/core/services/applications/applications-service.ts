import { HttpClient, HttpParams } from '@angular/common/http';
import { inject, Injectable, InjectionToken } from '@angular/core';
import { Observable } from 'rxjs';

import {
  ApplicationListItemDto,
  ApplicationStatus,
  PagedResult,
} from '../../models/application-list-item-dto';
import {
  ApplicationListOptions,
  ApplicationStatisticsDto,
  ApplicationStatisticsPeriod,
} from '../../models/application-management-dto';
import { EmployerApplicationDto } from '../../models/employer-application-dto';
import { SubmitApplicationRequest } from '../../models/submit-application-request';

// Override at bootstrap if the deployment exposes a different application API base URL.
export const APPLICATIONS_API_URL = new InjectionToken<string>('APPLICATIONS_API_URL', {
  providedIn: 'root',
  factory: () => '/api/applications',
});

@Injectable({ providedIn: 'root' })
export class ApplicationsService {
  private readonly http = inject(HttpClient);
  private readonly api = inject(APPLICATIONS_API_URL).replace(/\/+$/, '');

  submitApplication(request: SubmitApplicationRequest): Observable<ApplicationListItemDto> {
    // Only application fields are sent; identity and the current CV are resolved by the server.
    return this.http.post<ApplicationListItemDto>(this.api, {
      jobId: request.jobId,
      coverLetter: request.coverLetter ?? null,
    });
  }

  getForJob(
    jobId: string,
    pageNumber = 1,
    pageSize = 20,
    options: ApplicationListOptions = {},
  ): Observable<PagedResult<EmployerApplicationDto>> {
    const params = this.listParams(pageNumber, pageSize, options);

    return this.http.get<PagedResult<EmployerApplicationDto>>(
      `${this.api}/jobs/${encodeURIComponent(jobId)}`, { params },
    );
  }

  getMyApplications(
    pageNumber = 1,
    pageSize = 20,
    options: ApplicationListOptions = {},
  ): Observable<PagedResult<ApplicationListItemDto>> {
    const params = this.listParams(pageNumber, pageSize, options);

    return this.http.get<PagedResult<ApplicationListItemDto>>(`${this.api}/me`, { params });
  }

  changeStatus(applicationId: string, status: ApplicationStatus): Observable<void> {
    return this.http.put<void>(`${this.api}/${encodeURIComponent(applicationId)}/status`, { status });
  }

  getStatistics(period: ApplicationStatisticsPeriod = {}): Observable<ApplicationStatisticsDto> {
    let params = new HttpParams();
    if (period.from) params = params.set('from', period.from);
    if (period.to) params = params.set('to', period.to);

    return this.http.get<ApplicationStatisticsDto>(`${this.api}/statistics`, { params });
  }

  private listParams(
    pageNumber: number,
    pageSize: number,
    options: ApplicationListOptions,
  ): HttpParams {
    let params = new HttpParams().set('pageNumber', pageNumber).set('pageSize', pageSize);
    if (options.status) params = params.set('status', options.status);
    if (options.sortBy) params = params.set('sortBy', options.sortBy);
    if (options.sortDirection) params = params.set('sortDirection', options.sortDirection);
    return params;
  }
}
