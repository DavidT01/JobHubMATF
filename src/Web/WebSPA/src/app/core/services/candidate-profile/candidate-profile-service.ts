import { HttpClient } from '@angular/common/http';
import { inject, Injectable } from '@angular/core';
import { Observable } from 'rxjs';
import { environment } from '../../../../environments/environment';
import { CandidateProfileDto } from '../../models/candidate-profile-dto';

@Injectable({
  providedIn: 'root',
})
export class CandidateProfileService {
  private readonly http = inject(HttpClient);
  private readonly api = `${environment.apiUrl}/candidate-profiles`;

  getProfile(id: string): Observable<CandidateProfileDto> {
    return this.http.get<CandidateProfileDto>(`${this.api}/${id}`);
  }

  updateProfile(id: string, data: CandidateProfileDto): Observable<void> {
    return this.http.put<void>(`${this.api}/${id}`, data);
  }

  deleteProfile(id: string): Observable<void> {
    return this.http.delete<void>(`${this.api}/${id}`);
  }
}
