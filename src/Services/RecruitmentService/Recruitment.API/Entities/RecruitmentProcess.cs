namespace Recruitment.API.Entities
{
    public class RecruitmentProcess
    {
        public Guid Id { get; set; } = Guid.NewGuid();
        public Guid CompanyId { get; set; }
        public Guid JobId { get; set; }
        public bool Active { get; set; } = false;
        public List<SelectionRound> Rounds { get; set; } = new();
        public DateTime CreatedAt { get; set; } = DateTime.UtcNow;
        public DateTime? ModifiedAt { get; set; }
    }
}
