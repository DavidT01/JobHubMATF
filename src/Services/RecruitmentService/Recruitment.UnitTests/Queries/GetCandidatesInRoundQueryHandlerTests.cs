using FluentAssertions;
using Microsoft.Extensions.Logging.Abstractions;
using Recruitment.API.Entities;
using Recruitment.API.Enums;
using Recruitment.API.Features.Queries.GetCandidatesInRound;
using Recruitment.UnitTests.Common;

namespace Recruitment.UnitTests.Queries;

public class GetCandidatesInRoundQueryHandlerTests
{
    [Fact]
    public async Task Handle_ReturnsCandidatesInRequestedRoundInCreationOrder()
    {
        using var context = TestHelpers.CreateDbContext();
        var mapper = TestHelpers.CreateMapper();
        var roundId = Guid.NewGuid();
        var otherRoundId = Guid.NewGuid();
        var firstCandidate = Guid.NewGuid();
        var secondCandidate = Guid.NewGuid();

        context.Progresses.AddRange(
            new CandidateProgress
            {
                CandidateProfileId = firstCandidate,
                CurrentSelectionRoundId = roundId,
                CreatedAt = DateTime.UtcNow.AddMinutes(-2),
                Status = CandidateProgressStatus.InProgress
            },
            new CandidateProgress
            {
                CandidateProfileId = secondCandidate,
                CurrentSelectionRoundId = roundId,
                CreatedAt = DateTime.UtcNow.AddMinutes(-1),
                Status = CandidateProgressStatus.Completed
            },
            new CandidateProgress
            {
                CandidateProfileId = Guid.NewGuid(),
                CurrentSelectionRoundId = otherRoundId,
                Status = CandidateProgressStatus.InProgress
            });
        await context.SaveChangesAsync();

        var handler = new GetCandidatesInRoundQueryHandler(
            context,
            mapper,
            NullLogger<GetCandidatesInRoundQueryHandler>.Instance);

        var result = await handler.Handle(new GetCandidatesInRoundQuery(roundId), CancellationToken.None);

        result.Should().HaveCount(2);
        result.Select(candidate => candidate.CandidateProfileId)
            .Should().ContainInOrder(firstCandidate, secondCandidate);
    }
}
