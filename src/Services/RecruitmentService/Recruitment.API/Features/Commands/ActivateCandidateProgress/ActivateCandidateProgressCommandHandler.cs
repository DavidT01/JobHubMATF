using AutoMapper;
using MediatR;
using Microsoft.EntityFrameworkCore;
using Recruitment.API.Data;
using Recruitment.API.DTOs;
using Recruitment.API.Enums;
using Recruitment.API.Exceptions;
using Recruitment.API.Infrastructure;

namespace Recruitment.API.Features.Commands.ActivateCandidateProgress;

public sealed class ActivateCandidateProgressCommandHandler(
    RecruitmentContext context,
    IMapper mapper,
    IProfileServiceClient profileServiceClient,
    ILogger<ActivateCandidateProgressCommandHandler> logger)
    : IRequestHandler<ActivateCandidateProgressCommand, CandidateProgressDto>
{
    public async Task<CandidateProgressDto> Handle(
        ActivateCandidateProgressCommand request,
        CancellationToken cancellationToken)
    {
        var jobId = request.JobId.ToLowerInvariant();
        var process = await context.Processes
            .Include(item => item.Rounds)
            .SingleOrDefaultAsync(item => item.JobId == jobId, cancellationToken);
        if (process is null)
        {
            throw new RecruitmentValidationException($"Recruitment process for job {jobId} was not found.");
        }

        var existingProgress = await context.Progresses
            .SingleOrDefaultAsync(item => item.CandidateProfileId == request.CandidateProfileId
                && item.RecruitmentProcessId == process.Id, cancellationToken);
        if (existingProgress is not null)
        {
            logger.LogInformation(
                "Candidate {CandidateProfileId} is already active in recruitment process {RecruitmentProcessId}.",
                request.CandidateProfileId, process.Id);
            return mapper.Map<CandidateProgressDto>(existingProgress);
        }

        if (!await profileServiceClient.ValidateCandidateProfileAsync(
            request.CandidateProfileId, cancellationToken))
        {
            throw new RecruitmentValidationException($"Candidate profile {request.CandidateProfileId} was not found.");
        }

        var progress = new Entities.CandidateProgress
        {
            CandidateProfileId = request.CandidateProfileId,
            RecruitmentProcessId = process.Id,
            CurrentSelectionRoundId = process.Rounds.OrderBy(round => round.Index).FirstOrDefault()?.Id,
            Status = CandidateProgressStatus.InProgress
        };

        context.Progresses.Add(progress);
        await context.SaveChangesAsync(cancellationToken);

        logger.LogInformation(
            "Activated candidate {CandidateProfileId} from application {ApplicationId} in recruitment process {RecruitmentProcessId}.",
            request.CandidateProfileId, request.ApplicationId, process.Id);
        return mapper.Map<CandidateProgressDto>(progress);
    }
}
