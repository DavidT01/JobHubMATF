import { Routes } from '@angular/router';

export const routes: Routes = [
  {
    path: 'profile/candidate/:userId',
    loadComponent: () => import('./components/candidate-profile/candidate-profile-component')
      .then(c => c.CandidateProfileComponent)
  }
];
