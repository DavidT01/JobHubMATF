using FluentAssertions;
using Microsoft.Extensions.Logging.Abstractions;
using Recruitment.API.Entities;
using Recruitment.API.Features.Queries.GetProcessByJobId;
using Recruitment.UnitTests.Common;

namespace Recruitment.UnitTests.Queries
{
    public class GetProcessByJobIdQueryHandlerTests
    {
        [Fact]
        public async Task Handle_ProcessNotFound_ReturnsNull()
        {
            using var context = TestHelpers.CreateDbContext();
            var mapper = TestHelpers.CreateMapper();
            var handler = new GetProcessByJobIdQueryHandler(context, mapper, NullLogger<GetProcessByJobIdQueryHandler>.Instance);

            var result = await handler.Handle(new GetProcessByJobIdQuery(Guid.NewGuid()), CancellationToken.None);

            result.Should().BeNull();
        }

        [Fact]
        public async Task Handle_ProcessFound_ReturnsDtoWithOrderedRounds()
        {
            using var context = TestHelpers.CreateDbContext();
            var mapper = TestHelpers.CreateMapper();
            var jobId = Guid.NewGuid();
            var process = new RecruitmentProcess { CompanyId = Guid.NewGuid(), JobId = jobId };
            process.Rounds.Add(new SelectionRound { RecruitmentProcessId = process.Id, Title = "R2", Index = 1 });
            process.Rounds.Add(new SelectionRound { RecruitmentProcessId = process.Id, Title = "R1", Index = 0 });
            context.Processes.Add(process);
            await context.SaveChangesAsync();

            var handler = new GetProcessByJobIdQueryHandler(context, mapper, NullLogger<GetProcessByJobIdQueryHandler>.Instance);

            var result = await handler.Handle(new GetProcessByJobIdQuery(jobId), CancellationToken.None);

            result.Should().NotBeNull();
            result.JobId.Should().Be(jobId);
            result.Rounds.Should().HaveCount(2);
            result.Rounds.Select(r => r.OrderIndex).Should().BeEquivalentTo([0, 1]);
        }
    }
}
