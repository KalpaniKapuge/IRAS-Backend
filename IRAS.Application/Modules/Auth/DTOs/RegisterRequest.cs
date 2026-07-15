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

        // Sent as a string (like every other request-side enum in this API — see
        // CreateJobRequest.EducationReq, UpdateApplicationStatusRequest.Status, etc.) and
        // parsed with ParseEnum<UserRole>() in AuthService, rather than bound directly as
        // UserRole. Binding it directly would require the raw numeric enum value on the
        // wire, since this API has no global JsonStringEnumConverter configured.
        [Required(ErrorMessage = "Role is required.")]
        public string Role { get; set; } = null!;    // Candidate or Employer only — Admin created separately

        public string? FirstName { get; set; }        // required if Candidate
        public string? LastName { get; set; }          // required if Candidate
        public string? CompanyName { get; set; }       // required if Employer

        public IEnumerable<ValidationResult> Validate(ValidationContext validationContext)
        {
            if (!Enum.TryParse<UserRole>(Role, ignoreCase: true, out var role) || !Enum.IsDefined(role))
            {
                yield return new ValidationResult(
                    $"'{Role}' is not a valid Role. Valid values: {string.Join(", ", Enum.GetNames<UserRole>())}.", [nameof(Role)]);
                yield break;
            }

            if (role == UserRole.Admin)
                yield return new ValidationResult("Admin accounts cannot be self-registered.", [nameof(Role)]);

            if (role == UserRole.Candidate)
            {
                if (string.IsNullOrWhiteSpace(FirstName))
                    yield return new ValidationResult("First name is required for candidate accounts.", [nameof(FirstName)]);
                if (string.IsNullOrWhiteSpace(LastName))
                    yield return new ValidationResult("Last name is required for candidate accounts.", [nameof(LastName)]);
            }
            else if (role == UserRole.Employer)
            {
                if (string.IsNullOrWhiteSpace(CompanyName))
                    yield return new ValidationResult("Company name is required for employer accounts.", [nameof(CompanyName)]);
            }
        }
    }
}
