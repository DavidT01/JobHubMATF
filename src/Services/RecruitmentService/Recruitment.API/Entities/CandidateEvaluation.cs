namespace Recruitment.API.Entities
{
    public class CandidateEvaluation
    {
        public Guid Id { get; set; } = Guid.NewGuid();
        public Guid CandidateProfileId { get; set; }
        public Guid SelectionRoundId { get; set; }
        public int Score { get; set; }
        public string? Notes { get; set; }
        public DateTime CreatedAt { get; set; } = DateTime.UtcNow;
        public DateTime? ModifiedAt { get; set; }

        public SelectionRound? SelectionRound { get; set; }
    }
}
