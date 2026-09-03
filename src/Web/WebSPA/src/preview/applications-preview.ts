import { ChangeDetectionStrategy, Component, signal } from '@angular/core';
import { MatButtonModule } from '@angular/material/button';
import { ApplicationFormComponent } from '../app/components/application-form/application-form-component';
import { CandidateApplicationsComponent } from '../app/components/candidate-applications/candidate-applications-component';
import { EmployerApplicationsComponent } from '../app/components/employer-applications/employer-applications-component';
import { PREVIEW_APPLY_JOB_ID, PREVIEW_JOB_ID } from './preview-applications-service';

@Component({
  selector: 'app-root',
  imports: [MatButtonModule, ApplicationFormComponent, CandidateApplicationsComponent, EmployerApplicationsComponent],
  changeDetection: ChangeDetectionStrategy.OnPush,
  styles: [`
    :host { display: block; }
    .preview-header { padding: 1.5rem; max-width: 76rem; margin: 0 auto; }
    nav, .job-picker { display: flex; flex-wrap: wrap; gap: .75rem; }
    .job-picker { margin-top: 1rem; }
    .narrow { max-width: 390px; margin: 0 auto; }
  `],
  template: `
    <header class="preview-header">
      <h1>Applications · local preview</h1>
      <p>Sample data only. No login, backend calls or real applications. Reloading resets the demo.</p>
      <nav aria-label="Preview screens">
        <button mat-stroked-button (click)="screen.set('employer')" [attr.aria-pressed]="screen() === 'employer'">Company view</button>
        <button mat-stroked-button (click)="screen.set('candidate')" [attr.aria-pressed]="screen() === 'candidate'">My applications</button>
        <button mat-stroked-button (click)="screen.set('apply')" [attr.aria-pressed]="screen() === 'apply'">Apply to a job</button>
        <button mat-stroked-button (click)="narrow.set(!narrow())" [attr.aria-pressed]="narrow()">Narrow layout</button>
      </nav>
      @if (screen() === 'employer') {
        <div class="job-picker" aria-label="Preview job selection">
          <button mat-button (click)="job.set(sampleJob)" [attr.aria-pressed]="job() === sampleJob">Job with 24 applications</button>
          <button mat-button (click)="job.set(applyJob)" [attr.aria-pressed]="job() === applyJob">New job / your demo application</button>
        </div>
      }
    </header>
    <main [class.narrow]="narrow()">
      @switch (screen()) {
        @case ('employer') { <app-employer-applications [jobId]="job()" /> }
        @case ('candidate') { <app-candidate-applications /> }
        @case ('apply') { <app-application-form [jobId]="applyJob" /> }
      }
    </main>
  `,
})
export class ApplicationsPreview {
  protected readonly sampleJob = PREVIEW_JOB_ID;
  protected readonly applyJob = PREVIEW_APPLY_JOB_ID;
  protected readonly job = signal(PREVIEW_JOB_ID);
  protected readonly screen = signal<'employer' | 'candidate' | 'apply'>('employer');
  protected readonly narrow = signal(false);
}
