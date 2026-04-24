export interface CandidateProfileDto {
  id: string;
  userId: string;
  firstName: string;
  lastName: string;
  email: string;
  phoneNumber: string;
  location: string;
  education: string;
  experience: string;
  projects: string;
  skills: string;
  languages: string;
  cvUrl: string;
  githubUrl?: string;
  gitlabUrl?: string;
  linkedInUrl?: string;
  pictureUrl?: string;
}
