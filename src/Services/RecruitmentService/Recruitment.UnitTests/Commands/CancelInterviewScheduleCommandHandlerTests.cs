using FluentAssertions;
using Microsoft.Extensions.Logging.Abstractions;
using Moq;
using Recruitment.API.Entities;
using Recruitment.API.Features.Commands.CancelInterviewSchedule;
using Recruitment.API.Infrastructure;
using Recruitment.UnitTests.Common;

namespace Recruitment.UnitTests.Commands;

public class CancelInterviewScheduleCommandHandlerTests
{
    [Fact]
    public async Task Handle_MissingSchedule_ThrowsValidationException()
    {
        using var context = TestHelpers.CreateDbContext();
        var handler = new CancelInterviewScheduleCommandHandler(
            context,
            new Mock<IMeetingService>().Object,
            NullLogger<CancelInterviewScheduleCommandHandler>.Instance);

        var act = () => handler.Handle(
            new CancelInterviewScheduleCommand(Guid.NewGuid()),
            CancellationToken.None);

        await act.Should().ThrowAsync<Recruitment.API.Exceptions.RecruitmentValidationException>();
    }

    [Fact]
    public async Task Handle_ExistingSchedule_DeletesCalendarEventAndDatabaseRecord()
    {
        using var context = TestHelpers.CreateDbContext();
        var schedule = new InterviewSchedule { EventId = "event-123" };
        context.InterviewSchedules.Add(schedule);
        await context.SaveChangesAsync();
        var meetingServiceMock = new Mock<IMeetingService>();
        var handler = new CancelInterviewScheduleCommandHandler(
            context,
            meetingServiceMock.Object,
            NullLogger<CancelInterviewScheduleCommandHandler>.Instance);

        await handler.Handle(new CancelInterviewScheduleCommand(schedule.Id), CancellationToken.None);

        context.InterviewSchedules.Should().BeEmpty();
        meetingServiceMock.Verify(service => service.DeleteMeetingAsync("event-123"), Times.Once);
    }
}
