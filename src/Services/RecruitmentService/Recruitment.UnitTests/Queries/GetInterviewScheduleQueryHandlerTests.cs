using FluentAssertions;
using Microsoft.Extensions.Logging.Abstractions;
using Recruitment.API.Entities;
using Recruitment.API.Features.Queries.GetInterviewSchedule;
using Recruitment.UnitTests.Common;

namespace Recruitment.UnitTests.Queries;

public class GetInterviewScheduleQueryHandlerTests
{
    [Fact]
    public async Task Handle_MissingSchedule_ReturnsNull()
    {
        using var context = TestHelpers.CreateDbContext();
        var handler = new GetInterviewScheduleQueryHandler(
            context,
            TestHelpers.CreateMapper(),
            NullLogger<GetInterviewScheduleQueryHandler>.Instance);

        var result = await handler.Handle(
            new GetInterviewScheduleQuery(Guid.NewGuid(), Guid.NewGuid()),
            CancellationToken.None);

        result.Should().BeNull();
    }

    [Fact]
    public async Task Handle_ExistingSchedule_ReturnsFullScheduleDto()
    {
        using var context = TestHelpers.CreateDbContext();
        var candidateId = Guid.NewGuid();
        var roundId = Guid.NewGuid();
        var schedule = new InterviewSchedule
        {
            CandidateProfileId = candidateId,
            SelectionRoundId = roundId,
            Title = "Technical interview",
            Description = "Backend round",
            AdditionalAttendeeEmails = ["hr@example.com"],
            StartTime = DateTime.UtcNow,
            EndTime = DateTime.UtcNow.AddHours(1),
            GoogleMeetUrl = "https://meet.google.com/test"
        };
        context.InterviewSchedules.Add(schedule);
        await context.SaveChangesAsync();

        var handler = new GetInterviewScheduleQueryHandler(
            context,
            TestHelpers.CreateMapper(),
            NullLogger<GetInterviewScheduleQueryHandler>.Instance);

        var result = await handler.Handle(
            new GetInterviewScheduleQuery(candidateId, roundId),
            CancellationToken.None);

        result.Should().NotBeNull();
        result!.Title.Should().Be("Technical interview");
        result.AdditionalAttendeeEmails.Should().ContainSingle("hr@example.com");
        result.GoogleMeetUrl.Should().Be("https://meet.google.com/test");
    }
}
