export interface CompanyProfileDto {
  id: string,
  userId: string,
  companyName: string,
  description: string,
  location: string,
  contactEmail: string,
  contactPhone: string,
  websiteUrl?: string | null;
  linkedInUrl?: string | null;
  logoUrl?: string | null;
}
