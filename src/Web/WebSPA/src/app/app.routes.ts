import { Routes } from '@angular/router';
import { applicationRoleGuard } from './core/guards/application-role.guard';
import { JobList } from './components/job-list/job-list';
import { JobSearch } from './components/job-search/job-search';
import { JobDetails } from './components/job-details/job-details';
import { JobCreate } from './components/job-create/job-create';
import { SavedJobs } from './components/saved-jobs/saved-jobs';
import { ChatComponent } from './components/chat/chat';
import { DashboardComponent } from './components/dashboard/dashboard';
import { roleGuard } from './guards/role';
import { authGuard, guestGuard, adminGuard } from './core/guards/auth.guard';
import { LoginComponent } from './features/auth/login/login.component';
import { RegisterComponent } from './features/auth/register/register.component';
import { ConfirmEmailComponent } from './features/auth/confirm-email/confirm-email.component';
import { ForgotPasswordComponent } from './features/auth/forgot-password/forgot-password.component';
import { ResetPasswordComponent } from './features/auth/reset-password/reset-password.component';
import { HomeComponent } from './features/home/home.component';
import { NotificationsComponent } from './features/notifications/notifications.component';
import { AdminUsersComponent } from './features/admin/admin-users.component';

export const routes: Routes = [
  {
    path: 'applications',
    loadComponent: () => import('./components/candidate-applications/candidate-applications-component')
      .then(c => c.CandidateApplicationsComponent),
    canActivate: [applicationRoleGuard],
    data: { roles: ['Candidate'] },
  },
  { path: 'login', component: LoginComponent, canActivate: [guestGuard] },
  { path: 'register', component: RegisterComponent, canActivate: [guestGuard] },
  { path: 'confirm-email', component: ConfirmEmailComponent, canActivate: [guestGuard] },
  { path: 'forgot-password', component: ForgotPasswordComponent, canActivate: [guestGuard] },
  { path: 'reset-password', component: ResetPasswordComponent, canActivate: [guestGuard] },
  { path: 'notifications', component: NotificationsComponent, canActivate: [authGuard] },
  { path: 'admin', component: AdminUsersComponent, canActivate: [adminGuard] },
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
  {
    path: 'profile/candidate/:userId',
    loadComponent: () => import('./components/candidate-profile/candidate-profile-component')
      .then(c => c.CandidateProfileComponent)
  },
  {
    path: 'profile/company/:userId',
    loadComponent: () => import('./components/company-profile/company-profile-component')
      .then(c => c.CompanyProfileComponent)
  },
  {
    path: 'recruitment-processes/:jobId',
    loadComponent: () => import('./components/recruitment-process/recruitment-process-component')
      .then(c => c.RecruitmentProcessComponent)
  },
  {
    path: 'recruitment-processes/:jobId/rounds/:selectionRoundId',
    loadComponent: () => import('./components/round-candidates/round-candidates-component')
      .then(c => c.RoundCandidatesComponent)
  },
  {
    path: 'applications/:applicationId',
    loadComponent: () => import('./components/candidate-application-view/candidate-application-view')
      .then(c => c.CandidateApplicationViewComponent)
  },
  { path: 'jobs', component: JobList },
  { path: 'search', component: JobSearch },
  { path: 'jobs/new', component: JobCreate },
  { path: 'bookmarks', component: SavedJobs },
  { path: 'jobs/:id', component: JobDetails },
  { path: '', component: HomeComponent, canActivate: [authGuard] },
  { path: '**', redirectTo: '' }
];
