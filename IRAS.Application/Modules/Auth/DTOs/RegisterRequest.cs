// IRAS.Application/Modules/Auth/DTOs/RegisterRequest.cs
using IRAS.Domain.Enums;

namespace IRAS.Application.Modules.Auth.DTOs
{
    public class RegisterRequest
    {
        public string Email { get; set; } = null!;
        public string Password { get; set; } = null!;
        public UserRole Role { get; set; }          // Candidate or Employer only — Admin created separately
        public string FirstName { get; set; } = null!;   // used if Candidate
        public string LastName { get; set; } = null!;    // used if Candidate
        public string? CompanyName { get; set; }          // used if Employer
    }
}