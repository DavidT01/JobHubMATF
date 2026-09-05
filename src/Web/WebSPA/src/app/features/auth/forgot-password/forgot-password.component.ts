import { Component, inject, signal } from '@angular/core';
import { CommonModule } from '@angular/common';
import { FormBuilder, ReactiveFormsModule, Validators } from '@angular/forms';
import { Router, RouterModule } from '@angular/router';
import { MatCardModule } from '@angular/material/card';
import { MatFormFieldModule } from '@angular/material/form-field';
import { MatInputModule } from '@angular/material/input';
import { MatButtonModule } from '@angular/material/button';
import { MatIconModule } from '@angular/material/icon';
import { MatSnackBar, MatSnackBarModule } from '@angular/material/snack-bar';
import { AuthService } from '../../../core/services/auth.service';

@Component({
  selector: 'app-forgot-password',
  standalone: true,
  imports: [
    CommonModule,
    ReactiveFormsModule,
    RouterModule,
    MatCardModule,
    MatFormFieldModule,
    MatInputModule,
    MatButtonModule,
    MatIconModule,
    MatSnackBarModule
  ],
  templateUrl: './forgot-password.component.html',
  styleUrl: '../login/login.component.scss'
})
export class ForgotPasswordComponent {
  private fb = inject(FormBuilder);
  private authService = inject(AuthService);
  private snackBar = inject(MatSnackBar);
  private router = inject(Router);

  form = this.fb.group({
    email: ['', [Validators.required, Validators.email]]
  });

  resetUrl = signal<string | null>(null);

  onSubmit(): void {
    if (this.form.invalid) {
      return;
    }

    const email = this.form.value.email!;
    this.authService.forgotPassword(email).subscribe({
      next: res => {
        this.snackBar.open(res.message, 'Close', { duration: 4000 });
        this.resetUrl.set(res.resetUrl ?? null);
        if (res.resetUrl) {
          // Course project without SMTP: open reset page directly from returned link.
          const url = new URL(res.resetUrl);
          this.router.navigate(['/reset-password'], {
            queryParams: {
              email: url.searchParams.get('email'),
              token: url.searchParams.get('token')
            }
          });
        }
      },
      error: err => {
        this.snackBar.open(err.error?.message || 'Request failed.', 'Close', { duration: 3000 });
      }
    });
  }
}
