import { Routes } from '@angular/router';
import { ChatComponent } from './components/chat/chat';
import { DashboardComponent } from './components/dashboard/dashboard';
import { roleGuard } from './guards/role';

export const routes: Routes = [
  { path: '', redirectTo: 'dashboard', pathMatch: 'full' },
  {
    path: 'dashboard',
    component: DashboardComponent,
    canActivate: [roleGuard],
    data: { roles: ['Candidate', 'Employer', 'Admin'] }
  },
  {
    path: 'chat',
    component: ChatComponent,
    canActivate: [roleGuard],
    data: { roles: ['Candidate', 'Employer', 'Admin'] }
  },
  { path: '**', redirectTo: 'dashboard' }
];
