import { HttpClient } from '@angular/common/http';
import { inject, Injectable } from '@angular/core';
import { environment } from '../../../../environments/environment';
import { Observable } from 'rxjs';

import { CompanyProfileDto } from '../../models/company-profile-dto';
import { UrlResponseDto } from '../../models/url-response-dto';

@Injectable({
  providedIn: 'root',
})
export class CompanyProfileService {
  private readonly http = inject(HttpClient);
  private readonly api = `${environment.apiUrl}/company-profiles`;

  getProfile(userId: string): Observable<CompanyProfileDto> {
    return this.http.get<CompanyProfileDto>(`${this.api}/${userId}`);
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
