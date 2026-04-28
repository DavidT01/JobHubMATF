import { EducationDto } from "./education-dto";
import { ExperienceDto } from "./experience-dto";
import { ProjectDto } from "./project-dto";
import { LanguageDto } from "./language-dto";

export interface CandidateProfileDto {
  id: string;
  userId: string;
  firstName: string;
  lastName: string;
  email: string;
  phoneNumber: string;
  location: string;
  education: EducationDto[];
  experience: ExperienceDto[];
  projects: ProjectDto[];
  skills: string[];
  languages: LanguageDto[];
  cvUrl: string;
  githubUrl?: string;
  gitlabUrl?: string;
  linkedInUrl?: string;
  pictureUrl?: string;
}
