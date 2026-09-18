import { Component, inject } from '@angular/core';
import { CommonModule } from '@angular/common';
import { FormsModule, ReactiveFormsModule, FormBuilder, Validators } from '@angular/forms';
import { Router, RouterModule } from '@angular/router';
import { MatCardModule } from '@angular/material/card';
import { MatFormFieldModule } from '@angular/material/form-field';
import { MatInputModule } from '@angular/material/input';
import { MatButtonModule } from '@angular/material/button';
import { MatIconModule } from '@angular/material/icon';
import { MatSnackBar, MatSnackBarModule } from '@angular/material/snack-bar';
import { AuthService } from '../../../core/services/auth.service';

@Component({
  selector: 'app-register-company',
  standalone: true,
  imports: [
    CommonModule,
    FormsModule,
    ReactiveFormsModule,
    RouterModule,
    MatCardModule,
    MatFormFieldModule,
    MatInputModule,
    MatButtonModule,
    MatIconModule,
    MatSnackBarModule
  ],
  templateUrl: './register-company.component.html',
  styleUrl: './register-company.component.scss'
})
export class RegisterCompanyComponent {
  private fb = inject(FormBuilder);
  private authService = inject(AuthService);
  private router = inject(Router);
  private snackBar = inject(MatSnackBar);

  registerForm = this.fb.group({
    companyName: ['', [Validators.required]],
    email: ['', [Validators.required, Validators.email]],
    password: ['', [Validators.required, Validators.minLength(6)]]
  });

  hidePassword = true;

  onSubmit(): void {
    if (this.registerForm.invalid) {
      return;
    }

    const value = this.registerForm.getRawValue();
    this.authService.register({
      firstName: value.companyName,
      lastName: null,
      email: value.email,
      password: value.password,
      role: 'Employer'
    }).subscribe({
      next: (res) => {
        this.snackBar.open(res.message || 'Company registration successful!', 'Close', { duration: 5000 });
        const url = new URL(res.confirmationUrl);
        this.router.navigate(['/confirm-email'], {
          queryParams: {
            userId: url.searchParams.get('userId'),
            token: url.searchParams.get('token')
          }
        });
      },
      error: (err) => {
        this.snackBar.open(err.error?.message || 'Registration failed!', 'Close', { duration: 3000 });
      }
    });
  }
}
