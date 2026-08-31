export interface EvaluateCandidateCommand {
    candidateProfileId: string;
    selectionRoundId: string;
    score: number;
    notes?: string;
}
