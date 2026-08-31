export interface CandidateEvaluationDto {
    id: string;
    candidateProfileId: string;
    selectionRoundId: string;
    score: number;
    notes?: string;
    createdAt: string;
}
