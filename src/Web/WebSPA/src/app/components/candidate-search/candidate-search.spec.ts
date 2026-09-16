import { ComponentFixture, TestBed } from '@angular/core/testing';

import { CandidateSearch } from './candidate-search';

describe('CandidateSearch', () => {
  let component: CandidateSearch;
  let fixture: ComponentFixture<CandidateSearch>;

  beforeEach(async () => {
    await TestBed.configureTestingModule({
      imports: [CandidateSearch]
    })
    .compileComponents();

    fixture = TestBed.createComponent(CandidateSearch);
    component = fixture.componentInstance;
    await fixture.whenStable();
  });

  it('should create', () => {
    expect(component).toBeTruthy();
  });
});
