import { HttpClient, HttpParams } from '@angular/common/http';
import { inject, Injectable, InjectionToken } from '@angular/core';
import { Observable } from 'rxjs';

import { ApplicationListItemDto, PagedResult } from '../../models/application-list-item-dto';
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
  ): Observable<PagedResult<EmployerApplicationDto>> {
    const params = new HttpParams().set('pageNumber', pageNumber).set('pageSize', pageSize);

    return this.http.get<PagedResult<EmployerApplicationDto>>(
      `${this.api}/jobs/${encodeURIComponent(jobId)}`, { params },
    );
  }

  getMyApplications(
    pageNumber = 1,
    pageSize = 20,
  ): Observable<PagedResult<ApplicationListItemDto>> {
    const params = new HttpParams().set('pageNumber', pageNumber).set('pageSize', pageSize);

    return this.http.get<PagedResult<ApplicationListItemDto>>(`${this.api}/me`, { params });
  }
}
