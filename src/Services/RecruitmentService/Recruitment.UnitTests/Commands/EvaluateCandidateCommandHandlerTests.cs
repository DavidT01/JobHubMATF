using FluentAssertions;
using Recruitment.API.Entities;
using Recruitment.API.Exceptions;
using Recruitment.API.Features.Commands.EvaluateCandidate;
using Recruitment.UnitTests.Common;

namespace Recruitment.UnitTests.Commands
{
    public class EvaluateCandidateCommandHandlerTests
    {
        [Fact]
        public async Task Handle_RoundNotFound_ThrowsRecruitmentValidationException()
        {
            using var context = TestHelpers.CreateDbContext();
            var mapper = TestHelpers.CreateMapper();
            var handler = new EvaluateCandidateCommandHandler(context, mapper);

            var command = new EvaluateCandidateCommand
            {
                CandidateProfileId = Guid.NewGuid(),
                SelectionRoundId = Guid.NewGuid(),
                Score = 8
            };

            var act = () => handler.Handle(command, CancellationToken.None);

            await act.Should().ThrowAsync<RecruitmentValidationException>();
        }

        [Fact]
        public async Task Handle_NewEvaluation_IsAddedAndReturned()
        {
            using var context = TestHelpers.CreateDbContext();
            var mapper = TestHelpers.CreateMapper();
            var round = new SelectionRound { Title = "Round 1", Index = 0 };
            context.Rounds.Add(round);
            await context.SaveChangesAsync();

            var handler = new EvaluateCandidateCommandHandler(context, mapper);
            var candidateId = Guid.NewGuid();
            var command = new EvaluateCandidateCommand
            {
                CandidateProfileId = candidateId,
                SelectionRoundId = round.Id,
                Score = 9,
                Notes = "Great candidate"
            };

            var result = await handler.Handle(command, CancellationToken.None);

            result.Score.Should().Be(9);
            result.Notes.Should().Be("Great candidate");
            context.Evaluations.Should().ContainSingle(e => e.CandidateProfileId == candidateId);
        }

        [Fact]
        public async Task Handle_ExistingEvaluation_IsUpdated()
        {
            using var context = TestHelpers.CreateDbContext();
            var mapper = TestHelpers.CreateMapper();
            var round = new SelectionRound { Title = "Round 1", Index = 0 };
            context.Rounds.Add(round);
            await context.SaveChangesAsync();

            var candidateId = Guid.NewGuid();
            var existing = new CandidateEvaluation { CandidateProfileId = candidateId, SelectionRoundId = round.Id, Score = 5 };
            context.Evaluations.Add(existing);
            await context.SaveChangesAsync();

            var handler = new EvaluateCandidateCommandHandler(context, mapper);
            var command = new EvaluateCandidateCommand
            {
                CandidateProfileId = candidateId,
                SelectionRoundId = round.Id,
                Score = 10,
                Notes = "Updated"
            };

            var result = await handler.Handle(command, CancellationToken.None);

            result.Score.Should().Be(10);
            result.Notes.Should().Be("Updated");
            context.Evaluations.Should().ContainSingle();
        }
    }
}
