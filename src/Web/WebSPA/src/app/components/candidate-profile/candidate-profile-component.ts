import { Component, inject, OnInit, signal } from '@angular/core';
import { FormBuilder, FormGroup, Validators } from '@angular/forms';
import { ActivatedRoute, Router } from '@angular/router';
import { CandidateProfileService } from '../../core/services/candidate-profile/candidate-profile-service';
import { CandidateProfileDto } from '../../core/models/candidate-profile-dto';

@Component({
  selector: 'app-candidate-profile',
  imports: [],
  templateUrl: './candidate-profile.html',
  styleUrl: './candidate-profile.scss',
})
export class CandidateProfileComponent implements OnInit {
  private route = inject(ActivatedRoute);
  private router = inject(Router);
  private fb = inject(FormBuilder);
  private profileService = inject(CandidateProfileService);

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
      education: [''],
      experience: [''],
      projects: [''],
      skills: [''],
      languages: [''],
      githubUrl: [''],
      gitlabUrl: [''],
      linkedInUrl: ['']
    });
  }

  loadProfile(): void {
    this.profileService.getProfile(this.userId).subscribe({
      next: (data) => {
        this.profileData.set(data);
        this.form.patchValue(data);
        this.isLoading.set(false);
      },
      error: (err) => {
        console.error(err);
        if (err.status === 404) {
          this.router.navigate(['/']); // maybe create a not found component?
        }
      }
    });
  }

  toggleEditMode(): void {
    if (this.isEditMode() && this.profileData()?.id)
      this.loadProfile();
    this.isEditMode.update(v => !v);
  }

  saveProfile(): void {
    if (this.form.invalid || !this.profileData()?.id)
      return;

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

    this.profileService.deleteProfile(id).subscribe({
      next: () => {
        this.router.navigate(['/']);
      }
    });
  }
}
