using Recruitment.API.Enums;

namespace Recruitment.API.DTOs
{
    public class CandidateProgressDto
    {
        public Guid Id { get; set; }
        public Guid CandidateProfileId { get; set; }
        public Guid RecruitmentProcessId { get; set; }
        public Guid? CurrentSelectionRoundId { get; set; }
        public CandidateProgressStatus Status { get; set; }
    }
}
