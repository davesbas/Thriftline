import { Component, OnInit, inject, signal } from '@angular/core';
import { RouterLink } from '@angular/router';
import { MatIconModule } from '@angular/material/icon';
import { UserService, UserProfileResponse } from '../../core/users/user';
import { StoreService, StoreResponse } from '../../core/stores/store';
import { AddressService, AddressResponse } from '../../core/addresses/address';
import { AppHeader } from '../../shared/app-header/app-header';
import { WalletService } from '../../core/wallets/wallet';

@Component({
  selector: 'app-profile',
  standalone: true,
  imports: [RouterLink, MatIconModule, AppHeader],
  templateUrl: './profile.html',
  styleUrl: './profile.scss'
})
export class Profile implements OnInit {
  private userService = inject(UserService);
  private storeService = inject(StoreService);
  private addressService = inject(AddressService);
  private walletService = inject(WalletService);

  profile = signal<UserProfileResponse | null>(null);
  myStore = signal<StoreResponse | null>(null);
  primaryAddress = signal<AddressResponse | null>(null);
  isLoading = signal(true);
  loadError = signal<string | null>(null);
  walletBalance = signal(0);

  formatPrice(price: number): string {
    return new Intl.NumberFormat('id-ID', {
      style: 'currency',
      currency: 'IDR',
      maximumFractionDigits: 0
    }).format(price);
  }

  ngOnInit(): void {
    this.userService.getMe().subscribe({
      next: (data) => {
        this.profile.set(data);
        this.isLoading.set(false);
      },
      error: (err) => {
        this.isLoading.set(false);
        const message = typeof err.error === 'string' ? err.error : 'Gagal memuat profil. Coba muat ulang halaman.';
        this.loadError.set(message);
      }
    });

    this.storeService.getMyStore().subscribe((store) => this.myStore.set(store));

    this.addressService.getMine().subscribe((addresses) => {
      this.primaryAddress.set(addresses.find((a) => a.isPrimary) ?? addresses[0] ?? null);
    });

    this.walletService.getMyWallet().subscribe((wallet) => this.walletBalance.set(wallet.balance));
  }
}
