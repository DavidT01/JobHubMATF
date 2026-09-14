import { ComponentFixture, TestBed } from '@angular/core/testing';
import { provideHttpClient } from '@angular/common/http';
import { provideHttpClientTesting } from '@angular/common/http/testing';
import { provideRouter } from '@angular/router';

import { CandidateProfileComponent } from './candidate-profile-component';

describe('CandidateProfile', () => {
  let component: CandidateProfileComponent;
  let fixture: ComponentFixture<CandidateProfileComponent>;

  beforeEach(async () => {
    await TestBed.configureTestingModule({
      imports: [CandidateProfileComponent],
      providers: [provideRouter([]), provideHttpClient(), provideHttpClientTesting()]
    })
    .compileComponents();

    fixture = TestBed.createComponent(CandidateProfileComponent);
    component = fixture.componentInstance;
    await fixture.whenStable();
  });

  it('should create', () => {
    expect(component).toBeTruthy();
  });
});
