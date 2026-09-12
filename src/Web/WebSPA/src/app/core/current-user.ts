import { Injectable } from '@angular/core';

@Injectable({
  providedIn: 'root',
})
export class CurrentUser {
  private readonly userId = 'test-user-123' // placeholder 

  getUserId(): string {
    return this.userId;
  }
}
