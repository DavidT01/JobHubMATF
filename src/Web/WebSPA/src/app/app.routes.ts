import { Routes } from '@angular/router';
import { JobList } from './components/job-list/job-list';
import { JobSearch } from './components/job-search/job-search';
import { JobDetails } from './components/job-details/job-details';
import { JobCreate } from './components/job-create/job-create';
import { SavedJobs } from './components/saved-jobs/saved-jobs';

export const routes: Routes = [
    {path: '' , component: JobList},
    {path: 'search' , component: JobSearch},
    {path: 'jobs/new' , component: JobCreate},
    {path: 'bookmarks' , component: SavedJobs},
    {path: 'jobs/:id' , component: JobDetails},
];
