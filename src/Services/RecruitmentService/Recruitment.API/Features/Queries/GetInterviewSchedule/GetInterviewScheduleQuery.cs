using MediatR;
using Recruitment.API.DTOs;

namespace Recruitment.API.Features.Queries.GetInterviewSchedule;

public record GetInterviewScheduleQuery(Guid CandidateProfileId, Guid SelectionRoundId) : IRequest<InterviewScheduleDto?>;
