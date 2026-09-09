using FluentAssertions;
using Recruitment.API.DTOs;
using Recruitment.API.Features.Commands.EvaluateCandidate;
using Recruitment.API.Features.Commands.ScheduleInterview;
using Recruitment.API.Features.Commands.UpdateInterviewSchedule;
using Recruitment.API.Features.Commands.UpdateSelectionRounds;
using Recruitment.API.Features.Commands.Validators;

namespace Recruitment.UnitTests.Commands;

public class RecruitmentValidatorTests
{
    [Fact]
    public async Task EvaluateCandidateValidator_RejectsScoreOutsideRange()
    {
        var validator = new EvaluateCandidateCommandValidator();
        var result = await validator.ValidateAsync(new EvaluateCandidateCommand
        {
            CandidateProfileId = Guid.NewGuid(),
            SelectionRoundId = Guid.NewGuid(),
            Score = 11
        });

        result.IsValid.Should().BeFalse();
        result.Errors.Should().Contain(error => error.PropertyName == "Score");
    }

    [Fact]
    public async Task ScheduleInterviewValidator_RejectsInvalidAttendeeEmailAndReversedDates()
    {
        var validator = new ScheduleInterviewCommandValidator();
        var start = DateTime.UtcNow.AddHours(2);
        var result = await validator.ValidateAsync(new ScheduleInterviewCommand
        {
            SelectionRoundId = Guid.NewGuid(),
            CandidateProfileId = Guid.NewGuid(),
            Title = "Interview",
            StartTime = start,
            EndTime = start.AddHours(-1),
            AttendeeEmails = ["invalid-email"]
        });

        result.IsValid.Should().BeFalse();
        result.Errors.Should().Contain(error => error.PropertyName == "StartTime");
        result.Errors.Should().Contain(error => error.PropertyName == "AttendeeEmails[0]");
    }

    [Fact]
    public async Task UpdateInterviewScheduleValidator_RejectsEmptyTitleAndInvalidEmail()
    {
        var validator = new UpdateInterviewScheduleCommandValidator();
        var result = await validator.ValidateAsync(new UpdateInterviewScheduleCommand
        {
            InterviewScheduleId = Guid.NewGuid(),
            StartTime = DateTime.UtcNow,
            EndTime = DateTime.UtcNow.AddHours(1),
            Title = "",
            AdditionalAttendeeEmails = ["invalid-email"]
        });

        result.IsValid.Should().BeFalse();
        result.Errors.Should().Contain(error => error.PropertyName == "Title");
        result.Errors.Should().Contain(error => error.PropertyName == "AdditionalAttendeeEmails[0]");
    }

    [Fact]
    public async Task UpdateSelectionRoundsValidator_RejectsDuplicateTitles()
    {
        var validator = new UpdateSelectionRoundsCommandValidator();
        var result = await validator.ValidateAsync(new UpdateSelectionRoundsCommand
        {
            ProcessId = Guid.NewGuid(),
            Rounds =
            [
                new SelectionRoundInsertDto { Title = "Technical", OrderIndex = 0 },
                new SelectionRoundInsertDto { Title = " technical ", OrderIndex = 1 }
            ]
        });

        result.IsValid.Should().BeFalse();
        result.Errors.Should().Contain(error => error.PropertyName == "Rounds");
    }

    [Fact]
    public void EmailValidation_AcceptsValidAndRejectsInvalidAddresses()
    {
        EmailValidation.IsValidEmail("candidate@example.com").Should().BeTrue();
        EmailValidation.IsValidEmail("candidate.example.com").Should().BeFalse();
    }
}
