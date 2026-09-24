import { Component, OnInit, OnDestroy, inject, signal, computed } from '@angular/core';
import { ActivatedRoute, Router, RouterLink } from '@angular/router';
import { MatButtonModule } from '@angular/material/button';
import { MatIconModule } from '@angular/material/icon';
import { MatRadioModule } from '@angular/material/radio';
import { MatSnackBar } from '@angular/material/snack-bar';
import { CartService, CartItemResponse } from '../../core/cart/cart';
import { OrderService, CourierOption } from '../../core/orders/order';
import { AddressService, AddressResponse } from '../../core/addresses/address';
import { PaymentService } from '../../core/payments/payment';
import { AppHeader } from '../../shared/app-header/app-header';
import { WalletService } from '../../core/wallets/wallet';

interface CheckoutGroup {
  storeId: string;
  storeName: string;
  items: CartItemResponse[];
  subtotal: number;
}

type PaymentStage = 'form' | 'waiting' | 'done';

const WAITING_METHODS = new Set([0, 1, 2, 4]); // BankTransfer, VirtualAccount, MobileBanking, QRIS
const VA_PREFIXES: Record<number, string> = { 0: '7001', 1: '8808', 2: '3901' };
const COUNTDOWN_SECONDS = 300;
const QR_CELL_COUNT = 225;

@Component({
  selector: 'app-checkout',
  standalone: true,
  imports: [RouterLink, MatButtonModule, MatIconModule, MatRadioModule, AppHeader],
  templateUrl: './checkout.html',
  styleUrl: './checkout.scss'
})
export class Checkout implements OnInit, OnDestroy {
  private route = inject(ActivatedRoute);
  private router = inject(Router);
  private cartService = inject(CartService);
  private orderService = inject(OrderService);
  private addressService = inject(AddressService);
  private paymentService = inject(PaymentService);
  private snackBar = inject(MatSnackBar);
  private walletService = inject(WalletService);

  isLoading = signal(true);
  isSubmitting = signal(false);
  walletBalance = signal(0);

  storeGroups = signal<CheckoutGroup[]>([]);
  addresses = signal<AddressResponse[]>([]);
  selectedAddressId = signal<string | null>(null);
  couriers = signal<CourierOption[]>([]);
  courierByStore = signal<Record<string, string>>({});
  noteByStore = signal<Record<string, string>>({});
  selectedMethod = signal(0);

  paymentStage = signal<PaymentStage>('form');
  countdownSeconds = signal(0);
  virtualAccountNumber = signal('');
  qrCells = signal<boolean[]>([]);
  private pendingOrderIds: string[] = [];
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

  selectedAddress = computed(() => this.addresses().find((a) => a.id === this.selectedAddressId()) ?? null);
  subtotal = computed(() => this.storeGroups().reduce((sum, g) => sum + g.subtotal, 0));
  shippingTotal = computed(() =>
    this.storeGroups().reduce((sum, g) => sum + this.groupShippingCost(g.storeId), 0)
  );
  grandTotal = computed(() => this.subtotal() + this.shippingTotal());
  selectedMethodLabel = computed(() => this.methods.find((m) => m.value === this.selectedMethod())?.label ?? '');

  countdownDisplay = computed(() => {
    const total = this.countdownSeconds();
    const m = Math.floor(total / 60).toString().padStart(2, '0');
    const s = (total % 60).toString().padStart(2, '0');
    return `${m}:${s}`;
  });

