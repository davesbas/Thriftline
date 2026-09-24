import { Component, OnInit, inject, signal, computed } from '@angular/core';
import { Router, RouterLink } from '@angular/router';
import { MatTabsModule } from '@angular/material/tabs';
import { MatButtonModule } from '@angular/material/button';
import { MatSnackBar } from '@angular/material/snack-bar';
import { OrderService, OrderSummaryResponse } from '../../core/orders/order';
import { AppHeader } from '../../shared/app-header/app-header';
import { MatIconModule } from '@angular/material/icon';

interface StatusTab {
  label: string;
  status: string | null;
}

@Component({
  selector: 'app-orders',
  standalone: true,
  imports: [MatTabsModule, MatButtonModule, AppHeader, RouterLink, MatIconModule],
  templateUrl: './orders.html',
  styleUrl: './orders.scss'
})
export class Orders implements OnInit {
  private orderService = inject(OrderService);
  private router = inject(Router);
  private snackBar = inject(MatSnackBar);

  orders = signal<OrderSummaryResponse[]>([]);
  isLoading = signal(true);
  selectedTabIndex = signal(0);
  cancellingIds = signal<Set<string>>(new Set());

  readonly tabs: StatusTab[] = [
    { label: 'Semua', status: null },
    { label: 'Belum Bayar', status: 'Pending' },
    { label: 'Dibayar', status: 'Paid' },
    { label: 'Diproses', status: 'Processing' },
    { label: 'Dikirim', status: 'Shipped' },
    { label: 'Selesai', status: 'Completed' },
    { label: 'Dibatalkan', status: 'Cancelled' }
  ];

  filteredOrders = computed(() => {
    const status = this.tabs[this.selectedTabIndex()].status;
    const all = this.orders();
    return status ? all.filter((o) => o.status === status) : all;
  });

  ngOnInit(): void {
    this.orderService.getMyOrders().subscribe({
      next: (data) => {
        this.orders.set(data);
        this.isLoading.set(false);
      },
      error: () => this.isLoading.set(false)
    });
  }

  formatPrice(price: number): string {
    return new Intl.NumberFormat('id-ID', {
      style: 'currency',
      currency: 'IDR',
      maximumFractionDigits: 0
    }).format(price);
  }

  formatDate(date: string): string {
    return new Intl.DateTimeFormat('id-ID', { dateStyle: 'medium', timeStyle: 'short' }).format(new Date(date));
  }

  payNow(order: OrderSummaryResponse): void {
    this.router.navigate(['/payment', order.id]);
  }

  isCancelling(orderId: string): boolean {
    return this.cancellingIds().has(orderId);
  }

  cancelOrder(order: OrderSummaryResponse): void {
    if (!confirm(`Batalkan pesanan ${order.orderNumber}?`)) return;

    this.cancellingIds.update((set) => new Set(set).add(order.id));

    this.orderService.cancel(order.id).subscribe({
      next: () => {
        this.orders.update((list) =>
          list.map((o) => (o.id === order.id ? { ...o, status: 'Cancelled' } : o))
        );
        this.cancellingIds.update((set) => {
          const next = new Set(set);
          next.delete(order.id);
          return next;
        });
        this.snackBar.open('Pesanan dibatalkan.', 'Tutup', { duration: 3000 });
      },
      error: (err) => {
        const message = typeof err.error === 'string' ? err.error : 'Gagal membatalkan pesanan.';
        this.snackBar.open(message, 'Tutup', { duration: 3000 });
        this.cancellingIds.update((set) => {
          const next = new Set(set);
          next.delete(order.id);
          return next;
        });
      }
    });
  }
}
