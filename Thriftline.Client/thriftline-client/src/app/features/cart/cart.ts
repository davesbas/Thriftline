import { Component, OnInit, inject, signal, computed } from '@angular/core';
import { Router } from '@angular/router';
import { MatButtonModule } from '@angular/material/button';
import { MatIconModule } from '@angular/material/icon';
import { MatSnackBar } from '@angular/material/snack-bar';
import { CartService, CartItemResponse } from '../../core/cart/cart';
import { AppHeader } from '../../shared/app-header/app-header';

interface StoreGroup {
  storeId: string;
  storeName: string;
  items: CartItemResponse[];
}

@Component({
  selector: 'app-cart',
  standalone: true,
  imports: [MatButtonModule, MatIconModule, AppHeader],
  templateUrl: './cart.html',
  styleUrl: './cart.scss'
})
export class Cart implements OnInit {
  private cartService = inject(CartService);
  private router = inject(Router);
  private snackBar = inject(MatSnackBar);

  items = signal<CartItemResponse[]>([]);
  isLoading = signal(true);
  selectedItemIds = signal<Set<string>>(new Set());

  storeGroups = computed<StoreGroup[]>(() => {
    const groups = new Map<string, StoreGroup>();
    for (const item of this.items()) {
      let group = groups.get(item.storeId);
      if (!group) {
        group = { storeId: item.storeId, storeName: item.storeName, items: [] };
        groups.set(item.storeId, group);
      }
      group.items.push(item);
    }
    return Array.from(groups.values());
  });

  selectedSubtotal = computed(() => {
    const selected = this.selectedItemIds();
    return this.items()
      .filter((item) => item.isAvailable && selected.has(item.id))
      .reduce((sum, item) => sum + item.subtotal, 0);
  });

  ngOnInit(): void {
    this.loadCart();
  }

  loadCart(): void {
    this.isLoading.set(true);
    this.cartService.getSummary().subscribe({
      next: (data) => {
        this.items.set(data.items);
        this.selectedItemIds.set(new Set(data.items.filter((i) => i.isAvailable).map((i) => i.id)));
        this.isLoading.set(false);
      },
      error: () => this.isLoading.set(false)
    });
  }

  isItemSelected(itemId: string): boolean {
    return this.selectedItemIds().has(itemId);
  }

  toggleItem(item: CartItemResponse): void {
    if (!item.isAvailable) return;

    this.selectedItemIds.update((set) => {
      const next = new Set(set);
      if (next.has(item.id)) {
        next.delete(item.id);
      } else {
        next.add(item.id);
      }
      return next;
    });
  }

  isStoreFullySelected(group: StoreGroup): boolean {
    const available = group.items.filter((i) => i.isAvailable);
    return available.length > 0 && available.every((i) => this.selectedItemIds().has(i.id));
  }

  toggleStore(group: StoreGroup): void {
    const shouldSelect = !this.isStoreFullySelected(group);

    this.selectedItemIds.update((set) => {
      const next = new Set(set);
      for (const item of group.items) {
        if (!item.isAvailable) continue;
        if (shouldSelect) {
          next.add(item.id);
        } else {
          next.delete(item.id);
        }
      }
      return next;
    });
  }

  changeQuantity(item: CartItemResponse, delta: number): void {
    const newQuantity = item.quantity + delta;
    if (newQuantity < 1) return;

    this.cartService.updateQuantity(item.id, newQuantity).subscribe({
      next: (data) => this.items.set(data.items),
      error: () => this.snackBar.open('Gagal mengubah quantity.', 'Tutup', { duration: 3000 })
    });
  }

  removeItem(item: CartItemResponse): void {
    this.cartService.removeItem(item.id).subscribe({
      next: () => {
        this.selectedItemIds.update((set) => {
          const next = new Set(set);
          next.delete(item.id);
          return next;
        });
        this.loadCart();
      },
      error: () => this.snackBar.open('Gagal menghapus item.', 'Tutup', { duration: 3000 })
    });
  }

  formatPrice(price: number): string {
    return new Intl.NumberFormat('id-ID', {
      style: 'currency',
      currency: 'IDR',
      maximumFractionDigits: 0
    }).format(price);
  }

  goToCheckout(): void {
    const selectedIds = Array.from(this.selectedItemIds());
    if (selectedIds.length === 0) {
      this.snackBar.open('Pilih minimal satu produk untuk checkout.', 'Tutup', { duration: 3000 });
      return;
    }

    this.router.navigate(['/checkout'], { queryParams: { items: selectedIds.join(',') } });
  }
}
