import { Component, OnInit, inject, signal } from '@angular/core';
import { CommonModule } from '@angular/common';
import { ActivatedRoute, Router, RouterModule } from '@angular/router';
import { MatCardModule } from '@angular/material/card';
import { MatButtonModule } from '@angular/material/button';
import { MatProgressSpinnerModule } from '@angular/material/progress-spinner';
import { AuthService } from '../../../core/services/auth.service';

@Component({
  selector: 'app-confirm-email',
  standalone: true,
  imports: [CommonModule, RouterModule, MatCardModule, MatButtonModule, MatProgressSpinnerModule],
  templateUrl: './confirm-email.component.html',
  styleUrl: '../login/login.component.scss'
})
export class ConfirmEmailComponent implements OnInit {
  private route = inject(ActivatedRoute);
  private router = inject(Router);
  private authService = inject(AuthService);

  status = signal<'loading' | 'success' | 'error'>('loading');
  message = signal('Confirming your email...');

  ngOnInit(): void {
    const userId = this.route.snapshot.queryParamMap.get('userId');
    const token = this.route.snapshot.queryParamMap.get('token');

    if (!userId || !token) {
      this.status.set('error');
      this.message.set('Invalid confirmation link.');
      return;
    }

    this.authService.confirmEmail({ userId, token }).subscribe({
      next: res => {
        this.status.set('success');
        this.message.set(res.message || 'Email confirmed successfully.');
      },
      error: err => {
        this.status.set('error');
        this.message.set(err.error?.message || 'Email confirmation failed.');
      }
    });
  }

  goToLogin(): void {
    this.router.navigate(['/login']);
  }
}
