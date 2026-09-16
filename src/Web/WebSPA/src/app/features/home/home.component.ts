import { Component, computed, inject } from '@angular/core';
import { RouterLink } from '@angular/router';
import { MatCardModule } from '@angular/material/card';
import { MatIconModule } from '@angular/material/icon';
import { navigationFor } from '../../core/layout/navigation';
import { SessionService } from '../../core/services/session.service';
import { PageHeaderComponent } from '../../shared/page-header/page-header.component';

@Component({
  selector: 'app-home',
  standalone: true,
  imports: [MatCardModule, MatIconModule, RouterLink, PageHeaderComponent],
  templateUrl: './home.component.html',
  styleUrl: './home.component.scss'
})
export class HomeComponent {
  private readonly session = inject(SessionService);

  protected readonly greeting = computed(() => {
    const user = this.session.user();
    const name = user?.firstName || this.session.displayName();
    return name ? `Welcome back, ${name}` : 'Welcome to JobHub';
  });

  protected readonly subtitle = computed(() => {
    switch (this.session.role()) {
      case 'Candidate': return 'Find your next role and keep track of your applications.';
      case 'Employer': return 'Post jobs, review applications, and reach out to candidates.';
      case 'Admin': return 'Manage user accounts and review platform activity.';
      default: return 'Pick a section to get started.';
    }
  });

  protected readonly quickLinks = computed(() =>
    navigationFor(this.session.role()).filter(item => item.link !== '/')
  );
}
