import { navigationFor, primaryRole, profileLinkFor } from './navigation';

describe('navigation', () => {
  it('prefers the most privileged role when a user has several', () => {
    expect(primaryRole(['Candidate', 'Admin'])).toBe('Admin');
    expect(primaryRole(['Candidate', 'Employer'])).toBe('Employer');
    expect(primaryRole(['Candidate'])).toBe('Candidate');
    expect(primaryRole([])).toBeNull();
    expect(primaryRole(['Unknown'])).toBeNull();
  });

  it('shows only public job pages to visitors', () => {
    expect(navigationFor(null).map(item => item.link)).toEqual(['/jobs', '/search']);
  });

  it('gives each role its own entry points', () => {
    const links = (role: Parameters<typeof navigationFor>[0]) => navigationFor(role).map(item => item.link);

    expect(links('Candidate')).toEqual(['/', '/jobs', '/search', '/bookmarks', '/applications', '/chat']);
    expect(links('Employer')).toEqual(['/', '/jobs', '/jobs/new', '/applications-received', '/candidates', '/chat']);
    expect(links('Admin')).toEqual(['/', '/admin', '/admin/statistics', '/jobs']);
  });

  it('links to the profile page that matches the role', () => {
    expect(profileLinkFor('Candidate')).toBe('/profile/candidate/me');
    expect(profileLinkFor('Employer')).toBe('/profile/company/me');
    expect(profileLinkFor('Admin')).toBeNull();
    expect(profileLinkFor(null)).toBeNull();
  });
});
