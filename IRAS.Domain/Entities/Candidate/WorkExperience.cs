// IRAS.Domain/Entities/Candidate/WorkExperience.cs
namespace IRAS.Domain.Entities.Candidate
{
    public class WorkExperience
    {
        public int ExperienceId { get; set; }
        public int CandidateId { get; set; }
        public string CompanyName { get; set; } = null!;
        public string JobTitle { get; set; } = null!;
        public DateTime StartDate { get; set; }
        public DateTime? EndDate { get; set; }
        public bool IsCurrent { get; set; }
        public string? Description { get; set; }

        public CandidateProfile Candidate { get; set; } = null!;
    }
}