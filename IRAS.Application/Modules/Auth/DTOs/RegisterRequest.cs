// IRAS.Application/Modules/Auth/DTOs/RegisterRequest.cs
using System.ComponentModel.DataAnnotations;
using IRAS.Domain.Enums;

namespace IRAS.Application.Modules.Auth.DTOs
{
    public class RegisterRequest : IValidatableObject
    {
        [Required(ErrorMessage = "Email is required.")]
        [EmailAddress(ErrorMessage = "A valid email address is required.")]
        public string Email { get; set; } = null!;

        [Required(ErrorMessage = "Password is required.")]
        [MinLength(8, ErrorMessage = "Password must be at least 8 characters long.")]
        public string Password { get; set; } = null!;

        public UserRole Role { get; set; }          // Candidate or Employer only — Admin created separately

        public string? FirstName { get; set; }        // required if Candidate
        public string? LastName { get; set; }          // required if Candidate
        public string? CompanyName { get; set; }       // required if Employer

        public IEnumerable<ValidationResult> Validate(ValidationContext validationContext)
        {
            if (Role == UserRole.Admin)
                yield return new ValidationResult("Admin accounts cannot be self-registered.", [nameof(Role)]);

            if (Role == UserRole.Candidate)
            {
                if (string.IsNullOrWhiteSpace(FirstName))
                    yield return new ValidationResult("First name is required for candidate accounts.", [nameof(FirstName)]);
                if (string.IsNullOrWhiteSpace(LastName))
                    yield return new ValidationResult("Last name is required for candidate accounts.", [nameof(LastName)]);
            }
            else if (Role == UserRole.Employer)
            {
                if (string.IsNullOrWhiteSpace(CompanyName))
                    yield return new ValidationResult("Company name is required for employer accounts.", [nameof(CompanyName)]);
            }
        }
    }
}
