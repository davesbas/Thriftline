import { Component, inject, signal } from '@angular/core';
import { ReactiveFormsModule, FormBuilder, Validators } from '@angular/forms';
import { Router, RouterLink } from '@angular/router';
import { MatFormFieldModule } from '@angular/material/form-field';
import { MatInputModule } from '@angular/material/input';
import { MatButtonModule } from '@angular/material/button';
import { MatCardModule } from '@angular/material/card';
import { AuthService } from '../../../core/auth/auth';

const PHONE_PATTERN = /^(\+62|62|0)8[1-9][0-9]{6,10}$/;

@Component({
  selector: 'app-register',
  standalone: true,
  imports: [ReactiveFormsModule, RouterLink, MatFormFieldModule, MatInputModule, MatButtonModule, MatCardModule],
  templateUrl: './register.html',
  styleUrl: './register.scss'
})
export class Register {
  private fb = inject(FormBuilder);
  private authService = inject(AuthService);
  private router = inject(Router);

  errorMessage = signal<string | null>(null);
  isSubmitting = signal(false);

  registerForm = this.fb.nonNullable.group({
    fullName: ['', Validators.required],
    email: ['', [Validators.required, Validators.email]],
    password: ['', [Validators.required, Validators.minLength(6)]],
    phoneNumber: ['', [Validators.required, Validators.pattern(PHONE_PATTERN)]]
  });

  onSubmit(): void {
    if (this.registerForm.invalid) {
      this.registerForm.markAllAsTouched();
      return;
    }

    this.errorMessage.set(null);
    this.isSubmitting.set(true);

    const raw = this.registerForm.getRawValue();

    this.authService.register({
      fullName: raw.fullName,
      email: raw.email,
      password: raw.password,
      phoneNumber: raw.phoneNumber
    }).subscribe({
      next: () => {
        this.isSubmitting.set(false);
        this.router.navigate(['/']);
      },
      error: (err) => {
        this.isSubmitting.set(false);
        const message = typeof err.error === 'string' ? err.error : null;
        this.errorMessage.set(err.status === 409 ? 'Email sudah digunakan.' : (message ?? 'Registrasi gagal, coba lagi.'));
      }
    });
  }
}
