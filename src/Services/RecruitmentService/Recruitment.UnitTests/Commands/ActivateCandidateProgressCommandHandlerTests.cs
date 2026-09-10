using FluentAssertions;
using Microsoft.Extensions.Logging.Abstractions;
using Moq;
using Recruitment.API.Entities;
using Recruitment.API.Enums;
using Recruitment.API.Exceptions;
using Recruitment.API.Features.Commands.ActivateCandidateProgress;
using Recruitment.API.Infrastructure;
using Recruitment.UnitTests.Common;

namespace Recruitment.UnitTests.Commands;

public sealed class ActivateCandidateProgressCommandHandlerTests
{
    private const string JobId = "0123456789abcdef01234567";

    [Fact]
    public async Task Handle_FirstActivation_CreatesProgressOnFirstRound()
    {
        using var context = TestHelpers.CreateDbContext();
        var mapper = TestHelpers.CreateMapper();
        var process = new RecruitmentProcess { CompanyId = Guid.NewGuid(), JobId = JobId };
        var round = new SelectionRound { RecruitmentProcessId = process.Id, Title = "Screening", Index = 0 };
        process.Rounds.Add(round);
        context.Processes.Add(process);
        await context.SaveChangesAsync();

        var candidateId = Guid.NewGuid();
        var applicationId = Guid.NewGuid();
        var profileClient = new Mock<IProfileServiceClient>();
        profileClient
            .Setup(client => client.ValidateCandidateProfileAsync(candidateId, It.IsAny<CancellationToken>()))
            .ReturnsAsync(true);
        var handler = new ActivateCandidateProgressCommandHandler(
            context, mapper, profileClient.Object,
            NullLogger<ActivateCandidateProgressCommandHandler>.Instance);

        var result = await handler.Handle(
            new ActivateCandidateProgressCommand(candidateId, JobId, applicationId), CancellationToken.None);

        result.ApplicationId.Should().Be(applicationId);
        result.CandidateProfileId.Should().Be(candidateId);
        result.RecruitmentProcessId.Should().Be(process.Id);
        result.CurrentSelectionRoundId.Should().Be(round.Id);
        result.Status.Should().Be(CandidateProgressStatus.InProgress);
    }

    [Fact]
    public async Task Handle_RepeatedActivation_ReturnsExistingProgressWithoutAdvancing()
    {
        using var context = TestHelpers.CreateDbContext();
        var mapper = TestHelpers.CreateMapper();
        var process = new RecruitmentProcess { CompanyId = Guid.NewGuid(), JobId = JobId };
        var round = new SelectionRound { RecruitmentProcessId = process.Id, Title = "Screening", Index = 0 };
        process.Rounds.Add(round);
        var candidateId = Guid.NewGuid();
        var existing = new CandidateProgress
        {
            CandidateProfileId = candidateId,
            RecruitmentProcessId = process.Id,
            CurrentSelectionRoundId = round.Id,
            Status = CandidateProgressStatus.InProgress
        };
        context.Processes.Add(process);
        context.Progresses.Add(existing);
        await context.SaveChangesAsync();

        var profileClient = new Mock<IProfileServiceClient>();
        var handler = new ActivateCandidateProgressCommandHandler(
            context, mapper, profileClient.Object,
            NullLogger<ActivateCandidateProgressCommandHandler>.Instance);

        var result = await handler.Handle(
            new ActivateCandidateProgressCommand(candidateId, JobId), CancellationToken.None);

        result.Id.Should().Be(existing.Id);
        result.CurrentSelectionRoundId.Should().Be(round.Id);
        profileClient.Verify(
            client => client.ValidateCandidateProfileAsync(It.IsAny<Guid>(), It.IsAny<CancellationToken>()),
            Times.Never);
    }

    [Fact]
    public async Task Handle_UnknownJob_ThrowsValidationException()
    {
        using var context = TestHelpers.CreateDbContext();
        var handler = new ActivateCandidateProgressCommandHandler(
            context,
            TestHelpers.CreateMapper(),
            Mock.Of<IProfileServiceClient>(),
            NullLogger<ActivateCandidateProgressCommandHandler>.Instance);

        var act = () => handler.Handle(
            new ActivateCandidateProgressCommand(Guid.NewGuid(), JobId), CancellationToken.None);

        await act.Should().ThrowAsync<RecruitmentValidationException>();
    }
}
