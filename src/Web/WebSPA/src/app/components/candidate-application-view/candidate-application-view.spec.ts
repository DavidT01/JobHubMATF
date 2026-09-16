import { provideHttpClient } from '@angular/common/http';
import { provideHttpClientTesting } from '@angular/common/http/testing';
import { ComponentFixture, TestBed } from '@angular/core/testing';
import { provideRouter } from '@angular/router';

import { CandidateApplicationViewComponent } from './candidate-application-view';

describe('CandidateApplicationViewComponent', () => {
  let component: CandidateApplicationViewComponent;
  let fixture: ComponentFixture<CandidateApplicationViewComponent>;

  beforeEach(async () => {
    await TestBed.configureTestingModule({
      imports: [CandidateApplicationViewComponent],
      providers: [provideRouter([]), provideHttpClient(), provideHttpClientTesting()]
    })
    .compileComponents();

    fixture = TestBed.createComponent(CandidateApplicationViewComponent);
    component = fixture.componentInstance;
    await fixture.whenStable();
  });

  it('should create', () => {
    expect(component).toBeTruthy();
  });
});
