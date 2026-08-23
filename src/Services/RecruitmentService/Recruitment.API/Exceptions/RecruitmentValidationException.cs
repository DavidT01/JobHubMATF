using FluentValidation.Results;

namespace Recruitment.API.Exceptions
{
    public class RecruitmentValidationException : Exception
    {
        public IEnumerable<ValidationFailure> Errors { get; }

        public RecruitmentValidationException(IEnumerable<ValidationFailure> errors) : base("Validation failed.")
        {
            Errors = errors;
        }

        public RecruitmentValidationException(string message) : base(message)
        {
            Errors = [new ValidationFailure(string.Empty, message)];
        }
    }
}
