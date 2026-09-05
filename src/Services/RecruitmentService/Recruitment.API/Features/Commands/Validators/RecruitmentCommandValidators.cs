using FluentValidation;
using Recruitment.API.DTOs;
using Recruitment.API.Features.Commands.EvaluateCandidate;
using Recruitment.API.Features.Commands.ScheduleInterview;
using Recruitment.API.Features.Commands.UpdateSelectionRounds;
using System.Net.Mail;

namespace Recruitment.API.Features.Commands.Validators;

public class EvaluateCandidateCommandValidator : AbstractValidator<EvaluateCandidateCommand>
{
    public EvaluateCandidateCommandValidator()
    {
        RuleFor(command => command.CandidateProfileId).NotEmpty();
        RuleFor(command => command.SelectionRoundId).NotEmpty();
        RuleFor(command => command.Score).InclusiveBetween(1, 10);
    }
}

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

public class UpdateSelectionRoundsCommandValidator : AbstractValidator<UpdateSelectionRoundsCommand>
{
    public UpdateSelectionRoundsCommandValidator()
    {
        RuleFor(command => command.ProcessId).NotEmpty();
        RuleForEach(command => command.Rounds).SetValidator(new SelectionRoundInsertDtoValidator());
        RuleFor(command => command.Rounds)
            .Must(HaveUniqueTitles)
            .WithMessage("Selection round titles must be unique within a recruitment process.");
    }

    private static bool HaveUniqueTitles(IEnumerable<SelectionRoundInsertDto> rounds)
    {
        return rounds
            .Select(round => round.Title?.Trim() ?? string.Empty)
            .Distinct(StringComparer.OrdinalIgnoreCase)
            .Count() == rounds.Count();
    }
}

public class SelectionRoundInsertDtoValidator : AbstractValidator<SelectionRoundInsertDto>
{
    public SelectionRoundInsertDtoValidator()
    {
        RuleFor(round => round.Title).NotEmpty().MaximumLength(200);
        RuleFor(round => round.OrderIndex).GreaterThanOrEqualTo(0);
    }
}
