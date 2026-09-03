using ApplicationService.Application.Authorization;
using ApplicationService.Application.Catalog;
using ApplicationService.Application.Commands;
using ApplicationService.Application.DTOs;
using ApplicationService.Application.Exceptions;
using ApplicationService.Application.Profiles;
using ApplicationService.Domain.Entities;
using ApplicationService.Persistence.Data;
using MediatR;
using Microsoft.EntityFrameworkCore;
using Npgsql;

namespace ApplicationService.Application.Handlers;

public sealed class SubmitApplicationHandler(
    ApplicationDbContext dbContext,
    ICurrentUser currentUser,
    ICandidateProfileReader profileReader,
    IJobReader jobReader,
    TimeProvider timeProvider) : IRequestHandler<SubmitApplicationCommand, ApplicationListItemDto>
{
    public async Task<ApplicationListItemDto> Handle(SubmitApplicationCommand request, CancellationToken cancellationToken)
    {
        var errors = new Dictionary<string, string[]>();
        if (string.IsNullOrWhiteSpace(request.JobId) || request.JobId.Length != JobApplication.CatalogJobIdLength
            || !request.JobId.All(Uri.IsHexDigit))
        {
            errors["jobId"] = ["Job identifier must be a 24-character hexadecimal Catalog identifier."];
        }

        if (request.CoverLetter?.Trim().Length > JobApplication.MaximumCoverLetterLength)
        {
            errors["coverLetter"] = [$"Cover letter cannot exceed {JobApplication.MaximumCoverLetterLength} characters."];
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
        if (string.IsNullOrWhiteSpace(profile.CvUrl))
        {
            throw new RequestValidationException(new Dictionary<string, string[]>
            {
                ["cv"] = ["Upload a CV to your candidate profile before applying."]
            });
        }

        var jobId = request.JobId!.ToLowerInvariant();
        var job = await jobReader.GetByIdAsync(jobId, cancellationToken)
            ?? throw new ResourceNotFoundException("Job not found.");
        var now = timeProvider.GetUtcNow();
        if (!job.IsActive || job.ExpirationDate <= now)
        {
            throw new ConflictException("This job is no longer accepting applications.");
        }

        if (await dbContext.JobApplications.AnyAsync(
            application => application.CandidateId == profile.Id && application.JobId == jobId, cancellationToken))
        {
            throw new ConflictException("You have already applied for this job.");
        }

        // Keep profile identity, not a CV URL snapshot: the current CV is resolved when read.
        var application = JobApplication.Create(profile.Id, userId, jobId, request.CoverLetter, now);
        dbContext.JobApplications.Add(application);
        try
        {
            await dbContext.SaveChangesAsync(cancellationToken);
        }
        catch (DbUpdateException exception) when (exception.InnerException is PostgresException
        {
            SqlState: PostgresErrorCodes.UniqueViolation,
            ConstraintName: "ux_job_applications_candidate_job"
        })
        {
            throw new ConflictException("You have already applied for this job.");
        }

        return new ApplicationListItemDto(application.Id, application.JobId, application.Status,
            application.SubmittedAtUtc, application.UpdatedAtUtc);
    }
}
