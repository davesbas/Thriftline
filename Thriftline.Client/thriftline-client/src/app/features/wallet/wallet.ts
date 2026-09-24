import { Component, OnInit, inject, signal } from '@angular/core';
import { RouterLink } from '@angular/router';
import { FormsModule } from '@angular/forms';
import { MatButtonModule } from '@angular/material/button';
import { MatIconModule } from '@angular/material/icon';
import { MatSnackBar } from '@angular/material/snack-bar';
import { WalletService, WalletTransactionResponse } from '../../core/wallets/wallet';
import { AppHeader } from '../../shared/app-header/app-header';

@Component({
  selector: 'app-wallet',
  standalone: true,
  imports: [RouterLink, FormsModule, MatButtonModule, MatIconModule, AppHeader],
  templateUrl: './wallet.html',
  styleUrl: './wallet.scss'
})
export class Wallet implements OnInit {
  private walletService = inject(WalletService);
  private snackBar = inject(MatSnackBar);

  balance = signal(0);
  transactions = signal<WalletTransactionResponse[]>([]);
  isLoading = signal(true);
  isToppingUp = signal(false);
  customAmount = signal<number | null>(null);

  readonly quickAmounts = [50000, 100000, 200000, 500000];

  ngOnInit(): void {
    this.load();
  }

  load(): void {
    this.isLoading.set(true);
    this.walletService.getMyWallet().subscribe({
      next: (data) => {
        this.balance.set(data.balance);
        this.isLoading.set(false);
      },
      error: () => this.isLoading.set(false)
    });

    this.walletService.getTransactions().subscribe({
      next: (data) => this.transactions.set(data),
      error: () => {}
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

  topUp(amount: number): void {
    if (amount < 10000) {
      this.snackBar.open('Minimal top up Rp10.000.', 'Tutup', { duration: 3000 });
      return;
    }

    this.isToppingUp.set(true);
    this.walletService.topUp(amount).subscribe({
      next: () => {
        this.isToppingUp.set(false);
        this.customAmount.set(null);
        this.snackBar.open('Top up berhasil.', 'Tutup', { duration: 3000 });
        this.load();
      },
      error: (err) => {
        this.isToppingUp.set(false);
        const message = typeof err.error === 'string' ? err.error : 'Top up gagal.';
        this.snackBar.open(message, 'Tutup', { duration: 3000 });
      }
    });
  }

  topUpCustom(): void {
    const amount = this.customAmount();
    if (!amount) {
      this.snackBar.open('Masukkan jumlah top up.', 'Tutup', { duration: 3000 });
      return;
    }
    this.topUp(amount);
  }
}
