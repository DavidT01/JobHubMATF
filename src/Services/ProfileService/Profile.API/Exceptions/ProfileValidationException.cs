using FluentValidation.Results;
using System.ComponentModel.DataAnnotations;

namespace Profile.API.Exceptions
{
    public class ProfileValidationException : Exception
    {
        public ProfileValidationException() : base("One or more profile validation failures have occurred.")
        {
            Errors = new Dictionary<string, string[]>();
        }

        public ProfileValidationException(IEnumerable<ValidationFailure> failures) : this()
        {
            Errors = failures
                .GroupBy(e => e.PropertyName, e => e.ErrorMessage)
                .ToDictionary(failureGroup => failureGroup.Key, failureGroup => failureGroup.ToArray());
        }

        public IDictionary<string, string[]> Errors { get; set; }
    }
}
