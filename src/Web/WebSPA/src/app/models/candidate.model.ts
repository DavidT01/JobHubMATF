export interface CandidateExperience {
  startDate: string;
  endDate?: string;
}

export interface CandidateProfile {
  id: string;
  userId: string;
  firstName: string;
  lastName: string;
  location: string;
  skills: string[];
  experience: CandidateExperience[];
}
