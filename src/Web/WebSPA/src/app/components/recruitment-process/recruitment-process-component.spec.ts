import { ComponentFixture, TestBed } from '@angular/core/testing';
import { provideHttpClient } from '@angular/common/http';
import { provideHttpClientTesting } from '@angular/common/http/testing';
import { provideRouter } from '@angular/router';

import { RecruitmentProcessComponent } from './recruitment-process-component';

describe('RecruitmentProcessComponent', () => {
  let component: RecruitmentProcessComponent;
  let fixture: ComponentFixture<RecruitmentProcessComponent>;

  beforeEach(async () => {
    await TestBed.configureTestingModule({
      imports: [RecruitmentProcessComponent],
      providers: [provideRouter([]), provideHttpClient(), provideHttpClientTesting()]
    })
    .compileComponents();

    fixture = TestBed.createComponent(RecruitmentProcessComponent);
    component = fixture.componentInstance;
    await fixture.whenStable();
  });

  it('should create', () => {
    expect(component).toBeTruthy();
  });
});
