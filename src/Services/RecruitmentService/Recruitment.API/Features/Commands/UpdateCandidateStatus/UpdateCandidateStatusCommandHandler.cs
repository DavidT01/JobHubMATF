using AutoMapper;
using MediatR;
using Microsoft.EntityFrameworkCore;
using Recruitment.API.Data;
using Recruitment.API.DTOs;
using Recruitment.API.Enums;
using Recruitment.API.Exceptions;
using Recruitment.API.Infrastructure;

namespace Recruitment.API.Features.Commands.UpdateCandidateStatus;

public class UpdateCandidateStatusCommandHandler(
    RecruitmentContext context,
    IMapper mapper,
    IProfileServiceClient profileServiceClient) : IRequestHandler<UpdateCandidateStatusCommand, CandidateProgressDto>
{
    public async Task<CandidateProgressDto> Handle(UpdateCandidateStatusCommand request, CancellationToken cancellationToken)
    {
        if (request.Status is not (CandidateProgressStatus.Rejected or CandidateProgressStatus.Hired))
        {
            throw new RecruitmentValidationException("Only Rejected or Hired can be set through this command.");
        }

        if (!await profileServiceClient.ValidateCandidateProfileAsync(request.CandidateProfileId, cancellationToken))
        {
            throw new RecruitmentValidationException($"Candidate profile {request.CandidateProfileId} not found");
        }

        var progress = await context.Progresses.FirstOrDefaultAsync(
            progress => progress.CandidateProfileId == request.CandidateProfileId
                && progress.RecruitmentProcessId == request.RecruitmentProcessId,
            cancellationToken)
            ?? throw new RecruitmentValidationException("Progress for the given candidate and process not found.");

        progress.Status = request.Status;
        progress.ModifiedAt = DateTime.UtcNow;
        await context.SaveChangesAsync(cancellationToken);

        return mapper.Map<CandidateProgressDto>(progress);
    }
}
