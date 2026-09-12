export interface MatchResult {
  jobId: string;
  jobTitle: string;
  score: number;
  matchedSkills: string[];
  missingSkills: string[];
}
