import { Component, OnInit, inject, signal, computed } from '@angular/core';
import { ActivatedRoute, RouterLink } from '@angular/router';
import { forkJoin } from 'rxjs';
import { MatButtonModule } from '@angular/material/button';
import { MatRadioModule, MatRadioChange } from '@angular/material/radio';
import { MatSnackBar } from '@angular/material/snack-bar';
import { OrderService, OrderDetailResponse } from '../../core/orders/order';
import { PaymentService } from '../../core/payments/payment';
import { AppHeader } from '../../shared/app-header/app-header';

@Component({
  selector: 'app-payment-batch',
  standalone: true,
  imports: [RouterLink, MatButtonModule, MatRadioModule, AppHeader],
  templateUrl: './payment-batch.html',
  styleUrl: './payment-batch.scss'
})
export class PaymentBatch implements OnInit {
  private route = inject(ActivatedRoute);
  private orderService = inject(OrderService);
  private paymentService = inject(PaymentService);
  private snackBar = inject(MatSnackBar);

  orders = signal<OrderDetailResponse[]>([]);
  isLoading = signal(true);
  isProcessing = signal(false);
  isPaid = signal(false);
  selectedMethod = signal(0);

  readonly methods = [
    { value: 0, label: 'Transfer Bank' },
    { value: 1, label: 'Virtual Account' },
    { value: 2, label: 'Mobile Banking' },
    { value: 3, label: 'E-Wallet' },
    { value: 4, label: 'QRIS' },
    { value: 5, label: 'Kartu Kredit' },
    { value: 6, label: 'Bayar di Tempat (COD)' }
  ];

  grandTotal = computed(() => this.orders().reduce((sum, o) => sum + o.totalAmount, 0));

  ngOnInit(): void {
    const idsParam = this.route.snapshot.queryParamMap.get('orders');
    const orderIds = idsParam ? idsParam.split(',').filter((id) => id.length > 0) : [];

    if (orderIds.length === 0) {
      this.isLoading.set(false);
      return;
    }

    forkJoin(orderIds.map((id) => this.orderService.getById(id))).subscribe({
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

  onMethodChange(event: MatRadioChange): void {
    this.selectedMethod.set(event.value);
  }

  pay(): void {
    const orderIds = this.orders().map((o) => o.id);
    if (orderIds.length === 0) return;

    this.isProcessing.set(true);

    this.paymentService.createBatch(orderIds, this.selectedMethod()).subscribe({
      next: () => {
        this.isProcessing.set(false);
        this.isPaid.set(true);
        this.snackBar.open('Pembayaran berhasil!', 'Tutup', { duration: 3000 });
      },
      error: (err) => {
        this.isProcessing.set(false);
        const message = typeof err.error === 'string' ? err.error : 'Pembayaran gagal, coba lagi.';
        this.snackBar.open(message, 'Tutup', { duration: 3000 });
      }
    });
  }
}
