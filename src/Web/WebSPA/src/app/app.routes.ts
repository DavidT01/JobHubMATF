import { Routes } from '@angular/router';
import { applicationRoleGuard } from './core/guards/application-role.guard';
import { authGuard, guestGuard, adminGuard } from './core/guards/auth.guard';
import { LoginComponent } from './features/auth/login/login.component';
import { RegisterComponent } from './features/auth/register/register.component';
import { RegisterCompanyComponent } from './features/auth/register-company/register-company.component';
import { ConfirmEmailComponent } from './features/auth/confirm-email/confirm-email.component';
import { ForgotPasswordComponent } from './features/auth/forgot-password/forgot-password.component';
import { ResetPasswordComponent } from './features/auth/reset-password/reset-password.component';
import { HomeComponent } from './features/home/home.component';
import { NotificationsComponent } from './features/notifications/notifications.component';
import { AdminUsersComponent } from './features/admin/admin-users.component';

// Every page is lazy loaded so the initial bundle only contains the app shell.
export const routes: Routes = [
  {
    path: 'admin/statistics',
    loadComponent: () => import('./components/application-statistics/application-statistics-component')
      .then(c => c.ApplicationStatisticsComponent),
    canActivate: [applicationRoleGuard],
    data: { roles: ['Admin'] },
  },
  {
    path: 'applications-received',
    loadComponent: () => import('./components/employer-application-page/employer-application-page')
      .then(c => c.EmployerApplicationPage),
    canActivate: [applicationRoleGuard],
    data: { roles: ['Employer'] },
  },
  {
    path: 'applications',
    loadComponent: () => import('./components/candidate-applications/candidate-applications-component')
      .then(c => c.CandidateApplicationsComponent),
    canActivate: [applicationRoleGuard],
    data: { roles: ['Candidate'] },
  },
  { path: 'login', component: LoginComponent, canActivate: [guestGuard] },
  { path: 'register', component: RegisterComponent, canActivate: [guestGuard] },
  { path: 'register-company', component: RegisterCompanyComponent, canActivate: [guestGuard] },
  { path: 'confirm-email', component: ConfirmEmailComponent, canActivate: [guestGuard] },
  { path: 'forgot-password', component: ForgotPasswordComponent, canActivate: [guestGuard] },
  { path: 'reset-password', component: ResetPasswordComponent, canActivate: [guestGuard] },
  { path: 'notifications', component: NotificationsComponent, canActivate: [authGuard] },
  { path: 'admin', component: AdminUsersComponent, canActivate: [adminGuard] },
  {
    path: 'dashboard',
    loadComponent: () => import('./components/dashboard/dashboard').then(c => c.DashboardComponent),
    canActivate: [applicationRoleGuard],
    data: { roles: ['Candidate', 'Employer', 'Admin'] }
  },
  {
    path: 'chat',
    loadComponent: () => import('./components/chat/chat').then(c => c.ChatComponent),
    canActivate: [applicationRoleGuard],
    data: { roles: ['Candidate', 'Employer', 'Admin'] }
  },
  {
    path: 'profile/candidate/:userId',
    loadComponent: () => import('./components/candidate-profile/candidate-profile-component')
      .then(c => c.CandidateProfileComponent),
    canActivate: [authGuard]
  },
  {
    path: 'profile/company/:userId',
    loadComponent: () => import('./components/company-profile/company-profile-component')
      .then(c => c.CompanyProfileComponent),
    canActivate: [authGuard]
  },
  {
    path: 'recruitment-processes/:jobId',
    loadComponent: () => import('./components/recruitment-process/recruitment-process-component')
      .then(c => c.RecruitmentProcessComponent),
    canActivate: [applicationRoleGuard],
    data: { roles: ['Employer', 'Admin'] }
  },
  {
    path: 'recruitment-processes/:jobId/rounds/:selectionRoundId',
    loadComponent: () => import('./components/round-candidates/round-candidates-component')
      .then(c => c.RoundCandidatesComponent),
    canActivate: [applicationRoleGuard],
    data: { roles: ['Employer', 'Admin'] }
  },
  {
    path: 'applications/:applicationId',
    loadComponent: () => import('./components/candidate-application-view/candidate-application-view')
      .then(c => c.CandidateApplicationViewComponent),
    canActivate: [applicationRoleGuard],
    data: { roles: ['Candidate'] }
  },
  {
    path: 'candidates',
    loadComponent: () => import('./components/candidate-search/candidate-search').then(c => c.CandidateSearch),
    canActivate: [applicationRoleGuard],
    data: { roles: ['Employer'] }
  },
  { path: 'jobs', loadComponent: () => import('./components/job-list/job-list').then(c => c.JobList) },
  { path: 'search', loadComponent: () => import('./components/job-search/job-search').then(c => c.JobSearch) },
  {
    path: 'jobs/new',
    loadComponent: () => import('./components/job-create/job-create').then(c => c.JobCreate),
    canActivate: [applicationRoleGuard],
    data: { roles: ['Employer'] }
  },
  {
    path: 'jobs/:id/edit',
    loadComponent: () => import('./components/job-create/job-create').then(c => c.JobCreate),
    canActivate: [applicationRoleGuard],
    data: { roles: ['Employer'] }
  },
  {
    path: 'bookmarks',
    loadComponent: () => import('./components/saved-jobs/saved-jobs').then(c => c.SavedJobs),
    canActivate: [applicationRoleGuard],
    data: { roles: ['Candidate'] }
  },
  { path: 'jobs/:id', loadComponent: () => import('./components/job-details/job-details').then(c => c.JobDetails) },
  {
    path: '',
    loadComponent: () => import('./features/home/home.component').then(c => c.HomeComponent),
    canActivate: [authGuard]
  },
  { path: '**', redirectTo: '' }
];
