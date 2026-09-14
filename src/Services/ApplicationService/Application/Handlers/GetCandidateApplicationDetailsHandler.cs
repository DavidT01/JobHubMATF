using ApplicationService.Application.Authorization;
using ApplicationService.Application.DTOs;
using ApplicationService.Application.Exceptions;
using ApplicationService.Application.Queries;
using ApplicationService.Persistence.Data;
using MediatR;
using Microsoft.EntityFrameworkCore;

namespace ApplicationService.Application.Handlers;

public sealed class GetCandidateApplicationDetailsHandler(
    ApplicationDbContext dbContext,
    ICurrentUser currentUser,
    ApplicationService.Application.Profiles.ICandidateProfileReader profileReader)
    : IRequestHandler<GetCandidateApplicationDetailsQuery, CandidateApplicationDetailsDto>
{
    public async Task<CandidateApplicationDetailsDto> Handle(GetCandidateApplicationDetailsQuery request, CancellationToken cancellationToken)
    {
        var userId = currentUser.UserId;
        if (!currentUser.IsAuthenticated || string.IsNullOrWhiteSpace(userId))
        {
            throw new ForbiddenException();
        }

        var profile = await profileReader.GetByUserIdAsync(userId, cancellationToken)
            ?? throw new ResourceNotFoundException("Candidate profile not found.");

        var application = await dbContext.JobApplications.AsNoTracking()
            .Where(candidateApplication => candidateApplication.Id == request.ApplicationId
                && candidateApplication.CandidateId == profile.Id)
            .Select(candidateApplication => new CandidateApplicationDetailsDto(
                candidateApplication.Id,
                candidateApplication.CandidateId,
                candidateApplication.JobId,
                candidateApplication.Status,
                candidateApplication.SubmittedAtUtc,
                candidateApplication.UpdatedAtUtc))
            .SingleOrDefaultAsync(cancellationToken);

        return application ?? throw new ResourceNotFoundException("Application not found.");
    }
}
