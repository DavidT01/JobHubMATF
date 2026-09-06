using FluentAssertions;
using JobHub.Grpc.Contracts.Profile;
using Microsoft.Extensions.Logging.Abstractions;
using Moq;
using Recruitment.API.Entities;
using Recruitment.API.Features.Commands.UpdateInterviewSchedule;
using Recruitment.API.Infrastructure;
using Recruitment.UnitTests.Common;

namespace Recruitment.UnitTests.Commands;

public class UpdateInterviewScheduleCommandHandlerTests
{
    [Fact]
    public async Task Handle_MissingSchedule_ThrowsValidationException()
    {
        using var context = TestHelpers.CreateDbContext();
        var handler = CreateHandler(context, new Mock<IMeetingService>(), new Mock<IProfileServiceClient>());

        var act = () => handler.Handle(new UpdateInterviewScheduleCommand
        {
            InterviewScheduleId = Guid.NewGuid(),
            StartTime = DateTime.UtcNow,
            EndTime = DateTime.UtcNow.AddHours(1),
            Title = "Updated"
        }, CancellationToken.None);

        await act.Should().ThrowAsync<Recruitment.API.Exceptions.RecruitmentValidationException>();
    }

    [Fact]
    public async Task Handle_ValidSchedule_UpdatesCalendarAndDatabase()
    {
        using var context = TestHelpers.CreateDbContext();
        var schedule = new InterviewSchedule
        {
            CandidateProfileId = Guid.NewGuid(),
            SelectionRoundId = Guid.NewGuid(),
            EventId = "event-123",
            Title = "Old title",
            Description = "Old description",
            StartTime = DateTime.UtcNow,
            EndTime = DateTime.UtcNow.AddHours(1)
        };
        context.InterviewSchedules.Add(schedule);
        await context.SaveChangesAsync();

        var meetingServiceMock = new Mock<IMeetingService>();
        var profileServiceMock = new Mock<IProfileServiceClient>();
        profileServiceMock
            .Setup(client => client.GetCandidateContactAsync(schedule.CandidateProfileId, It.IsAny<CancellationToken>()))
            .ReturnsAsync(new CandidateContactResponse { Email = "candidate@example.com" });
        var handler = CreateHandler(context, meetingServiceMock, profileServiceMock);
        var command = new UpdateInterviewScheduleCommand
        {
            InterviewScheduleId = schedule.Id,
            StartTime = DateTime.UtcNow.AddDays(1),
            EndTime = DateTime.UtcNow.AddDays(1).AddHours(1),
            Title = "Updated title",
            Description = "Updated description",
            AdditionalAttendeeEmails = ["hr@example.com"]
        };

        var result = await handler.Handle(command, CancellationToken.None);

        result.Title.Should().Be("Updated title");
        context.InterviewSchedules.Single().Description.Should().Be("Updated description");
        meetingServiceMock.Verify(service => service.UpdateMeetingAsync(
            "event-123", "Updated title", "Updated description", command.StartTime, command.EndTime,
            It.Is<string[]>(emails => emails.SequenceEqual(new[] { "candidate@example.com", "hr@example.com" }))), Times.Once);
    }

    private static UpdateInterviewScheduleCommandHandler CreateHandler(
        Recruitment.API.Data.RecruitmentContext context,
        Mock<IMeetingService> meetingService,
        Mock<IProfileServiceClient> profileService)
    {
        return new UpdateInterviewScheduleCommandHandler(
            context,
            TestHelpers.CreateMapper(),
            meetingService.Object,
            profileService.Object,
            NullLogger<UpdateInterviewScheduleCommandHandler>.Instance);
    }
}
