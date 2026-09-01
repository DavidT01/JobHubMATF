import { ComponentFixture, TestBed } from '@angular/core/testing';
import { CandidateEvaluationDialogComponent } from './candidate-evaluation-dialog.component';

describe('CandidateEvaluationDialogComponent', () => {
  let component: CandidateEvaluationDialogComponent;
  let fixture: ComponentFixture<CandidateEvaluationDialogComponent>;

  beforeEach(async () => {
    await TestBed.configureTestingModule({
      imports: [CandidateEvaluationDialogComponent]
    })
    .compileComponents();

    fixture = TestBed.createComponent(CandidateEvaluationDialogComponent);
    component = fixture.componentInstance;
    await fixture.whenStable();
  });

  it('should create', () => {
    expect(component).toBeTruthy();
  });
});