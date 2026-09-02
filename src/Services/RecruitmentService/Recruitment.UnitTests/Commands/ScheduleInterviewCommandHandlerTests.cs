using FluentAssertions;
using Moq;
using Recruitment.API.Entities;
using Recruitment.API.Exceptions;
using Recruitment.API.Features.Commands.ScheduleInterview;
using Recruitment.API.Infrastructure;
using Recruitment.UnitTests.Common;

namespace Recruitment.UnitTests.Commands
{
    public class ScheduleInterviewCommandHandlerTests
    {
        [Fact]
        public async Task Handle_RoundNotFound_ThrowsRecruitmentValidationException()
        {
            using var context = TestHelpers.CreateDbContext();
            var mapper = TestHelpers.CreateMapper();
            var meetingServiceMock = new Mock<IMeetingService>();
            var handler = new ScheduleInterviewCommandHandler(context, meetingServiceMock.Object, mapper);

            var command = new ScheduleInterviewCommand
            {
                SelectionRoundId = Guid.NewGuid(),
                CandidateProfileId = Guid.NewGuid(),
                StartTime = DateTime.UtcNow,
                EndTime = DateTime.UtcNow.AddHours(1)
            };

            var act = () => handler.Handle(command, CancellationToken.None);

            await act.Should().ThrowAsync<RecruitmentValidationException>();
            meetingServiceMock.Verify(m => m.ScheduleMeetingAsync(It.IsAny<string>(), It.IsAny<string>(), It.IsAny<DateTime>(), It.IsAny<DateTime>(), It.IsAny<string[]>()), Times.Never);
        }

        [Fact]
        public async Task Handle_ValidRound_SchedulesMeetingAndSavesSchedule()
        {
            using var context = TestHelpers.CreateDbContext();
            var mapper = TestHelpers.CreateMapper();
            var round = new SelectionRound { Title = "Interview Round", Index = 0 };
            context.Rounds.Add(round);
            await context.SaveChangesAsync();

            var meetingServiceMock = new Mock<IMeetingService>();
            meetingServiceMock
                .Setup(m => m.ScheduleMeetingAsync(It.IsAny<string>(), It.IsAny<string>(), It.IsAny<DateTime>(), It.IsAny<DateTime>(), It.IsAny<string[]>()))
                .ReturnsAsync(("event-123", "https://meet.google.com/abc-defg-hij"));

            var handler = new ScheduleInterviewCommandHandler(context, meetingServiceMock.Object, mapper);
            var command = new ScheduleInterviewCommand
            {
                SelectionRoundId = round.Id,
                CandidateProfileId = Guid.NewGuid(),
                StartTime = DateTime.UtcNow,
                EndTime = DateTime.UtcNow.AddHours(1),
                Title = "Tech Interview",
                Description = "First round",
                AttendeeEmails = ["candidate@example.com"]
            };

            var result = await handler.Handle(command, CancellationToken.None);

            result.GoogleMeetUrl.Should().Be("https://meet.google.com/abc-defg-hij");
            context.InterviewSchedules.Should().ContainSingle(s => s.EventId == "event-123");
            meetingServiceMock.Verify(m => m.ScheduleMeetingAsync("Tech Interview", "First round", command.StartTime, command.EndTime, command.AttendeeEmails), Times.Once);
        }
    }
}
