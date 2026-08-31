export interface ScheduleInterviewCommand {
  selectionRoundId: string;
  candidateProfileId: string;
  startTime: Date;
  endTime: Date;
  title: string;
  description: string;
  attendeeEmails: string[];
}