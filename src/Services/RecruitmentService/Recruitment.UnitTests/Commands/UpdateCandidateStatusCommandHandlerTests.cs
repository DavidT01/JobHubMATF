using FluentAssertions;
using JobHub.Grpc.Contracts.Profile;
using Microsoft.Extensions.Logging.Abstractions;
using Moq;
using Recruitment.API.Entities;
using Recruitment.API.Enums;
using Recruitment.API.Features.Commands.UpdateCandidateStatus;
using Recruitment.API.Infrastructure;
using Recruitment.UnitTests.Common;

namespace Recruitment.UnitTests.Commands;

public class UpdateCandidateStatusCommandHandlerTests
{
    [Fact]
    public async Task Handle_InvalidStatus_ThrowsValidationException()
    {
        using var context = TestHelpers.CreateDbContext();
        var profileServiceMock = new Mock<IProfileServiceClient>();
        var handler = CreateHandler(context, profileServiceMock);

        var act = () => handler.Handle(new UpdateCandidateStatusCommand
        {
            CandidateProfileId = Guid.NewGuid(),
            RecruitmentProcessId = Guid.NewGuid(),
            Status = CandidateProgressStatus.InProgress
        }, CancellationToken.None);

        await act.Should().ThrowAsync<Recruitment.API.Exceptions.RecruitmentValidationException>();
    }

    [Theory]
    [InlineData(CandidateProgressStatus.Rejected)]
    [InlineData(CandidateProgressStatus.Hired)]
    public async Task Handle_ValidStatus_UpdatesProgress(CandidateProgressStatus status)
    {
        using var context = TestHelpers.CreateDbContext();
        var candidateId = Guid.NewGuid();
        var processId = Guid.NewGuid();
        context.Progresses.Add(new CandidateProgress
        {
            CandidateProfileId = candidateId,
            RecruitmentProcessId = processId,
            Status = CandidateProgressStatus.InProgress
        });
        await context.SaveChangesAsync();

        var profileServiceMock = new Mock<IProfileServiceClient>();
        profileServiceMock
            .Setup(client => client.ValidateCandidateProfileAsync(candidateId, It.IsAny<CancellationToken>()))
            .ReturnsAsync(true);
        var handler = CreateHandler(context, profileServiceMock);

        var result = await handler.Handle(new UpdateCandidateStatusCommand
        {
            CandidateProfileId = candidateId,
            RecruitmentProcessId = processId,
            Status = status
        }, CancellationToken.None);

        result.Status.Should().Be(status);
        context.Progresses.Single().Status.Should().Be(status);
    }

    private static UpdateCandidateStatusCommandHandler CreateHandler(
        Recruitment.API.Data.RecruitmentContext context,
        Mock<IProfileServiceClient> profileService)
    {
        return new UpdateCandidateStatusCommandHandler(
            context,
            TestHelpers.CreateMapper(),
            profileService.Object,
            NullLogger<UpdateCandidateStatusCommandHandler>.Instance);
    }
}
