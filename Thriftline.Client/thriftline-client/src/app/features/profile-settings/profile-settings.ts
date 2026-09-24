import { Component, OnInit, inject, signal } from '@angular/core';
import { FormBuilder, ReactiveFormsModule, Validators } from '@angular/forms';
import { Router, RouterLink } from '@angular/router';
import { MatButtonModule } from '@angular/material/button';
import { MatFormFieldModule } from '@angular/material/form-field';
import { MatInputModule } from '@angular/material/input';
import { MatIconModule } from '@angular/material/icon';
import { MatSnackBar } from '@angular/material/snack-bar';
import { UserService } from '../../core/users/user';
import { UploadService } from '../../core/uploads/upload';
import { AuthService } from '../../core/auth/auth';
import { AppHeader } from '../../shared/app-header/app-header';

const PHONE_PATTERN = /^(\+62|62|0)8[1-9][0-9]{6,10}$/;

@Component({
  selector: 'app-profile-settings',
  standalone: true,
  imports: [ReactiveFormsModule, RouterLink, MatButtonModule, MatFormFieldModule, MatInputModule, MatIconModule, AppHeader],
  templateUrl: './profile-settings.html',
  styleUrl: './profile-settings.scss'
})
export class ProfileSettings implements OnInit {
  private fb = inject(FormBuilder);
  private userService = inject(UserService);
  private uploadService = inject(UploadService);
  private authService = inject(AuthService);
  private router = inject(Router);
  private snackBar = inject(MatSnackBar);

  isLoading = signal(true);
  isSubmitting = signal(false);
  isUploading = signal(false);
  avatarUrl = signal<string | null>(null);

  form = this.fb.nonNullable.group({
    fullName: ['', Validators.required],
    phoneNumber: ['', [Validators.required, Validators.pattern(PHONE_PATTERN)]],
  });

  ngOnInit(): void {
    this.userService.getMe().subscribe({
      next: (profile) => {
        this.form.patchValue({
          fullName: profile.fullName,
          phoneNumber: profile.phoneNumber ?? '',
        });
        this.avatarUrl.set(profile.profilePictureUrl);
        this.isLoading.set(false);
      },
      error: () => this.isLoading.set(false)
    });
  }

  onAvatarSelected(event: Event): void {
    const input = event.target as HTMLInputElement;
    if (!input.files || input.files.length === 0) return;

    const file = input.files[0];
    this.isUploading.set(true);

    this.uploadService.uploadMedia(file).subscribe({
      next: (result) => {
        this.avatarUrl.set(result.url);
        this.isUploading.set(false);
      },
      error: () => {
        this.snackBar.open('Gagal upload foto.', 'Tutup', { duration: 3000 });
        this.isUploading.set(false);
      }
    });

    input.value = '';
  }

  submit(): void {
    if (this.form.invalid) {
      this.form.markAllAsTouched();
      return;
    }
    if (this.isSubmitting()) return;

    const raw = this.form.getRawValue();
    this.isSubmitting.set(true);

    this.userService
      .updateMe({
        fullName: raw.fullName,
        phoneNumber: raw.phoneNumber,
        profilePictureUrl: this.avatarUrl() ?? undefined
      })
      .subscribe({
        next: (profile) => {
          this.authService.updateStoredFullName(profile.fullName);
          this.isSubmitting.set(false);
          this.snackBar.open('Profil diperbarui.', 'Tutup', { duration: 3000 });
          this.router.navigate(['/profile']);
        },
        error: (err) => {
          this.isSubmitting.set(false);
          const message = typeof err.error === 'string' ? err.error : 'Gagal memperbarui profil.';
          this.snackBar.open(message, 'Tutup', { duration: 3000 });
        }
      });
  }
}
