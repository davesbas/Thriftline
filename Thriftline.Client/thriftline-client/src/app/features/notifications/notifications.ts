import { Component, OnInit, inject, signal } from '@angular/core';
import { Router } from '@angular/router';
import { MatButtonModule } from '@angular/material/button';
import { MatIconModule } from '@angular/material/icon';
import { NotificationService, NotificationResponse } from '../../core/notifications/notification';
import { AppHeader } from '../../shared/app-header/app-header';

@Component({
  selector: 'app-notifications',
  standalone: true,
  imports: [MatButtonModule, MatIconModule, AppHeader],
  templateUrl: './notifications.html',
  styleUrl: './notifications.scss'
})
export class Notifications implements OnInit {
  private notificationService = inject(NotificationService);
  private router = inject(Router);

  notifications = signal<NotificationResponse[]>([]);
  isLoading = signal(true);

  private readonly typeIcons: Record<string, string> = {
    Order: 'shopping_bag',
    Chat: 'chat_bubble',
    Forum: 'forum',
    System: 'info',
    Promo: 'local_offer'
  };

  private readonly typeColors: Record<string, string> = {
    Order: '#22c55e',
    Chat: '#4f6bff',
    Forum: '#a855f7',
    System: '#64748b',
    Promo: '#f59e0b'
  };

  colorFor(type: string): string {
    return this.typeColors[type] ?? '#4f6bff';
  }

  ngOnInit(): void {
    this.load();
  }

  load(): void {
    this.isLoading.set(true);
    this.notificationService.getAll().subscribe({
      next: (result) => {
        this.notifications.set(result.items);
        this.isLoading.set(false);
      },
      error: () => this.isLoading.set(false)
    });
  }

  iconFor(type: string): string {
    return this.typeIcons[type] ?? 'notifications';
  }

  formatDate(date: string): string {
    return new Intl.DateTimeFormat('id-ID', { dateStyle: 'medium', timeStyle: 'short' }).format(new Date(date));
  }

  openNotification(notification: NotificationResponse): void {
    if (!notification.isRead) {
      this.notificationService.markAsRead(notification.id).subscribe(() => {
        notification.isRead = true;
        this.notifications.update((items) => [...items]);
      });
    }

    if (notification.relatedEntityType === 'Order') {
      this.router.navigate(['/orders']);
    }
  }

  markAllAsRead(): void {
    this.notificationService.markAllAsRead().subscribe(() => {
      this.notifications.update((items) => items.map((n) => ({ ...n, isRead: true })));
    });
  }
}
