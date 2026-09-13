import { ComponentFixture, TestBed } from '@angular/core/testing';
import { provideHttpClient } from '@angular/common/http';
import { provideHttpClientTesting } from '@angular/common/http/testing';
import { MAT_DIALOG_DATA, MatDialogRef } from '@angular/material/dialog';
import { CandidateEvaluationDialogComponent } from './candidate-evaluation-dialog.component';

describe('CandidateEvaluationDialogComponent', () => {
  let component: CandidateEvaluationDialogComponent;
  let fixture: ComponentFixture<CandidateEvaluationDialogComponent>;

  beforeEach(async () => {
    await TestBed.configureTestingModule({
      imports: [CandidateEvaluationDialogComponent],
      providers: [provideHttpClient(), provideHttpClientTesting(), { provide: MatDialogRef, useValue: { close: () => {} } }, { provide: MAT_DIALOG_DATA, useValue: { selectionRoundId: 'round-1', candidateProfileId: 'candidate-1', existingEvaluation: null } }]
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