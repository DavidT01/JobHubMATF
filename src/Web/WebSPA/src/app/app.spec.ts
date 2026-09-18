import { Component } from '@angular/core';
import { TestBed } from '@angular/core/testing';
import { provideHttpClient } from '@angular/common/http';
import { provideHttpClientTesting } from '@angular/common/http/testing';
import { Router, provideRouter } from '@angular/router';
import { AppComponent } from './app';

@Component({ template: '<p>page</p>' })
class StubPage {}

describe('AppComponent', () => {
  beforeEach(async () => {
    localStorage.removeItem('auth_token');
    await TestBed.configureTestingModule({
      imports: [AppComponent],
      providers: [
        provideRouter([
          { path: 'login', component: StubPage, data: { layout: 'auth' } },
          { path: 'jobs', component: StubPage },
        ]),
        provideHttpClient(),
        provideHttpClientTesting(),
      ]
    }).compileComponents();
  });

  async function renderAt(url: string) {
    const fixture = TestBed.createComponent(AppComponent);
    await TestBed.inject(Router).navigateByUrl(url);
    fixture.detectChanges();
    await fixture.whenStable();
    fixture.detectChanges();
    return fixture.nativeElement as HTMLElement;
  }

  it('should create the app', () => {
    const fixture = TestBed.createComponent(AppComponent);
    expect(fixture.componentInstance).toBeTruthy();
  });

  it('shows the header and public navigation to visitors', async () => {
    const element = await renderAt('/jobs');

    expect(element.querySelector('mat-toolbar')).not.toBeNull();
    expect(element.textContent).toContain('Sign in');
    expect(element.textContent).toContain('Browse jobs');
    expect(element.textContent).toContain('page');
  });

  it('hides the header on sign-in pages', async () => {
    const element = await renderAt('/login');

    expect(element.querySelector('mat-toolbar')).toBeNull();
    expect(element.textContent).toContain('page');
  });
});
