import { Pipe, PipeTransform } from '@angular/core';
import { ExperienceLevel, JobType, WorkMode } from '../models/job.model';

export type JobLabelKind = 'jobType' | 'experienceLevel' | 'workMode';

const LABELS: {
  jobType: Record<JobType, string>;
  experienceLevel: Record<ExperienceLevel, string>;
  workMode: Record<WorkMode, string>;
} = {
  jobType: { FullTime: 'Full-time', PartTime: 'Part-time', Contract: 'Contract', Internship: 'Internship' },
  experienceLevel: { Junior: 'Junior', Mid: 'Mid-level', Senior: 'Senior', Lead: 'Lead' },
  workMode: { OnSite: 'On-site', Hybrid: 'Hybrid', Remote: 'Remote' },
};

/** Turns Catalog enum values (e.g. "FullTime") into readable labels (e.g. "Full-time"). */
@Pipe({ name: 'jobLabel' })
export class JobLabelPipe implements PipeTransform {
  transform(value: string | null | undefined, kind: JobLabelKind): string {
    if (!value) return '';
    return (LABELS[kind] as Record<string, string>)[value] ?? value;
  }
}
