import { HttpClient } from '@angular/common/http';
import { inject, Injectable } from '@angular/core';
import { Observable, switchMap } from 'rxjs';

import { CompanyProfileDto } from '../../models/company-profile-dto';
import { UrlResponseDto } from '../../models/url-response-dto';
import { CurrentUser } from '../../current-user';

@Injectable({
  providedIn: 'root',
})
export class CompanyProfileService {
  private readonly http = inject(HttpClient);
  private readonly currentUser = inject(CurrentUser);
  private readonly api = '/api/company-profiles';

  getProfile(userId: string): Observable<CompanyProfileDto> {
    return this.http.get<CompanyProfileDto>(`${this.api}/${userId}`);
  }

  /** The signed-in employer's company. Jobs reference it by the profile id, not the user id. */
  getMine(): Observable<CompanyProfileDto> {
    return this.currentUser.getUserId().pipe(switchMap(userId => this.getProfile(userId)));
  }

  updateProfile(id: string, data: CompanyProfileDto): Observable<void> {
    return this.http.put<void>(`${this.api}/${id}`, data);
  }

  deleteProfile(id: string): Observable<void> {
    return this.http.delete<void>(`${this.api}/${id}`);
  }

  uploadLogo(id: string, file: File): Observable<UrlResponseDto> {
    const formData = new FormData();
    formData.append('file', file);
    return this.http.post<UrlResponseDto>(`${this.api}/${id}/logo`, formData);
  }
}
