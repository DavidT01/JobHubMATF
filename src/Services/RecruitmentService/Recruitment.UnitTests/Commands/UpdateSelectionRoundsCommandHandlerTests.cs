using FluentAssertions;
using Microsoft.Extensions.Logging.Abstractions;
using Recruitment.API.DTOs;
using Recruitment.API.Entities;
using Recruitment.API.Features.Commands.UpdateSelectionRounds;
using Recruitment.UnitTests.Common;

namespace Recruitment.UnitTests.Commands
{
    public class UpdateSelectionRoundsCommandHandlerTests
    {
        [Fact]
        public async Task Handle_ProcessNotFound_ReturnsFalse()
        {
            using var context = TestHelpers.CreateDbContext();
            var mapper = TestHelpers.CreateMapper();
            var handler = new UpdateSelectionRoundsCommandHandler(context, mapper, NullLogger<UpdateSelectionRoundsCommandHandler>.Instance);

            var command = new UpdateSelectionRoundsCommand { ProcessId = Guid.NewGuid(), Rounds = [] };

            var result = await handler.Handle(command, CancellationToken.None);

            result.Should().BeFalse();
        }

        [Fact]
        public async Task Handle_NewRounds_AddsRoundsToProcess()
        {
            using var context = TestHelpers.CreateDbContext();
            var mapper = TestHelpers.CreateMapper();
            var process = new RecruitmentProcess { CompanyId = Guid.NewGuid(), JobId = Guid.NewGuid() };
            context.Processes.Add(process);
            await context.SaveChangesAsync();
            context.ChangeTracker.Clear();

            var handler = new UpdateSelectionRoundsCommandHandler(context, mapper, NullLogger<UpdateSelectionRoundsCommandHandler>.Instance);
            var command = new UpdateSelectionRoundsCommand
            {
                ProcessId = process.Id,
                Rounds =
                [
                    new SelectionRoundInsertDto { Title = "HR Interview", OrderIndex = 0 },
                    new SelectionRoundInsertDto { Title = "Technical Interview", OrderIndex = 1 }
                ]
            };

            var result = await handler.Handle(command, CancellationToken.None);

            result.Should().BeTrue();
            var savedProcess = await context.Processes.FindAsync(process.Id);
            context.Rounds.Should().HaveCount(2);
        }

        [Fact]
        public async Task Handle_ExistingRoundRemovedFromRequest_IsDeleted()
        {
            using var context = TestHelpers.CreateDbContext();
            var mapper = TestHelpers.CreateMapper();
            var process = new RecruitmentProcess { CompanyId = Guid.NewGuid(), JobId = Guid.NewGuid() };
            var round = new SelectionRound { RecruitmentProcessId = process.Id, Title = "Old Round", Index = 0 };
            process.Rounds.Add(round);
            context.Processes.Add(process);
            await context.SaveChangesAsync();

            var handler = new UpdateSelectionRoundsCommandHandler(context, mapper, NullLogger<UpdateSelectionRoundsCommandHandler>.Instance);
            var command = new UpdateSelectionRoundsCommand { ProcessId = process.Id, Rounds = [] };

            var result = await handler.Handle(command, CancellationToken.None);

            result.Should().BeTrue();
            context.Rounds.Should().BeEmpty();
        }

        [Fact]
        public async Task Handle_ExistingRoundInRequest_IsUpdated()
        {
            using var context = TestHelpers.CreateDbContext();
            var mapper = TestHelpers.CreateMapper();
            var process = new RecruitmentProcess { CompanyId = Guid.NewGuid(), JobId = Guid.NewGuid() };
            var round = new SelectionRound { RecruitmentProcessId = process.Id, Title = "Old Title", Index = 0 };
            process.Rounds.Add(round);
            context.Processes.Add(process);
            await context.SaveChangesAsync();

            var handler = new UpdateSelectionRoundsCommandHandler(context, mapper, NullLogger<UpdateSelectionRoundsCommandHandler>.Instance);
            var command = new UpdateSelectionRoundsCommand
            {
                ProcessId = process.Id,
                Rounds = [new SelectionRoundInsertDto { Id = round.Id, Title = "New Title", OrderIndex = 0 }]
            };

            await handler.Handle(command, CancellationToken.None);

            var updated = await context.Rounds.FindAsync(round.Id);
            updated!.Title.Should().Be("New Title");
        }
    }
}
