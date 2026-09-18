import { Routes } from '@angular/router';
import { applicationRoleGuard } from './core/guards/application-role.guard';
import { authGuard, guestGuard, adminGuard } from './core/guards/auth.guard';

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
  {
    path: 'login',
    loadComponent: () => import('./features/auth/login/login.component').then(c => c.LoginComponent),
    canActivate: [guestGuard],
    data: { layout: 'auth' }
  },
  {
    path: 'register',
    loadComponent: () => import('./features/auth/register/register.component').then(c => c.RegisterComponent),
    canActivate: [guestGuard],
    data: { layout: 'auth' }
  },
  {
    path: 'confirm-email',
    loadComponent: () => import('./features/auth/confirm-email/confirm-email.component').then(c => c.ConfirmEmailComponent),
    canActivate: [guestGuard],
    data: { layout: 'auth' }
  },
  {
    path: 'forgot-password',
    loadComponent: () => import('./features/auth/forgot-password/forgot-password.component').then(c => c.ForgotPasswordComponent),
    canActivate: [guestGuard],
    data: { layout: 'auth' }
  },
  {
    path: 'reset-password',
    loadComponent: () => import('./features/auth/reset-password/reset-password.component').then(c => c.ResetPasswordComponent),
    canActivate: [guestGuard],
    data: { layout: 'auth' }
  },
  {
    path: 'notifications',
    loadComponent: () => import('./features/notifications/notifications.component').then(c => c.NotificationsComponent),
    canActivate: [authGuard]
  },
  {
    path: 'admin',
    loadComponent: () => import('./features/admin/admin-users.component').then(c => c.AdminUsersComponent),
    canActivate: [adminGuard]
  },
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
  {
    path: 'candidates',
    loadComponent: () => import('./components/candidate-search/candidate-search').then(c => c.CandidateSearch),
    canActivate: [applicationRoleGuard],
    data: { roles: ['Employer'] }
  },
  { path: 'jobs', loadComponent: () => import('./components/job-list/job-list').then(c => c.JobList) },
  { path: 'search', loadComponent: () => import('./components/job-search/job-search').then(c => c.JobSearch) },
  { path: 'jobs/new', loadComponent: () => import('./components/job-create/job-create').then(c => c.JobCreate) },
  { path: 'bookmarks', loadComponent: () => import('./components/saved-jobs/saved-jobs').then(c => c.SavedJobs) },
  { path: 'jobs/:id', loadComponent: () => import('./components/job-details/job-details').then(c => c.JobDetails) },
  {
    path: '',
    loadComponent: () => import('./features/home/home.component').then(c => c.HomeComponent),
    canActivate: [authGuard]
  },
  { path: '**', redirectTo: '' }
];
