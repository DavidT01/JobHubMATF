import { HttpClient } from '@angular/common/http';
import { Injectable, inject } from '@angular/core';
import { Observable } from 'rxjs';
import { environment } from '../../../environments/environment';

export interface UserNotification {
  id: string;
  title: string;
  message: string;
  createdAtUtc: string;
  isRead: boolean;
}

@Injectable({
  providedIn: 'root'
})
export class NotificationService {
  private http = inject(HttpClient);
  private apiUrl = environment.notificationApiUrl;

  list(): Observable<UserNotification[]> {
    return this.http.get<UserNotification[]>(this.apiUrl);
  }

  unreadCount(): Observable<{ count: number }> {
    return this.http.get<{ count: number }>(`${this.apiUrl}/unread-count`);
  }

  markRead(id: string): Observable<{ message: string }> {
    return this.http.post<{ message: string }>(`${this.apiUrl}/${id}/read`, {});
  }

  markAllRead(): Observable<{ message: string }> {
    return this.http.post<{ message: string }>(`${this.apiUrl}/read-all`, {});
  }
}
