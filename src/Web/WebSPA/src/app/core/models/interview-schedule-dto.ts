export interface InterviewScheduleDto {
  id: string;
  selectionRoundId: string;
  candidateProfileId: string;
  title: string;
  description: string;
  additionalAttendeeEmails: string[];
  startTime: Date;
  endTime: Date;
  googleMeetUrl?: string;
}