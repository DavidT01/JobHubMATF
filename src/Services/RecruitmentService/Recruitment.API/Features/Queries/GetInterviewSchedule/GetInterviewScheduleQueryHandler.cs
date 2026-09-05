using AutoMapper;
using MediatR;
using Microsoft.EntityFrameworkCore;
using Recruitment.API.Data;
using Recruitment.API.DTOs;

namespace Recruitment.API.Features.Queries.GetInterviewSchedule;

public class GetInterviewScheduleQueryHandler(RecruitmentContext context, IMapper mapper, ILogger<GetInterviewScheduleQueryHandler> logger)
    : IRequestHandler<GetInterviewScheduleQuery, InterviewScheduleDto?>
{
    public async Task<InterviewScheduleDto?> Handle(GetInterviewScheduleQuery request, CancellationToken cancellationToken)
    {
        var schedule = await context.InterviewSchedules.FirstOrDefaultAsync(
            interview => interview.CandidateProfileId == request.CandidateProfileId
                && interview.SelectionRoundId == request.SelectionRoundId,
            cancellationToken);

        if (schedule is null)
        {
            logger.LogWarning("Interview schedule for candidate {CandidateProfileId} in round {SelectionRoundId} was not found.", request.CandidateProfileId, request.SelectionRoundId);
            return null;
        }

        logger.LogInformation("Retrieved interview schedule {InterviewScheduleId} for candidate {CandidateProfileId}", schedule.Id, request.CandidateProfileId);
        return mapper.Map<InterviewScheduleDto>(schedule);
    }
}
