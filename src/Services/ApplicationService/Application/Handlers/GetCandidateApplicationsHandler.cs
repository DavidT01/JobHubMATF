using ApplicationService.Application.Authorization;
using ApplicationService.Application.DTOs;
using ApplicationService.Application.Exceptions;
using ApplicationService.Application.Profiles;
using ApplicationService.Application.Queries;
using ApplicationService.Persistence.Data;
using MediatR;
using Microsoft.EntityFrameworkCore;

namespace ApplicationService.Application.Handlers;

public sealed class GetCandidateApplicationsHandler(
    ApplicationDbContext dbContext,
    ICurrentUser currentUser,
    ICandidateProfileReader profileReader)
    : IRequestHandler<GetCandidateApplicationsQuery, PagedResult<ApplicationListItemDto>>
{
    public async Task<PagedResult<ApplicationListItemDto>> Handle(
        GetCandidateApplicationsQuery request, CancellationToken cancellationToken)
    {
        var errors = new Dictionary<string, string[]>();
        if (request.PageNumber < 1)
        {
            errors["pageNumber"] = ["Page number must be at least 1."];
        }

        if (request.PageSize is < 1 or > 100)
        {
            errors["pageSize"] = ["Page size must be between 1 and 100."];
        }

        var offset = ((long)request.PageNumber - 1) * request.PageSize;
        if (offset > int.MaxValue)
        {
            errors["pageNumber"] = ["Requested page is too large."];
        }

        if (errors.Count > 0)
        {
            throw new RequestValidationException(errors);
        }

        var userId = currentUser.UserId;
        if (!currentUser.IsAuthenticated || string.IsNullOrWhiteSpace(userId))
        {
            throw new ForbiddenException();
        }

        var profile = await profileReader.GetByUserIdAsync(userId, cancellationToken)
            ?? throw new ResourceNotFoundException("Candidate profile not found.");

        var applications = dbContext.JobApplications.AsNoTracking()
            .Where(application => application.CandidateId == profile.Id);
        var totalCount = await applications.CountAsync(cancellationToken);
        var items = await applications
            .OrderByDescending(application => application.SubmittedAtUtc)
            .ThenBy(application => application.Id)
            .Skip((int)offset)
            .Take(request.PageSize)
            .Select(application => new ApplicationListItemDto(
                application.Id, application.JobId, application.Status,
                application.SubmittedAtUtc, application.UpdatedAtUtc))
            .ToListAsync(cancellationToken);

        return new PagedResult<ApplicationListItemDto>(items, totalCount, request.PageNumber, request.PageSize);
    }
}
