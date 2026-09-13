import { ComponentFixture, TestBed } from '@angular/core/testing';
import { provideHttpClient } from '@angular/common/http';
import { provideHttpClientTesting } from '@angular/common/http/testing';
import { provideRouter } from '@angular/router';

import { CompanyProfileComponent } from './company-profile-component';

describe('CompanyProfileComponent', () => {
  let component: CompanyProfileComponent;
  let fixture: ComponentFixture<CompanyProfileComponent>;

  beforeEach(async () => {
    await TestBed.configureTestingModule({
      imports: [CompanyProfileComponent],
      providers: [provideRouter([]), provideHttpClient(), provideHttpClientTesting()]
    })
    .compileComponents();

    fixture = TestBed.createComponent(CompanyProfileComponent);
    component = fixture.componentInstance;
    await fixture.whenStable();
  });

  it('should create', () => {
    expect(component).toBeTruthy();
  });
});
