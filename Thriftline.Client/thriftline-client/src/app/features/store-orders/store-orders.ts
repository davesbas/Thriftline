import { Component, OnInit, inject, signal } from '@angular/core';
import { RouterLink } from '@angular/router';
import { MatIconModule } from '@angular/material/icon';
import { MatButtonModule } from '@angular/material/button';
import { MatSnackBar } from '@angular/material/snack-bar';
import { OrderService, StoreOrderSummaryResponse } from '../../core/orders/order';
import { AppHeader } from '../../shared/app-header/app-header';

const NEXT_STATUS: Record<string, { next: string; label: string }> = {
  Paid: { next: 'Processing', label: 'Proses Pesanan' },
  Processing: { next: 'Shipped', label: 'Kirim Pesanan' },
  Shipped: { next: 'Completed', label: 'Selesaikan Pesanan' }
};

@Component({
  selector: 'app-store-orders',
  standalone: true,
  imports: [RouterLink, MatIconModule, MatButtonModule, AppHeader],
  templateUrl: './store-orders.html',
  styleUrl: './store-orders.scss'
})
export class StoreOrders implements OnInit {
  private orderService = inject(OrderService);
  private snackBar = inject(MatSnackBar);

  orders = signal<StoreOrderSummaryResponse[]>([]);
  isLoading = signal(true);
  updatingIds = signal<Set<string>>(new Set());

  ngOnInit(): void {
    this.load();
  }

  load(): void {
    this.isLoading.set(true);
    this.orderService.getStoreOrders().subscribe({
      next: (data) => {
        this.orders.set(data);
        this.isLoading.set(false);
      },
      error: () => this.isLoading.set(false)
    });
  }

  nextAction(status: string): { next: string; label: string } | null {
    return NEXT_STATUS[status] ?? null;
  }

  isUpdating(orderId: string): boolean {
    return this.updatingIds().has(orderId);
  }

  advanceStatus(order: StoreOrderSummaryResponse): void {
    const action = this.nextAction(order.status);
    if (!action || this.isUpdating(order.id)) return;

    this.updatingIds.update((set) => new Set(set).add(order.id));

    this.orderService.updateStatus(order.id, action.next).subscribe({
      next: () => {
        this.orders.update((items) =>
          items.map((o) => (o.id === order.id ? { ...o, status: action.next } : o))
        );
        this.updatingIds.update((set) => {
          const next = new Set(set);
          next.delete(order.id);
          return next;
        });
        this.snackBar.open('Status pesanan diperbarui.', 'Tutup', { duration: 3000 });
      },
      error: (err) => {
        const message = typeof err.error === 'string' ? err.error : 'Gagal memperbarui status.';
        this.snackBar.open(message, 'Tutup', { duration: 3000 });
        this.updatingIds.update((set) => {
          const next = new Set(set);
          next.delete(order.id);
          return next;
        });
      }
    });
  }

  formatPrice(price: number): string {
    return new Intl.NumberFormat('id-ID', { style: 'currency', currency: 'IDR', maximumFractionDigits: 0 }).format(price);
  }

  formatDate(date: string): string {
    return new Intl.DateTimeFormat('id-ID', { dateStyle: 'medium', timeStyle: 'short' }).format(new Date(date));
  }
}