  ngOnInit(): void {
    const idsParam = this.route.snapshot.queryParamMap.get('items');
    const selectedIds = new Set(idsParam ? idsParam.split(',').filter((id) => id.length > 0) : []);

    if (selectedIds.size === 0) {
      this.router.navigate(['/cart']);
      return;
    }

    this.cartService.getSummary().subscribe({
      next: (data) => {
        const selected = data.items.filter((item) => selectedIds.has(item.id));
        const map = new Map<string, CheckoutGroup>();
        for (const item of selected) {
          let group = map.get(item.storeId);
          if (!group) {
            group = { storeId: item.storeId, storeName: item.storeName, items: [], subtotal: 0 };
            map.set(item.storeId, group);
          }
          group.items.push(item);
          group.subtotal += item.subtotal;
        }
        this.storeGroups.set(Array.from(map.values()));
        this.isLoading.set(false);
      },
      error: () => this.isLoading.set(false)
    });

    this.orderService.getCouriers().subscribe({
      next: (data) => this.couriers.set(data),
      error: () => {}
    });

    this.addressService.getMine().subscribe({
      next: (data) => {
        this.addresses.set(data);
        const primary = data.find((a) => a.isPrimary) ?? data[0];
        if (primary) {
          this.selectedAddressId.set(primary.id);
        }
      },
      error: () => {}
    });

    this.walletService.getMyWallet().subscribe({
      next: (data) => this.walletBalance.set(data.balance),
      error: () => {}
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

  courierFor(storeId: string): string {
    return this.courierByStore()[storeId] ?? this.couriers()[0]?.name ?? '';
  }

  setCourier(storeId: string, name: string): void {
    this.courierByStore.update((map) => ({ ...map, [storeId]: name }));
  }

  groupShippingCost(storeId: string): number {
    const name = this.courierFor(storeId);
    return this.couriers().find((c) => c.name === name)?.cost ?? 0;
  }

  noteFor(storeId: string): string {
    return this.noteByStore()[storeId] ?? '';
  }

  setNote(storeId: string, value: string): void {
    this.noteByStore.update((map) => ({ ...map, [storeId]: value }));
  }

  placeOrder(): void {
    const address = this.selectedAddress();
    if (!address) {
      this.snackBar.open('Pilih atau tambah alamat pengiriman dulu.', 'Tutup', { duration: 3000 });
      return;
    }

    const groups = this.storeGroups();
    const storeOptions = groups.map((g) => ({
      storeId: g.storeId,
      shippingCourier: this.courierFor(g.storeId),
      note: this.noteFor(g.storeId).trim() || undefined
    }));

    if (storeOptions.some((o) => !o.shippingCourier)) {
      this.snackBar.open('Pilih kurir untuk setiap toko.', 'Tutup', { duration: 3000 });
      return;
    }

    const cartItemIds = groups.flatMap((g) => g.items.map((i) => i.id));
    const shippingAddress = `${address.recipientName} (${address.phoneNumber}) - ${address.fullAddress}`;

    this.isSubmitting.set(true);

    this.orderService.checkout({ shippingAddress, cartItemIds, storeOptions }).subscribe({
      next: (orders) => {
        this.isSubmitting.set(false);
        this.pendingOrderIds = orders.map((o) => o.id);

        if (WAITING_METHODS.has(this.selectedMethod())) {
          this.enterWaitingStage();
        } else {
          this.finalizePayment();
        }
      },
      error: (err) => {
        this.isSubmitting.set(false);
        const message = typeof err.error === 'string' ? err.error : 'Gagal membuat pesanan.';
        this.snackBar.open(message, 'Tutup', { duration: 3000 });
      }
    });
  }

  confirmPayment(): void {
    this.stopCountdown();
    this.finalizePayment();
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

  private finalizePayment(): void {
    this.isSubmitting.set(true);
    this.paymentService.createBatch(this.pendingOrderIds, this.selectedMethod()).subscribe({
      next: () => {
        this.isSubmitting.set(false);
        this.paymentStage.set('done');
      },
      error: () => {
        this.isSubmitting.set(false);
        this.snackBar.open(
          'Pesanan berhasil dibuat, tapi pembayaran gagal. Silakan bayar dari Pesanan Saya.',
          'Tutup',
          { duration: 5000 }
        );
        this.router.navigate(['/orders']);
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
}
