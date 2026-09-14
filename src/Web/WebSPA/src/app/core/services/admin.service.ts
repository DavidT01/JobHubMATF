import { HttpClient, HttpParams } from '@angular/common/http';
import { Injectable, inject } from '@angular/core';
import { Observable } from 'rxjs';

export interface AdminUser {
  id: string;
  email: string;
  firstName?: string;
  lastName?: string;
  roles: string[];
  emailConfirmed: boolean;
  lockedOut: boolean;
}

export interface AdminStats {
  totalUsers: number;
  confirmedEmails: number;
  lockedAccounts: number;
}

@Injectable({
  providedIn: 'root'
})
export class AdminService {
  private http = inject(HttpClient);
  private apiUrl = 'http://localhost:5283/api/admin';

  listUsers(search?: string): Observable<AdminUser[]> {
    let params = new HttpParams();
    if (search?.trim()) {
      params = params.set('search', search.trim());
    }
    return this.http.get<AdminUser[]>(`${this.apiUrl}/users`, { params });
  }

  stats(): Observable<AdminStats> {
    return this.http.get<AdminStats>(`${this.apiUrl}/stats`);
  }

  lockUser(id: string): Observable<{ message: string }> {
    return this.http.post<{ message: string }>(`${this.apiUrl}/users/${id}/lock`, {});
  }

  unlockUser(id: string): Observable<{ message: string }> {
    return this.http.post<{ message: string }>(`${this.apiUrl}/users/${id}/unlock`, {});
  }

  confirmEmail(id: string): Observable<AdminUser> {
    return this.http.post<AdminUser>(`${this.apiUrl}/users/${id}/confirm-email`, {});
  }

  setRole(id: string, role: string): Observable<AdminUser> {
    return this.http.put<AdminUser>(`${this.apiUrl}/users/${id}/role`, { role });
  }
}
