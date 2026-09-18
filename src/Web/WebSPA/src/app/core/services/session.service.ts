import { Injectable, computed, inject, signal } from '@angular/core';
import { Router } from '@angular/router';
import { CurrentUser } from '../current-user';
import { primaryRole, roleLabel } from '../layout/navigation';
import { AuthService, MeResponse } from './auth.service';
import { NotificationService } from './notification.service';

/** Signed-in user state shared by the app shell (header, sidebar, user menu). */
@Injectable({
  providedIn: 'root'
})
export class SessionService {
  private readonly auth = inject(AuthService);
  private readonly notifications = inject(NotificationService);
  private readonly currentUser = inject(CurrentUser);
  private readonly router = inject(Router);
  private loadedToken: string | null = null;

  readonly user = signal<MeResponse | null>(null);
  readonly unreadCount = signal(0);
  readonly role = computed(() => primaryRole(this.user()?.roles ?? []));
  readonly roleLabel = computed(() => roleLabel(this.role()));
  readonly displayName = computed(() => {
    const user = this.user();
    if (!user) return '';
    const name = [user.firstName, user.lastName].filter(Boolean).join(' ').trim();
    return name || user.email;
  });

  /** Loads the user when the token changes and refreshes the unread notification count. */
  sync(): void {
    const token = this.auth.getToken();
    if (!token) {
      this.reset();
      return;
    }

    if (token !== this.loadedToken) {
      this.loadedToken = token;
      this.user.set(null);
      this.currentUser.clear();
      this.auth.me().subscribe({
        next: user => this.user.set(user),
        error: () => this.user.set(null)
      });
    }

    this.refreshUnreadCount();
  }

  refreshUnreadCount(): void {
    if (!this.auth.isLoggedIn()) return;

    this.notifications.unreadCount().subscribe({
      next: response => this.unreadCount.set(response.count),
      error: () => this.unreadCount.set(0)
    });
  }

  signOut(): void {
    this.auth.logout();
    this.reset();
    this.router.navigate(['/login']);
  }

  private reset(): void {
    this.loadedToken = null;
    this.user.set(null);
    this.unreadCount.set(0);
    this.currentUser.clear();
  }
}
