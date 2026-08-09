using Recruitment.API.Entities;

namespace Recruitment.API.DTOs
{
    public class RecruitmentProcessDto
    {
        public Guid Id { get; set; }
        public Guid CompanyId { get; set; }
        public Guid JobId { get; set; }
        public bool IsActive { get; set; }
        public List<SelectionRound> Rounds { get; set; } = [];
    }
}
