// IRAS.Domain/Entities/Candidate/CandidateProfile.cs
using IRAS.Domain.Enums;
using IRAS.Domain.Entities.Identity;

namespace IRAS.Domain.Entities.Candidate
{
    public class CandidateProfile
    {
        public int CandidateId { get; set; } // PK and FK to User
        public string FirstName { get; set; } = null!;
        public string LastName { get; set; } = null!;
        public string? Citizenship { get; set; }
        public string? Phone { get; set; }
        public string? Headline { get; set; }
        public decimal TotalExpYears { get; set; }
        public EducationLevel EducationLevel { get; set; }
        public bool OptInMatching { get; set; } = true;
        public DateTime CreatedAt { get; set; } = DateTime.UtcNow;

        public User User { get; set; } = null!;
        public ICollection<Resume> Resumes { get; set; } = new List<Resume>();
        public ICollection<Education> Educations { get; set; } = new List<Education>();
        public ICollection<WorkExperience> WorkExperiences { get; set; } = new List<WorkExperience>();
        public ICollection<Certification> Certifications { get; set; } = new List<Certification>();
        public ICollection<CandidateSkill> CandidateSkills { get; set; } = new List<CandidateSkill>();
    }
}