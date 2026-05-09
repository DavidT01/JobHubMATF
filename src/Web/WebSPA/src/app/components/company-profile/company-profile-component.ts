import { Component, inject, OnInit, signal } from '@angular/core';
import { FormBuilder, FormGroup, ReactiveFormsModule, Validators } from '@angular/forms';
import { ActivatedRoute, Router } from '@angular/router';

import { MatButtonModule } from '@angular/material/button';
import { MatCardModule } from '@angular/material/card';
import { MatFormFieldModule } from '@angular/material/form-field';
import { MatInputModule } from '@angular/material/input';
import { MatDialog, MatDialogModule } from '@angular/material/dialog';

import { CompanyProfileDto } from '../../core/models/company-profile-dto';
import { CompanyProfileService } from '../../core/services/company-profile/company-profile-service';
import { ConfirmDialogComponent } from '../confirm-dialog/confirm-dialog-component';

@Component({
  selector: 'app-company-profile-component',
  imports: [
    ReactiveFormsModule,
    MatButtonModule,
    MatCardModule,
    MatFormFieldModule,
    MatInputModule,
    MatDialogModule
  ],
  templateUrl: './company-profile-component.html',
  styleUrl: './company-profile-component.scss',
})
export class CompanyProfileComponent implements OnInit {
  private route = inject(ActivatedRoute);
  private router = inject(Router);
  private fb = inject(FormBuilder);
  private profileService = inject(CompanyProfileService);
  private dialog = inject(MatDialog);

  profileData = signal<CompanyProfileDto | null>(null);
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
      companyName: ['', Validators.required],
      description: ['', Validators.required],
      location: ['', Validators.required],
      contactEmail: ['', [Validators.required, Validators.email]],
      contactPhone: [''],
      websiteUrl: [''],
      linkedInUrl: ['']
    })
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
        this.isLoading.set(false);
        if (err.status === 404) {
          this.router.navigate(['/']);
        }
      }
    });
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
    })
  }

  deleteProfile(): void {
    const id = this.profileData()?.id;
    if (!id)
      return;

    const dialogRef = this.dialog.open(ConfirmDialogComponent, {
      width: '400px',
      data: { message: 'Are you sure you want to delete your company profile?\nThis action is permanent.' }
    });

    dialogRef.afterClosed().subscribe(result => {
      if (result === true) {
        this.profileService.deleteProfile(id).subscribe({
          next: () => {
            this.router.navigate(['/']);
          }
        });
      }
    });
  }
}
