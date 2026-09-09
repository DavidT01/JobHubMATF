export interface ExperienceDto {
  companyName: string;
  position: string;
  startDate: Date | string;
  endDate?: Date | string | null;
}
