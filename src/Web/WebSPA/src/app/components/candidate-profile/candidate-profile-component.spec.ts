import { ComponentFixture, TestBed } from '@angular/core/testing';

import { CandidateProfileComponent } from './candidate-profile-component';

describe('CandidateProfile', () => {
  let component: CandidateProfileComponent;
  let fixture: ComponentFixture<CandidateProfileComponent>;

  beforeEach(async () => {
    await TestBed.configureTestingModule({
      imports: [CandidateProfileComponent]
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
