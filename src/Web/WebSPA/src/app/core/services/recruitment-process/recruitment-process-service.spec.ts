import { TestBed } from '@angular/core/testing';

import { RecruitmentProcessService } from './recruitment-process-service';

describe('RecruitmentProcessService', () => {
  let service: RecruitmentProcessService;

  beforeEach(() => {
    TestBed.configureTestingModule({});
    service = TestBed.inject(RecruitmentProcessService);
  });

  it('should be created', () => {
    expect(service).toBeTruthy();
  });
});
