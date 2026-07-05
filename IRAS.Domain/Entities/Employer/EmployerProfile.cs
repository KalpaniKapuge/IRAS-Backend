// IRAS.Domain/Entities/Employer/EmployerProfile.cs
using IRAS.Domain.Enums;
using IRAS.Domain.Entities.Identity;
using IRAS.Domain.Entities.Jobs;

namespace IRAS.Domain.Entities.Employer
{
    public class EmployerProfile
    {
        public int EmployerId { get; set; } // PK and FK to User
        public string CompanyName { get; set; } = null!;
        public string? Industry { get; set; }
        public CompanySize CompanySize { get; set; }
        public string? Website { get; set; }
        public string? Location { get; set; }
        public string? Description { get; set; }
        public DateTime CreatedAt { get; set; } = DateTime.UtcNow;

        public User User { get; set; } = null!;
        public ICollection<Job> Jobs { get; set; } = new List<Job>();
    }
}