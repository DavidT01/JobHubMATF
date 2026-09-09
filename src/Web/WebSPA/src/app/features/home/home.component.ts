import { Component, OnInit, computed, inject, signal } from '@angular/core';
import { Router, RouterLink } from '@angular/router';
import { MatButtonModule } from '@angular/material/button';
import { MatCardModule } from '@angular/material/card';
import { AuthService, MeResponse } from '../../core/services/auth.service';
import { NotificationService } from '../../core/services/notification.service';

@Component({
  selector: 'app-home',
  standalone: true,
  imports: [MatButtonModule, MatCardModule, RouterLink],
  templateUrl: './home.component.html',
  styleUrl: './home.component.scss'
})
export class HomeComponent implements OnInit {
  private authService = inject(AuthService);
  private notificationService = inject(NotificationService);
  private router = inject(Router);

  user = signal<MeResponse | null>(null);
  unreadCount = signal(0);

  isAdmin = computed(() =>
    (this.user()?.roles ?? []).some(r => r.toLowerCase() === 'admin')
  );

  ngOnInit(): void {
    this.authService.me().subscribe({
      next: profile => {
        this.user.set(profile);
        this.refreshUnread();
      },
      error: () => this.logout()
    });
  }

  refreshUnread(): void {
    this.notificationService.unreadCount().subscribe({
      next: res => this.unreadCount.set(res.count),
      error: () => this.unreadCount.set(0)
    });
  }

  logout(): void {
    this.authService.logout();
    this.router.navigate(['/login']);
  }
}
