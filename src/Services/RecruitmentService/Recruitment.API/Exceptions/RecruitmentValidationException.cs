using FluentValidation.Results;

namespace Recruitment.API.Exceptions
{
    public class RecruitmentValidationException(IEnumerable<ValidationFailure> errors) : Exception("Validation failed.")
    {
        public IEnumerable<ValidationFailure> Errors { get; } = errors;
    }
}
