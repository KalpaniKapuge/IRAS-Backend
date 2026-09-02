// IRAS.Domain/Entities/Identity/User.cs
using IRAS.Domain.Enums;
using IRAS.Domain.Entities.Candidate;
using IRAS.Domain.Entities.Employer;

namespace IRAS.Domain.Entities.Identity
{
    public class User
    {
        public int UserId { get; set; }
        public string Email { get; set; } = null!;

        // Null for accounts created via an external identity provider (e.g. Google) that
        // have never set a local password. Local email/password accounts always have one.
        public string? PasswordHash { get; set; }

        // "Local" for email/password sign-up, "Google" for Google Sign-In. Lets the login
        // path give a clear error when someone tries a password on a social-only account.
        public string AuthProvider { get; set; } = "Local";

        public UserRole Role { get; set; }
        public bool IsActive { get; set; } = true;
        public DateTime? LastLogin { get; set; }
        public DateTime CreatedAt { get; set; } = DateTime.UtcNow;

        // Navigation — one-to-one style depending on role
        public CandidateProfile? CandidateProfile { get; set; }
        public EmployerProfile? EmployerProfile { get; set; }
    }
}