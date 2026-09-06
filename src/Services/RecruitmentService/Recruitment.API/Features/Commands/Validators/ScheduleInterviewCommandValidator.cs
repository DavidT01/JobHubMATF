using FluentValidation;
using Recruitment.API.Features.Commands.ScheduleInterview;

namespace Recruitment.API.Features.Commands.Validators;

public class ScheduleInterviewCommandValidator : AbstractValidator<ScheduleInterviewCommand>
{
    public ScheduleInterviewCommandValidator()
    {
        RuleFor(command => command.SelectionRoundId).NotEmpty();
        RuleFor(command => command.CandidateProfileId).NotEmpty();
        RuleFor(command => command.Title).NotEmpty().MaximumLength(200);
        RuleFor(command => command.StartTime).LessThan(command => command.EndTime);
        RuleForEach(command => command.AttendeeEmails).Must(EmailValidation.IsValidEmail)
            .WithMessage("Each attendee email must be valid.");
    }
}
