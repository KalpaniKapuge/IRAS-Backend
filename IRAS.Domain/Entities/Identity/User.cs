// IRAS.Domain/Entities/Identity/User.cs
using IRAS.Domain.Enums;

namespace IRAS.Domain.Entities.Identity
{
    public class User
    {
        public int UserId { get; set; }
        public string Email { get; set; } = null!;
        public string PasswordHash { get; set; } = null!;
        public UserRole Role { get; set; }
        public bool IsActive { get; set; } = true;
        public DateTime? LastLogin { get; set; }
        public DateTime CreatedAt { get; set; } = DateTime.UtcNow;

        // Navigation — one-to-one style depending on role
        public CandidateProfile? CandidateProfile { get; set; }
        public EmployerProfile? EmployerProfile { get; set; }
    }
}