export interface EducationDto {
  institutionName: string;
  startDate: Date | string;
  endDate?: Date | string | null;
  degree?: string | null;
}
