import { Component, OnInit, OnDestroy, ChangeDetectorRef } from '@angular/core';
import { CommonModule } from '@angular/common';
import { Router, RouterModule } from '@angular/router';
import { Subscription } from 'rxjs';
import { ChatService } from '../../services/chat';

export type UserRole = 'Candidate' | 'Employer' | 'Admin';

@Component({
  selector: 'app-dashboard',
  standalone: true,
  imports: [CommonModule, RouterModule],
  templateUrl: './dashboard.html',
  styleUrls: ['./dashboard.scss']
})
export class DashboardComponent implements OnInit, OnDestroy {
  public userRole: UserRole = 'Candidate';
  public userName: string = 'Korisnik';
  public currentUserId: string = '';

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

    // 1. Pokrećemo SignalR konekciju i sa Dashboarda da bismo hvatali poruke u real-time-u
    this.chatService.startConnection();

    // 2. Pretplaćujemo se na reaktivni stream nepročitanih poruka
    this.subscribeToUnreadCount();
  }

  private extractUserFromToken(): void {
    const token = localStorage.getItem('jwt_token');
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
        'Korisnik';

      const roleClaim =
        payload.role ||
        payload['http://schemas.microsoft.com/ws/2008/06/identity/claims/role'];

      // ZAŠTITA: Proveravamo da li je izvučena uloga zaista jedna od dozvoljenih
      const VALID_ROLES: UserRole[] = ['Candidate', 'Employer', 'Admin'];

      if (roleClaim && VALID_ROLES.includes(roleClaim as UserRole)) {
        this.userRole = roleClaim as UserRole;
      } else {
        console.warn(`Prepoznata neispravna uloga: "${roleClaim}". Postavljam fallback na Candidate.`);
        this.userRole = 'Candidate'; // Siguran fallback da UI ne ostane prazan
      }
    } catch (e) {
      console.error('Greška pri dekodiranju tokena', e);
      this.userRole = 'Candidate';
    }
  }

  private subscribeToUnreadCount(): void {
    // Inicijalno dovlačimo konverzacije (tap operator u servisu automatski osvežava unreadCount$)
    this.chatService.getConversations().subscribe({
      error: (err) => console.error('Greška pri učitavanju nepročitanih poruka za Dashboard:', err)
    });

    // Pretplaćujemo se na stream koji reaguje i na inicijalno stanje i na SignalR poruke
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

  public navigateTo(route: string): void {
    this.router.navigate([route]);
  }

  ngOnDestroy(): void {
    if (this.unreadSub) {
      this.unreadSub.unsubscribe();
    }
  }
}
