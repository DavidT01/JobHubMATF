import { Component, OnInit, inject, signal } from '@angular/core';
import { RouterLink } from '@angular/router';
import { DatePipe } from '@angular/common';
import { MatButtonModule } from '@angular/material/button';
import { MatCardModule } from '@angular/material/card';
import { NotificationService, UserNotification } from '../../core/services/notification.service';

@Component({
  selector: 'app-notifications',
  standalone: true,
  imports: [MatButtonModule, MatCardModule, RouterLink, DatePipe],
  templateUrl: './notifications.component.html',
  styleUrl: './notifications.component.scss'
})
export class NotificationsComponent implements OnInit {
  private notificationService = inject(NotificationService);

  items = signal<UserNotification[]>([]);
  error = signal<string | null>(null);
  loading = signal(true);

  ngOnInit(): void {
    this.reload();
  }

  reload(): void {
    this.loading.set(true);
    this.notificationService.list().subscribe({
      next: (list: UserNotification[]) => {
        this.items.set(list);
        this.loading.set(false);
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
