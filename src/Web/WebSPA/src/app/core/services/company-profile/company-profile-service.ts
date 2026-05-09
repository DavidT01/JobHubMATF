import { HttpClient } from '@angular/common/http';
import { inject, Injectable } from '@angular/core';
import { environment } from '../../../../environments/environment';
import { Observable } from 'rxjs';
import { CompanyProfileDto } from '../../models/company-profile-dto';

@Injectable({
  providedIn: 'root',
})
export class CompanyProfileService {
  private readonly http = inject(HttpClient);
  private readonly apiUrl = `${environment.apiUrl}/company-profiles`;

  getProfile(userId: string): Observable<CompanyProfileDto> {
    return this.http.get<CompanyProfileDto>(`${this.apiUrl}/${userId}`);
  }

  updateProfile(id: string, data: CompanyProfileDto): Observable<void> {
    return this.http.put<void>(`${this.apiUrl}/${id}`, data);
  }

  deleteProfile(id: string): Observable<void> {
    return this.http.delete<void>(`${this.apiUrl}/${id}`);
  }
}
