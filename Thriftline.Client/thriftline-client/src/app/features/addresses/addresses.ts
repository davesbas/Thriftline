import { Component, OnInit, inject, signal } from '@angular/core';
import { RouterLink } from '@angular/router';
import { FormBuilder, ReactiveFormsModule, Validators } from '@angular/forms';
import { MatButtonModule } from '@angular/material/button';
import { MatIconModule } from '@angular/material/icon';
import { MatFormFieldModule } from '@angular/material/form-field';
import { MatInputModule } from '@angular/material/input';
import { MatSnackBar } from '@angular/material/snack-bar';
import { AddressService, AddressResponse } from '../../core/addresses/address';
import { AppHeader } from '../../shared/app-header/app-header';

@Component({
  selector: 'app-addresses',
  standalone: true,
  imports: [
    RouterLink,
    ReactiveFormsModule,
    MatButtonModule,
    MatIconModule,
    MatFormFieldModule,
    MatInputModule,
    AppHeader
  ],
  templateUrl: './addresses.html',
  styleUrl: './addresses.scss'
})
export class Addresses implements OnInit {
  private fb = inject(FormBuilder);
  private addressService = inject(AddressService);
  private snackBar = inject(MatSnackBar);

  addresses = signal<AddressResponse[]>([]);
  isLoading = signal(true);
  isSaving = signal(false);
  showForm = signal(false);
  editingId = signal<string | null>(null);

  form = this.fb.nonNullable.group({
    label: ['', Validators.required],
    recipientName: ['', Validators.required],
    phoneNumber: ['', Validators.required],
    fullAddress: ['', Validators.required],
    isPrimary: [false]
  });

  ngOnInit(): void {
    this.load();
  }

  load(): void {
    this.isLoading.set(true);
    this.addressService.getMine().subscribe({
      next: (data) => {
        this.addresses.set(data);
        this.isLoading.set(false);
      },
      error: () => this.isLoading.set(false)
    });
  }

  openAddForm(): void {
    this.editingId.set(null);
    this.form.reset({
      label: '',
      recipientName: '',
      phoneNumber: '',
      fullAddress: '',
      isPrimary: this.addresses().length === 0
    });
    this.showForm.set(true);
  }

  openEditForm(address: AddressResponse): void {
    this.editingId.set(address.id);
    this.form.reset({
      label: address.label,
      recipientName: address.recipientName,
      phoneNumber: address.phoneNumber,
      fullAddress: address.fullAddress,
      isPrimary: address.isPrimary
    });
    this.showForm.set(true);
  }

  cancelForm(): void {
    this.showForm.set(false);
    this.editingId.set(null);
  }

  submit(): void {
    if (this.form.invalid) {
      this.form.markAllAsTouched();
      return;
    }
    if (this.isSaving()) return;

    const raw = this.form.getRawValue();
    this.isSaving.set(true);

    const id = this.editingId();
    const request$ = id ? this.addressService.update(id, raw) : this.addressService.create(raw);

    request$.subscribe({
      next: () => {
        this.isSaving.set(false);
        this.showForm.set(false);
        this.editingId.set(null);
        this.snackBar.open('Alamat disimpan.', 'Tutup', { duration: 3000 });
        this.load();
      },
      error: (err) => {
        this.isSaving.set(false);
        const message = typeof err.error === 'string' ? err.error : 'Gagal menyimpan alamat.';
        this.snackBar.open(message, 'Tutup', { duration: 3000 });
      }
    });
  }

  setPrimary(address: AddressResponse): void {
    if (address.isPrimary) return;

    this.addressService.setPrimary(address.id).subscribe({
      next: () => this.load(),
      error: () => this.snackBar.open('Gagal mengubah alamat utama.', 'Tutup', { duration: 3000 })
    });
  }

  remove(address: AddressResponse): void {
    if (!confirm(`Hapus alamat "${address.label}"?`)) return;

    this.addressService.remove(address.id).subscribe({
      next: () => this.load(),
      error: () => this.snackBar.open('Gagal menghapus alamat.', 'Tutup', { duration: 3000 })
    });
  }
}
