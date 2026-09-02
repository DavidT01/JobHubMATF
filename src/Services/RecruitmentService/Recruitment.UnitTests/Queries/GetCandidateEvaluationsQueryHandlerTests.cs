using FluentAssertions;
using Recruitment.API.Entities;
using Recruitment.API.Features.Queries.GetCandidateEvaluations;
using Recruitment.UnitTests.Common;

namespace Recruitment.UnitTests.Queries
{
    public class GetCandidateEvaluationsQueryHandlerTests
    {
        [Fact]
        public async Task Handle_NoEvaluations_ReturnsEmptyList()
        {
            using var context = TestHelpers.CreateDbContext();
            var mapper = TestHelpers.CreateMapper();
            var handler = new GetCandidateEvaluationsQueryHandler(context, mapper);

            var result = await handler.Handle(new GetCandidateEvaluationsQuery(Guid.NewGuid()), CancellationToken.None);

            result.Should().BeEmpty();
        }

        [Fact]
        public async Task Handle_EvaluationsExist_ReturnsOnlyMatchingCandidate()
        {
            using var context = TestHelpers.CreateDbContext();
            var mapper = TestHelpers.CreateMapper();
            var candidateId = Guid.NewGuid();
            context.Evaluations.AddRange(
                new CandidateEvaluation { CandidateProfileId = candidateId, SelectionRoundId = Guid.NewGuid(), Score = 7 },
                new CandidateEvaluation { CandidateProfileId = candidateId, SelectionRoundId = Guid.NewGuid(), Score = 9 },
                new CandidateEvaluation { CandidateProfileId = Guid.NewGuid(), SelectionRoundId = Guid.NewGuid(), Score = 5 });
            await context.SaveChangesAsync();

            var handler = new GetCandidateEvaluationsQueryHandler(context, mapper);

            var result = await handler.Handle(new GetCandidateEvaluationsQuery(candidateId), CancellationToken.None);

            result.Should().HaveCount(2);
            result.Should().OnlyContain(e => e.CandidateProfileId == candidateId);
        }
    }
}
