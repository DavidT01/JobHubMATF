using System.Security.Claims;
using Microsoft.EntityFrameworkCore;
using Recruitment.API.Data;
using Recruitment.API.Exceptions;

namespace Recruitment.API.Infrastructure;

public sealed class RecruitmentAuthorization(
    IHttpContextAccessor httpContextAccessor,
    RecruitmentContext context,
    IProfileServiceClient profileClient) : IRecruitmentAuthorization
{
    private ClaimsPrincipal User => httpContextAccessor.HttpContext?.User ?? new ClaimsPrincipal();

    public async Task EnsureCompanyAsync(Guid companyId, CancellationToken cancellationToken)
    {
        if (User.IsInRole("Admin"))
        {
            return;
        }

        var currentCompanyId = await GetCurrentCompanyIdAsync(cancellationToken);
        if (currentCompanyId != companyId)
        {
            throw new RecruitmentForbiddenException();
        }
    }

    public async Task EnsureProcessOwnerAsync(Guid processId, CancellationToken cancellationToken)
    {
        var companyId = await context.Processes
            .Where(process => process.Id == processId)
            .Select(process => (Guid?)process.CompanyId)
            .SingleOrDefaultAsync(cancellationToken)
            ?? throw new RecruitmentValidationException($"Recruitment process {processId} was not found.");

        await EnsureCompanyAsync(companyId, cancellationToken);
    }

    public async Task EnsureRoundOwnerAsync(Guid selectionRoundId, CancellationToken cancellationToken)
    {
        var companyId = await context.Rounds
            .Where(round => round.Id == selectionRoundId)
            .Select(round => (Guid?)round.RecruitmentProcess!.CompanyId)
            .SingleOrDefaultAsync(cancellationToken)
            ?? throw new RecruitmentValidationException($"Selection round {selectionRoundId} was not found.");

        await EnsureCompanyAsync(companyId, cancellationToken);
    }

    public async Task EnsureInterviewOwnerAsync(Guid interviewScheduleId, CancellationToken cancellationToken)
    {
        var companyId = await context.InterviewSchedules
            .Where(schedule => schedule.Id == interviewScheduleId)
            .Select(schedule => (Guid?)schedule.SelectionRound!.RecruitmentProcess!.CompanyId)
            .SingleOrDefaultAsync(cancellationToken)
            ?? throw new RecruitmentValidationException($"Interview schedule {interviewScheduleId} was not found.");

        await EnsureCompanyAsync(companyId, cancellationToken);
    }

    public async Task EnsureEvaluationOwnerAsync(Guid candidateProfileId, CancellationToken cancellationToken)
    {
        var companyId = await context.Evaluations
            .Where(evaluation => evaluation.CandidateProfileId == candidateProfileId)
            .Select(evaluation => (Guid?)evaluation.SelectionRound!.RecruitmentProcess!.CompanyId)
            .FirstOrDefaultAsync(cancellationToken);

        if (companyId.HasValue)
        {
            await EnsureCompanyAsync(companyId.Value, cancellationToken);
            return;
        }

        await EnsureCandidateOwnsProfileAsync(candidateProfileId, cancellationToken);
    }

    public async Task EnsureCandidateOwnsProfileAsync(Guid candidateProfileId, CancellationToken cancellationToken)
    {
        if (User.IsInRole("Admin"))
        {
            return;
        }

        var userId = User.FindFirstValue(ClaimTypes.NameIdentifier) ?? User.FindFirstValue("sub");
        if (string.IsNullOrWhiteSpace(userId))
        {
            throw new RecruitmentForbiddenException();
        }

        var currentProfileId = await profileClient.GetCandidateProfileIdByUserIdAsync(userId, cancellationToken);
        if (currentProfileId != candidateProfileId)
        {
            throw new RecruitmentForbiddenException();
        }
    }

    private async Task<Guid> GetCurrentCompanyIdAsync(CancellationToken cancellationToken)
    {
        var userId = User.FindFirstValue(ClaimTypes.NameIdentifier) ?? User.FindFirstValue("sub");
        if (string.IsNullOrWhiteSpace(userId))
        {
            throw new RecruitmentForbiddenException();
        }

        return await profileClient.GetCompanyProfileIdByUserIdAsync(userId, cancellationToken)
            ?? throw new RecruitmentForbiddenException();
    }
}
