import { Routes } from '@angular/router';
import { JobList } from './components/job-list/job-list';
import { JobSearch } from './components/job-search/job-search';
import { JobDetails } from './components/job-details/job-details';
import { JobCreate } from './components/job-create/job-create';

export const routes: Routes = [
    {path: '' , component: JobList},
    {path: 'search' , component: JobSearch},
    {path: 'jobs/new' , component: JobCreate},
    {path: 'jobs/:id' , component: JobDetails},
];
