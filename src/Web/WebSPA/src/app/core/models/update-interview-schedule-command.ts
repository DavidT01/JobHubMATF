export interface UpdateInterviewScheduleCommand {
  interviewScheduleId: string;
  startTime: Date;
  endTime: Date;
  title: string;
  description: string;
  additionalAttendeeEmails: string[];
}
