import { Injectable, inject } from '@angular/core';
import { Observable, map, shareReplay } from 'rxjs';
import { AuthService } from './services/auth.service';

@Injectable({
  providedIn: 'root',
})
export class CurrentUser {
  private authService = inject(AuthService);
  private userId$?: Observable<string>;

  getUserId(): Observable<string> {
    if (!this.userId$) {
      this.userId$ = this.authService.me().pipe(
        map((me) => me.id),
        shareReplay(1)
      );
    }
    return this.userId$;
  }

  /** Forgets the cached id so the next caller loads the newly signed-in user. */
  clear(): void {
    this.userId$ = undefined;
  }
}
