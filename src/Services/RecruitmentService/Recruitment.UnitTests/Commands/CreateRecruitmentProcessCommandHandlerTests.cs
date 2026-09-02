using FluentAssertions;
using Microsoft.Extensions.Logging.Abstractions;
using Recruitment.API.Features.Commands.CreateRecruitmentProcess;
using Recruitment.UnitTests.Common;

namespace Recruitment.UnitTests.Commands
{
    public class CreateRecruitmentProcessCommandHandlerTests
    {
        [Fact]
        public async Task Handle_ValidCommand_CreatesProcessAndReturnsId()
        {
            using var context = TestHelpers.CreateDbContext();
            var mapper = TestHelpers.CreateMapper();
            var handler = new CreateRecruitmentProcessCommandHandler(context, mapper, NullLogger<CreateRecruitmentProcessCommandHandler>.Instance);

            var command = new CreateRecruitmentProcessCommand
            {
                CompanyId = Guid.NewGuid(),
                JobId = Guid.NewGuid()
            };

            var resultId = await handler.Handle(command, CancellationToken.None);

            resultId.Should().NotBeEmpty();
            var saved = await context.Processes.FindAsync(resultId);
            saved.Should().NotBeNull();
            saved.CompanyId.Should().Be(command.CompanyId);
            saved.JobId.Should().Be(command.JobId);
            saved.Active.Should().BeFalse();
        }
    }
}
