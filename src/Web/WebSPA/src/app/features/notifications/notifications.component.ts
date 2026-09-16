import { Component, OnInit, computed, inject, signal } from '@angular/core';
import { DatePipe } from '@angular/common';
import { MatButtonModule } from '@angular/material/button';
import { NotificationService, UserNotification } from '../../core/services/notification.service';
import { SessionService } from '../../core/services/session.service';
import { PageHeaderComponent } from '../../shared/page-header/page-header.component';

@Component({
  selector: 'app-notifications',
  standalone: true,
  imports: [MatButtonModule, DatePipe, PageHeaderComponent],
  templateUrl: './notifications.component.html',
  styleUrl: './notifications.component.scss'
})
export class NotificationsComponent implements OnInit {
  private notificationService = inject(NotificationService);
  private session = inject(SessionService);

  items = signal<UserNotification[]>([]);
  error = signal<string | null>(null);
  loading = signal(true);
  hasUnread = computed(() => this.items().some(item => !item.isRead));

  ngOnInit(): void {
    this.reload();
  }

  reload(): void {
    this.loading.set(true);
    this.notificationService.list().subscribe({
      next: (list: UserNotification[]) => {
        this.items.set(list);
        this.loading.set(false);
        this.session.refreshUnreadCount();
      },
      error: () => {
        this.error.set('Could not load notifications.');
        this.loading.set(false);
      }
    });
  }

  markRead(item: UserNotification): void {
    if (item.isRead) {
      return;
    }

    this.notificationService.markRead(item.id).subscribe({
      next: () => this.reload()
    });
  }

  markAllRead(): void {
    this.notificationService.markAllRead().subscribe({
      next: () => this.reload()
    });
  }
}
