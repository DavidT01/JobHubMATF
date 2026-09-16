import { Component, OnInit, OnDestroy, ChangeDetectorRef } from '@angular/core';
import { Router } from '@angular/router';
import { Subscription } from 'rxjs';
import { ChatService } from '../../services/chat';
import { profileLinkFor } from '../../core/layout/navigation';
import { PageHeaderComponent } from '../../shared/page-header/page-header.component';

import { MatCardModule } from '@angular/material/card';
import { MatButtonModule } from '@angular/material/button';
import { MatIconModule } from '@angular/material/icon';

export type UserRole = 'Candidate' | 'Employer' | 'Admin';

@Component({
  selector: 'app-dashboard',
  standalone: true,
  imports: [
    MatCardModule,
    MatButtonModule,
    MatIconModule,
    PageHeaderComponent
  ],
  templateUrl: './dashboard.html',
  styleUrls: ['./dashboard.scss']
})
export class DashboardComponent implements OnInit, OnDestroy {
  public userRole: UserRole = 'Candidate';
  public userName: string = 'User';
  public currentUserId: string = '';
  public profileLink: string | null = null;

  public candidateStats = {
    activeApplications: 5,
    savedJobs: 12,
    unreadMessages: 0,
    upcomingInterviews: 1
  };

  public companyStats = {
    activeAds: 4,
    totalApplications: 28,
    newMessages: 0,
    scheduledInterviews: 2
  };

  public adminStats = {
    totalUsers: 142,
    totalCompanies: 18,
    activeAds: 35,
    systemHealth: '99.9%'
  };

  private unreadSub!: Subscription;

  constructor(
    private router: Router,
    private chatService: ChatService,
    private cdr: ChangeDetectorRef
  ) { }

  ngOnInit(): void {
    this.extractUserFromToken();
    this.profileLink = profileLinkFor(this.userRole);

    // Start the SignalR connection here too so new messages arrive in real time.
    this.chatService.startConnection();

    // Keep the unread message counters in sync with the chat service.
    this.subscribeToUnreadCount();
  }

  private extractUserFromToken(): void {
    const token = localStorage.getItem('auth_token');
    if (!token) return;

    try {
      const payload = JSON.parse(atob(token.split('.')[1]));

      this.currentUserId = payload.sub ||
        payload['http://schemas.xmlsoap.org/ws/2005/05/identity/claims/nameidentifier'] || '';

      this.userName =
        payload.username ||
        payload.unique_name ||
        payload.name ||
        payload['http://schemas.xmlsoap.org/ws/2005/05/identity/claims/name'] ||
        payload.sub ||
        'User';

      const roleClaim =
        payload.role ||
        payload['http://schemas.microsoft.com/ws/2008/06/identity/claims/role'];

      // Only accept roles the dashboard knows how to render.
      const VALID_ROLES: UserRole[] = ['Candidate', 'Employer', 'Admin'];

      if (roleClaim && VALID_ROLES.includes(roleClaim as UserRole)) {
        this.userRole = roleClaim as UserRole;
      } else {
        console.warn(`Unrecognized role "${roleClaim}". Falling back to Candidate.`);
        this.userRole = 'Candidate';
      }
    } catch (e) {
      console.error('Failed to decode the auth token', e);
      this.userRole = 'Candidate';
    }
  }

  private subscribeToUnreadCount(): void {
    // Loading conversations also refreshes unreadCount$ inside the chat service.
    this.chatService.getConversations().subscribe({
      error: (err) => console.error('Failed to load unread messages for the dashboard:', err)
    });

    // Reacts to both the initial state and messages pushed over SignalR.
    this.unreadSub = this.chatService.unreadCount$.subscribe((totalUnread: number) => {
      this.candidateStats = {
        ...this.candidateStats,
        unreadMessages: totalUnread
      };

      this.companyStats = {
        ...this.companyStats,
        newMessages: totalUnread
      };

      this.cdr.detectChanges();
    });
  }

  public navigateTo(route: string, stateData?: any): void {
    this.router.navigate([route], { state: stateData });
  }

  public testOpenChatWithFakeUser() {
    // Use the id of a candidate that exists in the database.
    const fakeCandidateId = "6b934877-899b-4bff-ac2d-102468129cb7";
    const fakeCandidateName = "Sample candidate";
    this.router.navigate(['/chat'], { state: { recipientId: fakeCandidateId, recipientName: fakeCandidateName } });
  }

  ngOnDestroy(): void {
    if (this.unreadSub) {
      this.unreadSub.unsubscribe();
    }
  }
}
