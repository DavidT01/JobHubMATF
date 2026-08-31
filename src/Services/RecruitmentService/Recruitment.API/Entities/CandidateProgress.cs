using Recruitment.API.Enums;

namespace Recruitment.API.Entities
{
    public class CandidateProgress
    {
        public Guid Id { get; set; } = Guid.NewGuid();
        public Guid CandidateProfileId { get; set; }
        public Guid RecruitmentProcessId { get; set; }
        public Guid? CurrentSelectionRoundId { get; set; }
        public CandidateProgressStatus Status { get; set; } = CandidateProgressStatus.InProgress;
        public DateTime CreatedAt { get; set; } = DateTime.UtcNow;
        public DateTime? ModifiedAt { get; set; }

        public RecruitmentProcess? RecruitmentProcess { get; set; }
        public SelectionRound? CurrentSelectionRound { get; set; }
    }
}
