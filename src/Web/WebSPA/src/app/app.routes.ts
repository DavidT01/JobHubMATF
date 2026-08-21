import { Routes } from '@angular/router';
import { JobList } from './components/job-list/job-list';
import { JobSearch } from './components/job-search/job-search';
import { JobDetails } from './components/job-details/job-details';

export const routes: Routes = [
    {path: '' , component: JobList},
    {path: 'search' , component: JobSearch},
    {path: 'jobs/:id' , component: JobDetails}
];
