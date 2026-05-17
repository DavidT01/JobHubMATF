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
  }
];
