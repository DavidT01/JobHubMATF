namespace Recruitment.API.DTOs;

public sealed class CandidateApplicationProgressDto
{
    public CandidateProgressDto Progress { get; set; } = null!;
    public RecruitmentProcessDto Process { get; set; } = null!;
    public List<InterviewScheduleDto> Interviews { get; set; } = [];
}
