using FluentAssertions;
using Recruitment.API.Entities;
using Recruitment.API.Enums;
using Recruitment.API.Exceptions;
using Recruitment.API.Features.Queries.GetCandidateProgress;
using Recruitment.UnitTests.Common;

namespace Recruitment.UnitTests.Queries
{
    public class GetCandidateProgressQueryHandlerTests
    {
        [Fact]
        public async Task Handle_ProgressNotFound_ThrowsRecruitmentValidationException()
        {
            using var context = TestHelpers.CreateDbContext();
            var mapper = TestHelpers.CreateMapper();
            var handler = new GetCandidateProgressQueryHandler(context, mapper);

            var act = () => handler.Handle(new GetCandidateProgressQuery(Guid.NewGuid(), Guid.NewGuid()), CancellationToken.None);

            await act.Should().ThrowAsync<RecruitmentValidationException>();
        }

        [Fact]
        public async Task Handle_ProgressFound_ReturnsDto()
        {
            using var context = TestHelpers.CreateDbContext();
            var mapper = TestHelpers.CreateMapper();
            var candidateId = Guid.NewGuid();
            var processId = Guid.NewGuid();
            context.Progresses.Add(new CandidateProgress
            {
                CandidateProfileId = candidateId,
                RecruitmentProcessId = processId,
                Status = CandidateProgressStatus.InProgress
            });
            await context.SaveChangesAsync();

            var handler = new GetCandidateProgressQueryHandler(context, mapper);

            var result = await handler.Handle(new GetCandidateProgressQuery(candidateId, processId), CancellationToken.None);

            result.CandidateProfileId.Should().Be(candidateId);
            result.RecruitmentProcessId.Should().Be(processId);
            result.Status.Should().Be(CandidateProgressStatus.InProgress);
        }
    }
}
