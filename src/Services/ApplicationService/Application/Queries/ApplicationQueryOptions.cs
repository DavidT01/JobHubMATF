using ApplicationService.Application.Exceptions;
using ApplicationService.Domain.Entities;
using ApplicationService.Domain.Enums;

namespace ApplicationService.Application.Queries;

public enum ApplicationSortBy
{
    SubmittedAtUtc = 1,
    UpdatedAtUtc = 2
}

public enum SortDirection
{
    Asc = 1,
    Desc = 2
}

internal static class ApplicationQueryOptions
{
    public static void Validate(
        ApplicationStatus? status, ApplicationSortBy sortBy, SortDirection sortDirection)
    {
        var errors = new Dictionary<string, string[]>();
        if (status.HasValue && !Enum.IsDefined(status.Value))
        {
            errors["status"] = ["Application status is not valid."];
        }

        if (!Enum.IsDefined(sortBy))
        {
            errors["sortBy"] = ["Sort field must be SubmittedAtUtc or UpdatedAtUtc."];
        }

        if (!Enum.IsDefined(sortDirection))
        {
            errors["sortDirection"] = ["Sort direction must be Asc or Desc."];
        }

        if (errors.Count > 0)
        {
            throw new RequestValidationException(errors);
        }
    }

    public static IOrderedQueryable<JobApplication> ApplyOrdering(
        IQueryable<JobApplication> query, ApplicationSortBy sortBy, SortDirection sortDirection)
    {
        return (sortBy, sortDirection) switch
        {
            (ApplicationSortBy.SubmittedAtUtc, SortDirection.Asc) =>
                query.OrderBy(application => application.SubmittedAtUtc).ThenBy(application => application.Id),
            (ApplicationSortBy.SubmittedAtUtc, SortDirection.Desc) =>
                query.OrderByDescending(application => application.SubmittedAtUtc).ThenBy(application => application.Id),
            (ApplicationSortBy.UpdatedAtUtc, SortDirection.Asc) =>
                query.OrderBy(application => application.UpdatedAtUtc).ThenBy(application => application.Id),
            (ApplicationSortBy.UpdatedAtUtc, SortDirection.Desc) =>
                query.OrderByDescending(application => application.UpdatedAtUtc).ThenBy(application => application.Id),
            _ => throw new InvalidOperationException("Application query options must be validated before sorting.")
        };
    }
}
