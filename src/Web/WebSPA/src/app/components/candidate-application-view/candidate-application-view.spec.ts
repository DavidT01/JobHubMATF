import { ComponentFixture, TestBed } from '@angular/core/testing';

import { CandidateApplicationViewComponent } from './candidate-application-view';

describe('CandidateApplicationViewComponent', () => {
  let component: CandidateApplicationViewComponent;
  let fixture: ComponentFixture<CandidateApplicationViewComponent>;

  beforeEach(async () => {
    await TestBed.configureTestingModule({
      imports: [CandidateApplicationViewComponent]
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
