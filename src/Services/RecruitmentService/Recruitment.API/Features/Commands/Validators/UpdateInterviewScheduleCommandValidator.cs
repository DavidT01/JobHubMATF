using FluentValidation;
using Recruitment.API.Features.Commands.UpdateInterviewSchedule;

namespace Recruitment.API.Features.Commands.Validators;

public class UpdateInterviewScheduleCommandValidator : AbstractValidator<UpdateInterviewScheduleCommand>
{
    public UpdateInterviewScheduleCommandValidator()
    {
        RuleFor(command => command.InterviewScheduleId).NotEmpty();
        RuleFor(command => command.Title).NotEmpty().MaximumLength(200);
        RuleFor(command => command.StartTime).LessThan(command => command.EndTime);
        RuleForEach(command => command.AdditionalAttendeeEmails).Must(EmailValidation.IsValidEmail)
            .WithMessage("Each attendee email must be valid.");
    }
}
