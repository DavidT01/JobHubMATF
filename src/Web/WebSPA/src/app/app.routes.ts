import { Routes } from '@angular/router';

export const routes: Routes = [
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
    path: 'applications/:applicationId',
    loadComponent: () => import('./components/candidate-application-view/candidate-application-view')
      .then(c => c.CandidateApplicationViewComponent)
  }
];

