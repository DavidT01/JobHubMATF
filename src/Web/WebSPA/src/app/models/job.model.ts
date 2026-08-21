export type JobType = 'FullTime' | 'PartTime' | 'Contract' | 'Internship';

export type ExperienceLevel = 'Junior' | 'Mid' | 'Senior' | 'Lead';

export type WorkMode = 'OnSite' | 'Hybrid' | 'Remote';

export interface Job {
    id: string;
    jobType: JobType;
    experienceLevel: ExperienceLevel;
    workMode: WorkMode;
    title: string;
    description: string;
    companyId: string;
    companyName: string;
    applyUrl?: string;
    contactEmail?: string;
    postedDate: string;    
    salaryMin?: number;
    salaryMax?: number;
    currency: string;
    skills: string[];
    requirements: string[];
    responsibilities: string[];
    yearsOfExperience?: number;
    educationLevel?: string;
    city?: string;
    country?: string;
    isActive: boolean;
    expirationDate?: string;
}