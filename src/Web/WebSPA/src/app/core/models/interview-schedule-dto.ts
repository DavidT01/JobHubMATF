export interface InterviewScheduleDto {
  id: string;
  selectionRoundId: string;
  candidateProfileId: string;
  startTime: Date;
  endTime: Date;
  googleMeetUrl?: string;
}