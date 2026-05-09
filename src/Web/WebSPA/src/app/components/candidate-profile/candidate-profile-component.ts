import { Component, inject, OnInit, signal } from '@angular/core';
import { FormArray, FormBuilder, FormGroup, ReactiveFormsModule, Validators } from '@angular/forms';
import { ActivatedRoute, Router } from '@angular/router';
import { DatePipe } from '@angular/common';

import { MatButtonModule } from '@angular/material/button';
import { MatCardModule } from '@angular/material/card';
import { MatFormFieldModule } from '@angular/material/form-field';
import { MatInputModule } from '@angular/material/input';
import { MatChipsModule } from '@angular/material/chips';
import { MatIconModule } from '@angular/material/icon';
import { MatDialog, MatDialogModule } from '@angular/material/dialog';
import { MatDatepickerModule } from '@angular/material/datepicker';
import { MatNativeDateModule } from '@angular/material/core';

import { CandidateProfileService } from '../../core/services/candidate-profile/candidate-profile-service';
import { CandidateProfileDto } from '../../core/models/candidate-profile-dto';
import { ConfirmDialogComponent } from '../../components/confirm-dialog/confirm-dialog-component';

@Component({
  selector: 'app-candidate-profile-component',
  standalone: true,
  imports: [
    ReactiveFormsModule, 
    DatePipe,
    MatButtonModule,
    MatCardModule,
    MatFormFieldModule,
    MatInputModule,
    MatChipsModule,
    MatIconModule,
    MatDialogModule,
    MatDatepickerModule,
    MatNativeDateModule
  ],
  templateUrl: './candidate-profile-component.html',
  styleUrl: './candidate-profile-component.scss',
})
export class CandidateProfileComponent implements OnInit {
  private route = inject(ActivatedRoute);
  private router = inject(Router);
  private fb = inject(FormBuilder);
  private profileService = inject(CandidateProfileService);
  private dialog = inject(MatDialog);

  profileData = signal<CandidateProfileDto | null>(null);
  isLoading = signal<boolean>(true);
  isEditMode = signal<boolean>(false);

  userId: string = '';
  form!: FormGroup;

  ngOnInit(): void {
    this.userId = this.route.snapshot.paramMap.get('userId') || '';
    this.initForm();

    if (this.userId)
      this.loadProfile();
    else
      this.router.navigate(['/']);
  }

  initForm(): void {
    this.form = this.fb.group({
      firstName: ['', Validators.required],
      lastName: ['', Validators.required],
      email: ['', [Validators.required, Validators.email]],
      phoneNumber: [''],
      location: [''],
      education: this.fb.array([]),
      experience: this.fb.array([]),
      projects: this.fb.array([]),
      skills: this.fb.array([]),
      languages: this.fb.array([]),
      cvUrl: [''],
      githubUrl: [''],
      gitlabUrl: [''],
      linkedInUrl: ['']
    });
  }

  loadProfile(): void {
    this.profileService.getProfile(this.userId).subscribe({
      next: (data) => {
        this.profileData.set(data);
        this.patchFormArrays(data);
        this.form.patchValue(data);
        this.isLoading.set(false);
      },
      error: (err) => {
        console.error(err);
        this.isLoading.set(false);
        if (err.status === 404) {
          this.router.navigate(['/']);
        }
      }
    });
  }

