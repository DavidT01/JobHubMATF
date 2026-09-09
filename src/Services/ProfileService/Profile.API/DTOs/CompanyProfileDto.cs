namespace Profile.API.DTOs
{
    public class CompanyProfileDto
    {
        public Guid Id { get; set; }
        public string UserId { get; set; } = string.Empty;
        public string CompanyName { get; set; } = string.Empty;
        public string Description { get; set; } = string.Empty;
        public string Location { get; set; } = string.Empty;
        public string ContactEmail { get; set; } = string.Empty;
        public string ContactPhone { get; set; } = string.Empty;
        public string? WebsiteUrl { get; set; }
        public string? LinkedInUrl { get; set; }
        public string? LogoUrl { get; set; }
    }
}
