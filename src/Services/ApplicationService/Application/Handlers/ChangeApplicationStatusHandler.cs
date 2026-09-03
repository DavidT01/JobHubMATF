using ApplicationService.Application.Authorization;
using ApplicationService.Application.Commands;
using ApplicationService.Application.Exceptions;
using ApplicationService.Persistence.Data;
using MediatR;
using Microsoft.EntityFrameworkCore;

namespace ApplicationService.Application.Handlers;

public sealed class ChangeApplicationStatusHandler(
    ApplicationDbContext dbContext, JobOwnershipGuard ownership, TimeProvider timeProvider)
    : IRequestHandler<ChangeApplicationStatusCommand, Unit>
{
    public async Task<Unit> Handle(ChangeApplicationStatusCommand request, CancellationToken cancellationToken)
    {
        var errors = new Dictionary<string, string[]>();
        if (request.ApplicationId == Guid.Empty)
        {
            errors["applicationId"] = ["Application identifier is required."];
        }
        if (request.Status is null || !Enum.IsDefined(request.Status.Value))
        {
            errors["status"] = ["A valid application status is required."];
        }
        if (errors.Count > 0) throw new RequestValidationException(errors);

        var application = await dbContext.JobApplications.AsNoTracking()
            .SingleOrDefaultAsync(item => item.Id == request.ApplicationId, cancellationToken)
            ?? throw new ResourceNotFoundException("Application not found.");

        await ownership.EnsureOwnedAsync(application.JobId, cancellationToken);
        var previousStatus = application.Status;
        var previousUpdatedAt = application.UpdatedAtUtc;
        application.ChangeStatus(request.Status!.Value, timeProvider.GetUtcNow());

        // Compare and set in one SQL statement: concurrent requests cannot overwrite a newer status.
        // Setting the same status is idempotent and preserves the original update timestamp.
        var affectedRows = await dbContext.JobApplications
            .Where(item => item.Id == application.Id
                && item.Status == previousStatus && item.UpdatedAtUtc == previousUpdatedAt)
            .ExecuteUpdateAsync(setters => setters
                .SetProperty(item => item.Status, application.Status)
                .SetProperty(item => item.UpdatedAtUtc, application.UpdatedAtUtc), cancellationToken);
        if (affectedRows == 0)
        {
            throw new ConflictException("This application changed while you were editing it. Reload it and try again.");
        }

        return Unit.Value;
    }
}
