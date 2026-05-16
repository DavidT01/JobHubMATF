namespace Recruitment.API.Entities
{
    public class SelectionRound
    {
        public Guid Id { get; set; } = Guid.NewGuid();
        public Guid RecruitmentProcessId { get; set; }
        public string Title { get; set; } = string.Empty;
        public string? Description { get; set; }
        public int Index { get; set; }
        public DateTime CreatedAt { get; set; } = DateTime.UtcNow;
        public DateTime? ModifiedAt { get; set; }

        public RecruitmentProcess? RecruitmentProcess { get; set; }
    }
}
