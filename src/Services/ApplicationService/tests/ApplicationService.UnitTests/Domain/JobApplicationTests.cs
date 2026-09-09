using ApplicationService.Domain.Entities;
using ApplicationService.Domain.Enums;
using ApplicationService.Domain.Exceptions;

namespace ApplicationService.UnitTests.Domain;

public sealed class JobApplicationTests
{
    private static readonly Guid CandidateId = Guid.Parse("11111111-1111-1111-1111-111111111111");
    private static readonly DateTimeOffset SubmittedAt = new(2026, 9, 8, 8, 0, 0, TimeSpan.FromHours(2));

    [Fact]
    public void Create_NormalizesIdentifiersLetterAndTime()
    {
        var application = JobApplication.Create(
            CandidateId,
            "candidate-user",
            "ABCDEFABCDEFABCDEFABCDEF",
            "  A concise cover letter.  ",
            SubmittedAt);

        Assert.NotEqual(Guid.Empty, application.Id);
        Assert.Equal(CandidateId, application.CandidateId);
        Assert.Equal("candidate-user", application.CandidateUserId);
        Assert.Equal("abcdefabcdefabcdefabcdef", application.JobId);
        Assert.Equal("A concise cover letter.", application.CoverLetter);
        Assert.Equal(ApplicationStatus.Submitted, application.Status);
        Assert.Equal(SubmittedAt.ToUniversalTime(), application.SubmittedAtUtc);
        Assert.Equal(application.SubmittedAtUtc, application.UpdatedAtUtc);
    }

    [Theory]
    [InlineData(null)]
    [InlineData("")]
    [InlineData("   ")]
    public void Create_NormalizesEmptyCoverLetterToNull(string? coverLetter)
    {
        var application = Create(coverLetter);

        Assert.Null(application.CoverLetter);
    }

    [Fact]
    public void Create_AcceptsCoverLetterAtMaximumLength()
    {
        var coverLetter = new string('a', JobApplication.MaximumCoverLetterLength);

        var application = Create(coverLetter);

        Assert.Equal(coverLetter, application.CoverLetter);
    }

    [Theory]
    [InlineData("", "0123456789abcdef01234567")]
    [InlineData("   ", "0123456789abcdef01234567")]
    [InlineData("candidate-user", "")]
    [InlineData("candidate-user", "0123456789abcdef0123456")]
    [InlineData("candidate-user", "0123456789abcdef0123456z")]
    public void Create_RejectsInvalidUserOrJobIdentifier(string candidateUserId, string jobId)
    {
        Assert.Throws<ApplicationDomainException>(() => JobApplication.Create(
            CandidateId, candidateUserId, jobId, null, SubmittedAt));
    }

    [Fact]
    public void Create_RejectsEmptyCandidateIdentifier()
    {
        Assert.Throws<ApplicationDomainException>(() => JobApplication.Create(
            Guid.Empty, "candidate-user", "0123456789abcdef01234567", null, SubmittedAt));
    }

    [Fact]
    public void Create_RejectsUserIdentifierAboveMaximumLength()
    {
        var candidateUserId = new string('a', JobApplication.MaximumUserIdLength + 1);

        Assert.Throws<ApplicationDomainException>(() => JobApplication.Create(
            CandidateId, candidateUserId, "0123456789abcdef01234567", null, SubmittedAt));
    }

    [Fact]
    public void Create_RejectsCoverLetterAboveMaximumLengthAfterTrimming()
    {
        var coverLetter = $" {new string('a', JobApplication.MaximumCoverLetterLength + 1)} ";

        Assert.Throws<ApplicationDomainException>(() => Create(coverLetter));
    }

    [Theory]
    [MemberData(nameof(AllStatusTransitions))]
    public void ChangeStatus_EnforcesTransitionMatrix(
        ApplicationStatus current,
        ApplicationStatus requested,
        bool allowed)
    {
        var application = MoveTo(current);
        var previousUpdate = application.UpdatedAtUtc;
        var changedAt = previousUpdate.AddMinutes(1);

        if (!allowed)
        {
            Assert.Throws<ApplicationDomainException>(() => application.ChangeStatus(requested, changedAt));
            Assert.Equal(current, application.Status);
            Assert.Equal(previousUpdate, application.UpdatedAtUtc);
            return;
        }

        application.ChangeStatus(requested, changedAt);

        Assert.Equal(requested, application.Status);
        Assert.Equal(current == requested ? previousUpdate : changedAt, application.UpdatedAtUtc);
    }

    [Fact]
    public void ChangeStatus_RejectsTimeBeforePreviousUpdate()
    {
        var application = Create();

        Assert.Throws<ApplicationDomainException>(() =>
            application.ChangeStatus(ApplicationStatus.InReview, application.UpdatedAtUtc.AddTicks(-1)));
        Assert.Equal(ApplicationStatus.Submitted, application.Status);
        Assert.Equal(SubmittedAt.ToUniversalTime(), application.UpdatedAtUtc);
    }

    [Fact]
    public void ChangeStatus_StoresUtcTime()
    {
        var application = Create();
        var changedAt = new DateTimeOffset(2026, 9, 8, 11, 0, 0, TimeSpan.FromHours(3));

        application.ChangeStatus(ApplicationStatus.InReview, changedAt);

        Assert.Equal(TimeSpan.Zero, application.UpdatedAtUtc.Offset);
        Assert.Equal(changedAt.ToUniversalTime(), application.UpdatedAtUtc);
    }

    public static TheoryData<ApplicationStatus, ApplicationStatus, bool> AllStatusTransitions()
    {
        var allowed = new HashSet<(ApplicationStatus From, ApplicationStatus To)>
        {
            (ApplicationStatus.Submitted, ApplicationStatus.InReview),
            (ApplicationStatus.Submitted, ApplicationStatus.Rejected),
            (ApplicationStatus.InReview, ApplicationStatus.Interview),
            (ApplicationStatus.InReview, ApplicationStatus.Accepted),
            (ApplicationStatus.InReview, ApplicationStatus.Rejected),
            (ApplicationStatus.Interview, ApplicationStatus.Accepted),
            (ApplicationStatus.Interview, ApplicationStatus.Rejected)
        };
        var data = new TheoryData<ApplicationStatus, ApplicationStatus, bool>();

        foreach (var current in Enum.GetValues<ApplicationStatus>())
        {
            foreach (var requested in Enum.GetValues<ApplicationStatus>())
            {
                data.Add(current, requested, current == requested || allowed.Contains((current, requested)));
            }
        }

        return data;
    }

    private static JobApplication Create(string? coverLetter = null) => JobApplication.Create(
        CandidateId, "candidate-user", "0123456789abcdef01234567", coverLetter, SubmittedAt);

    private static JobApplication MoveTo(ApplicationStatus status)
    {
        var application = Create();
        var changedAt = application.UpdatedAtUtc.AddMinutes(1);

        switch (status)
        {
            case ApplicationStatus.InReview:
                application.ChangeStatus(ApplicationStatus.InReview, changedAt);
                break;
            case ApplicationStatus.Interview:
                application.ChangeStatus(ApplicationStatus.InReview, changedAt);
                application.ChangeStatus(ApplicationStatus.Interview, changedAt.AddMinutes(1));
                break;
            case ApplicationStatus.Rejected:
                application.ChangeStatus(ApplicationStatus.Rejected, changedAt);
                break;
            case ApplicationStatus.Accepted:
                application.ChangeStatus(ApplicationStatus.InReview, changedAt);
                application.ChangeStatus(ApplicationStatus.Accepted, changedAt.AddMinutes(1));
                break;
        }

        return application;
    }
}
