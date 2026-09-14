using AutoMapper;
using MediatR;
using Microsoft.EntityFrameworkCore;
using Recruitment.API.Data;
using Recruitment.API.DTOs;
using Recruitment.API.Exceptions;

namespace Recruitment.API.Features.Queries.GetCandidateApplicationProgress;

public sealed class GetCandidateApplicationProgressQueryHandler(
    RecruitmentContext context,
    IMapper mapper,
    ILogger<GetCandidateApplicationProgressQueryHandler> logger)
    : IRequestHandler<GetCandidateApplicationProgressQuery, CandidateApplicationProgressDto>
{
    public async Task<CandidateApplicationProgressDto> Handle(GetCandidateApplicationProgressQuery request, CancellationToken cancellationToken)
    {
        var progress = await context.Progresses
            .AsNoTracking()
            .Include(candidateProgress => candidateProgress.RecruitmentProcess!)
            .ThenInclude(process => process.Rounds)
            .SingleOrDefaultAsync(
                candidateProgress => candidateProgress.ApplicationId == request.ApplicationId,
                cancellationToken);

        if (progress?.RecruitmentProcess is null)
        {
            logger.LogWarning(
                "Progress for application {ApplicationId} was not found.", request.ApplicationId);
            throw new RecruitmentValidationException("Recruitment progress for the application was not found.");
        }

        var roundIds = progress.RecruitmentProcess.Rounds
            .Select(round => round.Id)
            .ToArray();
        var interviews = await context.InterviewSchedules
            .AsNoTracking()
            .Where(interview => interview.CandidateProfileId == progress.CandidateProfileId
                && roundIds.Contains(interview.SelectionRoundId))
            .OrderBy(interview => interview.StartTime)
            .ToListAsync(cancellationToken);

        return new CandidateApplicationProgressDto
        {
            Progress = mapper.Map<CandidateProgressDto>(progress),
            Process = mapper.Map<RecruitmentProcessDto>(progress.RecruitmentProcess),
            Interviews = mapper.Map<List<InterviewScheduleDto>>(interviews)
        };
    }
}
