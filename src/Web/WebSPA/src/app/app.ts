import { BreakpointObserver } from '@angular/cdk/layout';
import { Component, computed, inject, signal, viewChild } from '@angular/core';
import { takeUntilDestroyed, toSignal } from '@angular/core/rxjs-interop';
import {
  ActivatedRouteSnapshot,
  NavigationEnd,
  Router,
  RouterLink,
  RouterLinkActive,
  RouterOutlet
} from '@angular/router';
import { MatBadgeModule } from '@angular/material/badge';
import { MatButtonModule } from '@angular/material/button';
import { MatDividerModule } from '@angular/material/divider';
import { MatIconModule } from '@angular/material/icon';
import { MatListModule } from '@angular/material/list';
import { MatMenuModule } from '@angular/material/menu';
import { MatSidenav, MatSidenavModule } from '@angular/material/sidenav';
import { MatToolbarModule } from '@angular/material/toolbar';
import { MatTooltipModule } from '@angular/material/tooltip';
import { filter, map } from 'rxjs';

import { navigationFor, profileLinkFor } from './core/layout/navigation';
import { AuthService } from './core/services/auth.service';
import { SessionService } from './core/services/session.service';

@Component({
  selector: 'app-root',
  standalone: true,
  imports: [
    RouterOutlet,
    RouterLink,
    RouterLinkActive,
    MatBadgeModule,
    MatButtonModule,
    MatDividerModule,
    MatIconModule,
    MatListModule,
    MatMenuModule,
    MatSidenavModule,
    MatToolbarModule,
    MatTooltipModule
  ],
  templateUrl: './app.html',
  styleUrls: ['./app.scss']
})
export class AppComponent {
  private readonly router = inject(Router);
  private readonly sidenav = viewChild(MatSidenav);
  protected readonly auth = inject(AuthService);
  protected readonly session = inject(SessionService);

  protected readonly isHandset = toSignal(
    inject(BreakpointObserver).observe('(max-width: 959.98px)').pipe(map(state => state.matches)),
    { initialValue: false }
  );

  private readonly navigated = signal(false);
  private readonly authLayout = signal(false);

  /** Header and sidebar are hidden on sign-in style pages and until the first route resolves. */
  protected readonly showChrome = computed(() => this.navigated() && !this.authLayout());
  protected readonly navItems = computed(() => navigationFor(this.session.role()));
  protected readonly profileLink = computed(() => profileLinkFor(this.session.role()));

  constructor() {
    this.router.events.pipe(
      filter((event): event is NavigationEnd => event instanceof NavigationEnd),
      takeUntilDestroyed()
    ).subscribe(() => {
      this.authLayout.set(this.usesAuthLayout(this.router.routerState.snapshot.root));
      this.navigated.set(true);

      if (!this.authLayout()) {
        this.session.sync();
      }
      if (this.isHandset()) {
        this.sidenav()?.close();
      }
    });
  }

  protected toggleNavigation(): void {
    this.sidenav()?.toggle();
  }

  protected signOut(): void {
    this.session.signOut();
  }

  private usesAuthLayout(route: ActivatedRouteSnapshot | null): boolean {
    for (let current = route; current; current = current.firstChild) {
      if (current.data['layout'] === 'auth') {
        return true;
      }
    }
    return false;
  }
}
