import { Component, OnInit, OnDestroy, inject, signal, computed } from '@angular/core';
import { ActivatedRoute, Router, RouterLink } from '@angular/router';
import { MatButtonModule } from '@angular/material/button';
import { MatIconModule } from '@angular/material/icon';
import { MatRadioModule, MatRadioChange } from '@angular/material/radio';
import { MatSnackBar } from '@angular/material/snack-bar';
import { OrderService, OrderDetailResponse } from '../../core/orders/order';
import { PaymentService } from '../../core/payments/payment';
import { AppHeader } from '../../shared/app-header/app-header';

type PaymentStage = 'form' | 'waiting' | 'done';

const WAITING_METHODS = new Set([0, 1, 2, 4]); // BankTransfer, VirtualAccount, MobileBanking, QRIS
const VA_PREFIXES: Record<number, string> = { 0: '7001', 1: '8808', 2: '3901' };
const COUNTDOWN_SECONDS = 300;
const QR_CELL_COUNT = 225;

@Component({
  selector: 'app-payment',
  standalone: true,
  imports: [RouterLink, MatButtonModule, MatIconModule, MatRadioModule, AppHeader],
  templateUrl: './payment.html',
  styleUrl: './payment.scss'
})
export class Payment implements OnInit, OnDestroy {
  private route = inject(ActivatedRoute);
  private router = inject(Router);
  private orderService = inject(OrderService);
  private paymentService = inject(PaymentService);
  private snackBar = inject(MatSnackBar);

  order = signal<OrderDetailResponse | null>(null);
  isLoading = signal(true);
  isProcessing = signal(false);
  selectedMethod = signal(0);

  paymentStage = signal<PaymentStage>('form');
  countdownSeconds = signal(0);
  virtualAccountNumber = signal('');
  qrCells = signal<boolean[]>([]);
  private currentPaymentId: string | null = null;
  private countdownHandle?: ReturnType<typeof setInterval>;

  readonly methods = [
    { value: 0, label: 'Transfer Bank' },
    { value: 1, label: 'Virtual Account' },
    { value: 2, label: 'Mobile Banking' },
    { value: 3, label: 'E-Wallet' },
    { value: 4, label: 'QRIS' },
    { value: 5, label: 'Kartu Kredit' },
    { value: 6, label: 'Bayar di Tempat (COD)' }
  ];

  selectedMethodLabel = computed(() => this.methods.find((m) => m.value === this.selectedMethod())?.label ?? '');

  countdownDisplay = computed(() => {
    const total = this.countdownSeconds();
    const m = Math.floor(total / 60).toString().padStart(2, '0');
    const s = (total % 60).toString().padStart(2, '0');
    return `${m}:${s}`;
  });

  ngOnInit(): void {
    const orderId = this.route.snapshot.paramMap.get('orderId');
    if (!orderId) {
      this.router.navigate(['/']);
      return;
    }

    this.orderService.getById(orderId).subscribe({
      next: (data) => {
        this.order.set(data);
        this.isLoading.set(false);
      },
      error: () => this.isLoading.set(false)
    });
  }

  ngOnDestroy(): void {
    this.stopCountdown();
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
    const order = this.order();
    if (!order) return;

    this.isProcessing.set(true);

    this.paymentService.create({ orderId: order.id, method: this.selectedMethod() }).subscribe({
      next: (payment) => {
        this.currentPaymentId = payment.id;

        if (WAITING_METHODS.has(this.selectedMethod())) {
          this.isProcessing.set(false);
          this.enterWaitingStage();
        } else {
          this.finalizeConfirm();
        }
      },
      error: (err) => {
        this.isProcessing.set(false);
        this.snackBar.open(this.extractError(err, 'Gagal membuat pembayaran.'), 'Tutup', { duration: 3000 });
      }
    });
  }

  confirmPayment(): void {
    this.stopCountdown();
    this.finalizeConfirm();
  }

  copyVirtualAccount(): void {
    navigator.clipboard.writeText(this.virtualAccountNumber()).then(() => {
      this.snackBar.open('Nomor Virtual Account disalin.', 'Tutup', { duration: 2000 });
    });
  }

  private enterWaitingStage(): void {
    const prefix = VA_PREFIXES[this.selectedMethod()] ?? '7001';
    const digits = Math.floor(100000000000 + Math.random() * 899999999999).toString();
    this.virtualAccountNumber.set(prefix + digits);
    this.qrCells.set(Array.from({ length: QR_CELL_COUNT }, () => Math.random() > 0.5));
    this.paymentStage.set('waiting');
    this.startCountdown(COUNTDOWN_SECONDS);
  }

  private finalizeConfirm(): void {
    if (!this.currentPaymentId) return;

    this.isProcessing.set(true);

    this.paymentService.confirm(this.currentPaymentId).subscribe({
      next: () => {
        this.isProcessing.set(false);
        this.paymentStage.set('done');
        this.snackBar.open('Pembayaran berhasil!', 'Tutup', { duration: 3000 });
      },
      error: (err) => {
        this.isProcessing.set(false);
        this.snackBar.open(this.extractError(err, 'Konfirmasi pembayaran gagal.'), 'Tutup', { duration: 3000 });
      }
    });
  }

  private startCountdown(seconds: number): void {
    this.stopCountdown();
    this.countdownSeconds.set(seconds);
    this.countdownHandle = setInterval(() => {
      this.countdownSeconds.update((s) => (s <= 1 ? 0 : s - 1));
    }, 1000);
  }

  private stopCountdown(): void {
    if (this.countdownHandle) {
      clearInterval(this.countdownHandle);
      this.countdownHandle = undefined;
    }
  }

  private extractError(err: unknown, fallback: string): string {
    const errorBody = (err as { error?: unknown })?.error;
    return typeof errorBody === 'string' ? errorBody : fallback;
  }
}
