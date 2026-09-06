using FluentValidation;
using Recruitment.API.Features.Commands.ScheduleInterview;
using System.Net.Mail;

namespace Recruitment.API.Features.Commands.Validators;

public class ScheduleInterviewCommandValidator : AbstractValidator<ScheduleInterviewCommand>
{
    public ScheduleInterviewCommandValidator()
    {
        RuleFor(command => command.SelectionRoundId).NotEmpty();
        RuleFor(command => command.CandidateProfileId).NotEmpty();
        RuleFor(command => command.Title).NotEmpty().MaximumLength(200);
        RuleFor(command => command.StartTime).LessThan(command => command.EndTime);
        RuleForEach(command => command.AttendeeEmails).Must(BeValidEmail)
            .WithMessage("Each attendee email must be valid.");
    }

    private static bool BeValidEmail(string email)
    {
        try
        {
            _ = new MailAddress(email);
            return true;
        }
        catch (FormatException)
        {
            return false;
        }
    }
}
