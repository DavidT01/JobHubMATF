export type AppRole = 'Candidate' | 'Employer' | 'Admin';

export interface NavItem {
  label: string;
  icon: string;
  link: string;
  exact?: boolean;
}

const HOME: NavItem = { label: 'Home', icon: 'home', link: '/', exact: true };
const BROWSE_JOBS: NavItem = { label: 'Browse jobs', icon: 'work_outline', link: '/jobs', exact: true };
const SEARCH_JOBS: NavItem = { label: 'Search jobs', icon: 'search', link: '/search' };
const MESSAGES: NavItem = { label: 'Messages', icon: 'chat_bubble_outline', link: '/chat' };

const PUBLIC_NAVIGATION: readonly NavItem[] = [BROWSE_JOBS, SEARCH_JOBS];

const ROLE_NAVIGATION: Readonly<Record<AppRole, readonly NavItem[]>> = {
  Candidate: [
    HOME,
    BROWSE_JOBS,
    SEARCH_JOBS,
    { label: 'Saved jobs', icon: 'bookmark_border', link: '/bookmarks' },
    { label: 'My applications', icon: 'assignment', link: '/applications' },
    MESSAGES,
  ],
  Employer: [
    HOME,
    BROWSE_JOBS,
    { label: 'Post a job', icon: 'add_circle_outline', link: '/jobs/new' },
    { label: 'Applications', icon: 'inbox', link: '/applications-received' },
    { label: 'Find candidates', icon: 'person_search', link: '/candidates' },
    MESSAGES,
  ],
  Admin: [
    HOME,
    { label: 'Users', icon: 'manage_accounts', link: '/admin', exact: true },
    { label: 'Application statistics', icon: 'insights', link: '/admin/statistics' },
    BROWSE_JOBS,
  ],
};

const ROLE_PRECEDENCE: readonly AppRole[] = ['Admin', 'Employer', 'Candidate'];

/** Picks the role that drives the navigation when a user holds more than one. */
export function primaryRole(roles: readonly string[]): AppRole | null {
  return ROLE_PRECEDENCE.find(role => roles.includes(role)) ?? null;
}

export function navigationFor(role: AppRole | null): readonly NavItem[] {
  return role ? ROLE_NAVIGATION[role] : PUBLIC_NAVIGATION;
}

/** Profile pages resolve the signed-in user themselves, so the id segment is only a placeholder. */
export function profileLinkFor(role: AppRole | null): string | null {
  switch (role) {
    case 'Candidate': return '/profile/candidate/me';
    case 'Employer': return '/profile/company/me';
    default: return null;
  }
}

export function roleLabel(role: AppRole | null): string {
  switch (role) {
    case 'Candidate': return 'Candidate';
    case 'Employer': return 'Employer';
    case 'Admin': return 'Administrator';
    default: return '';
  }
}
