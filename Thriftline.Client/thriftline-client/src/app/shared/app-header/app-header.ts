import { Component, OnInit, inject, signal } from '@angular/core';
import { RouterLink } from '@angular/router';
import { MatIconModule } from '@angular/material/icon';
import { MatButtonModule } from '@angular/material/button';
import { MatMenuModule } from '@angular/material/menu';
import { AuthService } from '../../core/auth/auth';
import { CartService } from '../../core/cart/cart';
import { NotificationService } from '../../core/notifications/notification';
import { UserService } from '../../core/users/user';

@Component({
  selector: 'app-header',
  standalone: true,
  imports: [RouterLink, MatIconModule, MatButtonModule, MatMenuModule],
  templateUrl: './app-header.html',
  styleUrl: './app-header.scss'
})
export class AppHeader implements OnInit {
  authService = inject(AuthService);
  private cartService = inject(CartService);
  private notificationService = inject(NotificationService);
  private userService = inject(UserService);

  cartCount = signal(0);
  unreadCount = signal(0);
  avatarUrl = signal<string | null>(null);

  ngOnInit(): void {
    if (this.authService.isLoggedIn()) {
      this.cartService.getSummary().subscribe((data) => this.cartCount.set(data.totalItems));
      this.notificationService.getUnreadCount().subscribe((data) => this.unreadCount.set(data.unreadCount));
      this.userService.getMe().subscribe((profile) => this.avatarUrl.set(profile.profilePictureUrl));
    }
  }

  logout(): void {
    this.authService.logout();
    this.cartCount.set(0);
    this.unreadCount.set(0);
    this.avatarUrl.set(null);
  }
}