  private patchFormArrays(data: CandidateProfileDto): void {
    this.educationForms.clear();
    this.experienceForms.clear();
    this.projectsForms.clear();
    this.skillsForms.clear();
    this.languagesForms.clear();
    
    data.education?.forEach(() => this.addEducation());
    data.experience?.forEach(() => this.addExperience());
    data.projects?.forEach(() => this.addProject());
    data.skills?.forEach(() => this.addSkill());
    data.languages?.forEach(() => this.addLanguage());

    const formattedData = { ...data };
    if (formattedData.education) {
      formattedData.education = formattedData.education.map(e => ({
        ...e, 
        startDate: e.startDate ? new Date(e.startDate).toISOString().split('T')[0] as string : '',
        endDate: e.endDate ? new Date(e.endDate).toISOString().split('T')[0] as string : null
      })) as any;
    }
    
    if (formattedData.experience) {
      formattedData.experience = formattedData.experience.map(e => ({
        ...e, 
        startDate: e.startDate ? new Date(e.startDate).toISOString().split('T')[0] as string : '',
        endDate: e.endDate ? new Date(e.endDate).toISOString().split('T')[0] as string : null
      })) as any;
    }

    this.form.patchValue(formattedData);
  }

  get educationForms() { return this.form.get('education') as FormArray; }
  get experienceForms() { return this.form.get('experience') as FormArray; }
  get projectsForms() { return this.form.get('projects') as FormArray; }
  get skillsForms() { return this.form.get('skills') as FormArray; }
  get languagesForms() { return this.form.get('languages') as FormArray; }

  addEducation() {
    this.educationForms.push(this.fb.group({
      institutionName: ['', Validators.required],
      startDate: ['', Validators.required],
      endDate: [null],
      degree: [null]
    }));
  }
  removeEducation(i: number) { this.educationForms.removeAt(i); }

  addExperience() {
    this.experienceForms.push(this.fb.group({
      companyName: ['', Validators.required],
      position: ['', Validators.required],
      startDate: ['', Validators.required],
      endDate: [null]
    }));
  }
  removeExperience(i: number) { this.experienceForms.removeAt(i); }

  addProject() {
    this.projectsForms.push(this.fb.group({
      name: ['', Validators.required],
      description: [null],
      repositoryUrl: [null]
    }));
  }
  removeProject(i: number) { this.projectsForms.removeAt(i); }

  addSkill() { this.skillsForms.push(this.fb.control('', Validators.required)); }
  removeSkill(i: number) { this.skillsForms.removeAt(i); }

  addLanguage() {
    this.languagesForms.push(this.fb.group({
      name: ['', Validators.required],
      level: [null]
    }));
  }
  removeLanguage(i: number) { this.languagesForms.removeAt(i); }

  calculateDuration(startDate: string | Date | undefined, endDate: string | Date | null | undefined): string {
    if (!startDate)
      return '';

    const start = new Date(startDate);
    const end = endDate ? new Date(endDate) : new Date();

    let months = (end.getFullYear() - start.getFullYear()) * 12 + (end.getMonth() - start.getMonth());
    const years = Math.floor(months / 12);
    const monthsRemainder = months % 12;

    return [years > 0 ? `${years} yr` : '',
      monthsRemainder > 0 ? `${monthsRemainder} mo` : ''
    ].join(' ').trim();
  }

  toggleEditMode(): void {
    if (this.isEditMode() && this.profileData()?.id) {
      this.loadProfile();
    }
    this.isEditMode.update(v => !v);
  }

  saveProfile(): void {
    if (this.form.invalid || !this.profileData()?.id) {
      this.form.markAllAsTouched();
      return;
    }

    const data = { ...this.profileData()!, ...this.form.value, userId: this.userId };
    this.profileService.updateProfile(this.profileData()!.id, data).subscribe({
      next: () => {
        this.profileData.set(data);
        this.isEditMode.set(false);
      }
    });
  }

  deleteProfile(): void {
    const id = this.profileData()?.id;
    if (!id)
      return;

    const dialogRef = this.dialog.open(ConfirmDialogComponent, {
      width: '400px',
      data: { message: 'Are you sure you want to delete your candidate profile? This action is permanent.' }
    });

    dialogRef.afterClosed().subscribe(result => {
      if (result === true) {
        this.profileService.deleteProfile(id).subscribe({
          next: () => {
            this.router.navigate(['/']);
          }
        });
      }
    })
  }
}
