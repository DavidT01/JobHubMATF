import { HttpClient } from '@angular/common/http';
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

@Injectable({
  providedIn: 'root'
})
export class AdminService {
  private http = inject(HttpClient);
  private apiUrl = 'http://localhost:5283/api/admin';

  listUsers(): Observable<AdminUser[]> {
    return this.http.get<AdminUser[]>(`${this.apiUrl}/users`);
  }

  lockUser(id: string): Observable<{ message: string }> {
    return this.http.post<{ message: string }>(`${this.apiUrl}/users/${id}/lock`, {});
  }

  unlockUser(id: string): Observable<{ message: string }> {
    return this.http.post<{ message: string }>(`${this.apiUrl}/users/${id}/unlock`, {});
  }

  setRole(id: string, role: string): Observable<AdminUser> {
    return this.http.put<AdminUser>(`${this.apiUrl}/users/${id}/role`, { role });
  }
}
