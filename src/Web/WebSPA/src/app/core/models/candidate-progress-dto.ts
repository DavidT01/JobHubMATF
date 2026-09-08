export interface CandidateProgressDto {
    id: string;
    candidateProfileId: string;
    recruitmentProcessId: string;
    currentSelectionRoundId?: string;
    status: string;
}
