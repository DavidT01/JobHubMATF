using System.ComponentModel.DataAnnotations;

namespace Identity.API.Models
{
    public class RegisterDto : IValidatableObject
    {
        public string? FirstName { get; set; }

        public string? LastName { get; set; }

        [Required(ErrorMessage = "Email is required.")]
        [EmailAddress(ErrorMessage = "Invalid email format.")]
        public string? Email { get; set; }

        [Required(ErrorMessage = "Password is required.")]
        [MinLength(6, ErrorMessage = "Password must be at least 6 characters.")]
        public string? Password { get; set; }

        [Required(ErrorMessage = "Role is required.")]
        public string? Role { get; set; }

        public IEnumerable<ValidationResult> Validate(ValidationContext validationContext)
        {
            if (string.Equals(Role, AppRoles.Candidate, StringComparison.OrdinalIgnoreCase))
            {
                if (string.IsNullOrWhiteSpace(FirstName))
                {
                    yield return new ValidationResult("First name is required.", [nameof(FirstName)]);
                }

                if (string.IsNullOrWhiteSpace(LastName))
                {
                    yield return new ValidationResult("Last name is required.", [nameof(LastName)]);
                }
            }
            else if (string.Equals(Role, AppRoles.Employer, StringComparison.OrdinalIgnoreCase))
            {
                if (string.IsNullOrWhiteSpace(FirstName))
                {
                    yield return new ValidationResult("Company name is required.", [nameof(FirstName)]);
                }
            }
        }
    }
}
