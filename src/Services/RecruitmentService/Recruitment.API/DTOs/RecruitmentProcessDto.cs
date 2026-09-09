namespace Recruitment.API.DTOs
{
    public class RecruitmentProcessDto
    {
        public Guid Id { get; set; }
        public Guid CompanyId { get; set; }
        public string JobId { get; set; } = string.Empty;
        public bool IsActive { get; set; }
        public List<SelectionRoundDto> Rounds { get; set; } = [];
    }
}
