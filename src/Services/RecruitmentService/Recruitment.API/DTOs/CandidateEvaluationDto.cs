namespace Recruitment.API.DTOs
{
    public class CandidateEvaluationDto
    {
        public Guid Id { get; set; }
        public Guid CandidateProfileId { get; set; }
        public Guid SelectionRoundId { get; set; }
        public int Score { get; set; }
        public string? Notes { get; set; }
        public DateTime CreatedAt { get; set; }
    }
}
