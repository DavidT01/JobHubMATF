using FluentAssertions;
using Microsoft.Extensions.Logging.Abstractions;
using Moq;
using Recruitment.API.Entities;
using Recruitment.API.Enums;
using Recruitment.API.Exceptions;
using Recruitment.API.Features.Commands.AdvanceCandidate;
using Recruitment.API.Infrastructure;
using Recruitment.UnitTests.Common;

namespace Recruitment.UnitTests.Commands
{
    public class AdvanceCandidateCommandHandlerTests
    {
        [Fact]
        public async Task Handle_ProcessNotFound_ThrowsRecruitmentValidationException()
        {
            using var context = TestHelpers.CreateDbContext();
            var mapper = TestHelpers.CreateMapper();
            var profileServiceMock = new Mock<IProfileServiceClient>();
            var handler = new AdvanceCandidateCommandHandler(context, mapper, profileServiceMock.Object, NullLogger<AdvanceCandidateCommandHandler>.Instance);

            var command = new AdvanceCandidateCommand { CandidateProfileId = Guid.NewGuid(), RecruitmentProcessId = Guid.NewGuid() };

            var act = () => handler.Handle(command, CancellationToken.None);

            await act.Should().ThrowAsync<RecruitmentValidationException>();
        }

        [Fact]
        public async Task Handle_NoExistingProgress_CreatesProgressOnFirstRound()
        {
            using var context = TestHelpers.CreateDbContext();
            var mapper = TestHelpers.CreateMapper();
            var process = new RecruitmentProcess { CompanyId = Guid.NewGuid(), JobId = Guid.NewGuid() };
            var round1 = new SelectionRound { RecruitmentProcessId = process.Id, Title = "R1", Index = 0 };
            var round2 = new SelectionRound { RecruitmentProcessId = process.Id, Title = "R2", Index = 1 };
            process.Rounds.Add(round1);
            process.Rounds.Add(round2);
            context.Processes.Add(process);
            await context.SaveChangesAsync();

            var candidateId = Guid.NewGuid();
            var profileServiceMock = new Mock<IProfileServiceClient>();
            profileServiceMock
                .Setup(client => client.ValidateCandidateProfileAsync(candidateId, It.IsAny<CancellationToken>()))
                .ReturnsAsync(true);
            var handler = new AdvanceCandidateCommandHandler(context, mapper, profileServiceMock.Object, NullLogger<AdvanceCandidateCommandHandler>.Instance);
            var command = new AdvanceCandidateCommand { CandidateProfileId = candidateId, RecruitmentProcessId = process.Id };

            var result = await handler.Handle(command, CancellationToken.None);

            result.CurrentSelectionRoundId.Should().Be(round1.Id);
            result.Status.Should().Be(CandidateProgressStatus.InProgress);
        }

        [Fact]
        public async Task Handle_ExistingProgress_AdvancesToNextRound()
        {
            using var context = TestHelpers.CreateDbContext();
            var mapper = TestHelpers.CreateMapper();
            var process = new RecruitmentProcess { CompanyId = Guid.NewGuid(), JobId = Guid.NewGuid() };
            var round1 = new SelectionRound { RecruitmentProcessId = process.Id, Title = "R1", Index = 0 };
            var round2 = new SelectionRound { RecruitmentProcessId = process.Id, Title = "R2", Index = 1 };
            process.Rounds.Add(round1);
            process.Rounds.Add(round2);
            context.Processes.Add(process);
            await context.SaveChangesAsync();

            var candidateId = Guid.NewGuid();
            var progress = new CandidateProgress
            {
                CandidateProfileId = candidateId,
                RecruitmentProcessId = process.Id,
                CurrentSelectionRoundId = round1.Id,
                Status = CandidateProgressStatus.InProgress
            };
            context.Progresses.Add(progress);
            await context.SaveChangesAsync();

            var command = new AdvanceCandidateCommand { CandidateProfileId = candidateId, RecruitmentProcessId = process.Id };
            var profileServiceMock = new Mock<IProfileServiceClient>();
            profileServiceMock
                .Setup(client => client.ValidateCandidateProfileAsync(candidateId, It.IsAny<CancellationToken>()))
                .ReturnsAsync(true);
            var handler = new AdvanceCandidateCommandHandler(context, mapper, profileServiceMock.Object, NullLogger<AdvanceCandidateCommandHandler>.Instance);

            var result = await handler.Handle(command, CancellationToken.None);

            result.CurrentSelectionRoundId.Should().Be(round2.Id);
            result.Status.Should().Be(CandidateProgressStatus.InProgress);
        }

        [Fact]
        public async Task Handle_LastRound_MarksAsCompleted()
        {
            using var context = TestHelpers.CreateDbContext();
            var mapper = TestHelpers.CreateMapper();
            var process = new RecruitmentProcess { CompanyId = Guid.NewGuid(), JobId = Guid.NewGuid() };
            var round1 = new SelectionRound { RecruitmentProcessId = process.Id, Title = "R1", Index = 0 };
            process.Rounds.Add(round1);
            context.Processes.Add(process);
            await context.SaveChangesAsync();

            var candidateId = Guid.NewGuid();
            var progress = new CandidateProgress
            {
                CandidateProfileId = candidateId,
                RecruitmentProcessId = process.Id,
                CurrentSelectionRoundId = round1.Id,
                Status = CandidateProgressStatus.InProgress
            };
            context.Progresses.Add(progress);
            await context.SaveChangesAsync();

            var command = new AdvanceCandidateCommand { CandidateProfileId = candidateId, RecruitmentProcessId = process.Id };
            var profileServiceMock = new Mock<IProfileServiceClient>();
            profileServiceMock
                .Setup(client => client.ValidateCandidateProfileAsync(candidateId, It.IsAny<CancellationToken>()))
                .ReturnsAsync(true);
            var handler = new AdvanceCandidateCommandHandler(context, mapper, profileServiceMock.Object, NullLogger<AdvanceCandidateCommandHandler>.Instance);

            var result = await handler.Handle(command, CancellationToken.None);

            result.Status.Should().Be(CandidateProgressStatus.Completed);
        }

        [Fact]
        public async Task Handle_ProgressNotInProgress_ThrowsRecruitmentValidationException()
        {
            using var context = TestHelpers.CreateDbContext();
            var mapper = TestHelpers.CreateMapper();
            var process = new RecruitmentProcess { CompanyId = Guid.NewGuid(), JobId = Guid.NewGuid() };
            context.Processes.Add(process);
            await context.SaveChangesAsync();

            var candidateId = Guid.NewGuid();
            var progress = new CandidateProgress
            {
                CandidateProfileId = candidateId,
                RecruitmentProcessId = process.Id,
                Status = CandidateProgressStatus.Rejected
            };
            context.Progresses.Add(progress);
            await context.SaveChangesAsync();

            var command = new AdvanceCandidateCommand { CandidateProfileId = candidateId, RecruitmentProcessId = process.Id };
            var profileServiceMock = new Mock<IProfileServiceClient>();
            profileServiceMock
                .Setup(client => client.ValidateCandidateProfileAsync(candidateId, It.IsAny<CancellationToken>()))
                .ReturnsAsync(true);
            var handler = new AdvanceCandidateCommandHandler(context, mapper, profileServiceMock.Object, NullLogger<AdvanceCandidateCommandHandler>.Instance);

            var act = () => handler.Handle(command, CancellationToken.None);

            await act.Should().ThrowAsync<RecruitmentValidationException>();
        }
    }
}
