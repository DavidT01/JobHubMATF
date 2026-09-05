using AutoMapper;
using MediatR;
using Microsoft.EntityFrameworkCore;
using Recruitment.API.Data;
using Recruitment.API.DTOs;

namespace Recruitment.API.Features.Queries.GetInterviewSchedule;

public class GetInterviewScheduleQueryHandler(RecruitmentContext context, IMapper mapper)
    : IRequestHandler<GetInterviewScheduleQuery, InterviewScheduleDto?>
{
    public async Task<InterviewScheduleDto?> Handle(GetInterviewScheduleQuery request, CancellationToken cancellationToken)
    {
        var schedule = await context.InterviewSchedules.FirstOrDefaultAsync(
            interview => interview.CandidateProfileId == request.CandidateProfileId
                && interview.SelectionRoundId == request.SelectionRoundId,
            cancellationToken);

        return schedule is null ? null : mapper.Map<InterviewScheduleDto>(schedule);
    }
}
