import { Component, OnInit, inject, signal } from '@angular/core';
import { FormsModule } from '@angular/forms';
import { RouterLink } from '@angular/router';
import { MatButtonModule } from '@angular/material/button';
import { MatCardModule } from '@angular/material/card';
import { MatFormFieldModule } from '@angular/material/form-field';
import { MatSelectModule } from '@angular/material/select';
import { AdminService, AdminUser } from '../../core/services/admin.service';

@Component({
  selector: 'app-admin-users',
  standalone: true,
  imports: [
    FormsModule,
    MatButtonModule,
    MatCardModule,
    MatFormFieldModule,
    MatSelectModule,
    RouterLink
  ],
  templateUrl: './admin-users.component.html',
  styleUrl: './admin-users.component.scss'
})
export class AdminUsersComponent implements OnInit {
  private adminService = inject(AdminService);

  users = signal<AdminUser[]>([]);
  error = signal<string | null>(null);
  loading = signal(true);
  readonly roles = ['Candidate', 'Employer', 'Admin'];

  ngOnInit(): void {
    this.reload();
  }

  reload(): void {
    this.loading.set(true);
    this.adminService.listUsers().subscribe({
      next: list => {
        this.users.set(list);
        this.loading.set(false);
        this.error.set(null);
      },
      error: () => {
        this.error.set('Could not load users. Admin role required.');
        this.loading.set(false);
      }
    });
  }

  currentRole(user: AdminUser): string {
    return user.roles[0] ?? 'Candidate';
  }

  onRoleChange(user: AdminUser, role: string): void {
    this.adminService.setRole(user.id, role).subscribe({
      next: () => this.reload(),
      error: () => this.error.set('Failed to update role.')
    });
  }

  lock(user: AdminUser): void {
    this.adminService.lockUser(user.id).subscribe({
      next: () => this.reload(),
      error: () => this.error.set('Failed to lock user.')
    });
  }

  unlock(user: AdminUser): void {
    this.adminService.unlockUser(user.id).subscribe({
      next: () => this.reload(),
      error: () => this.error.set('Failed to unlock user.')
    });
  }
}
