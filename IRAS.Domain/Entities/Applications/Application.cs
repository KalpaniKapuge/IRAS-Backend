// IRAS.Domain/Entities/Applications/Application.cs
using IRAS.Domain.Enums;
using IRAS.Domain.Entities.Candidate;
using IRAS.Domain.Entities.Jobs;

namespace IRAS.Domain.Entities.Applications
{
    public class Application
    {
        public int ApplicationId { get; set; }
        public int CandidateId { get; set; }
        public int JobId { get; set; }
        public int ResumeId { get; set; }
        public ApplicationStatus Status { get; set; } = ApplicationStatus.Applied;

        public decimal TotalScore { get; set; }
        public decimal SkillMatch { get; set; }
        public decimal ExperienceMatch { get; set; }
        public decimal EducationMatch { get; set; }
        public decimal SemanticSimilarity { get; set; }

        public DateTime AppliedAt { get; set; } = DateTime.UtcNow;
        public DateTime? UpdatedAt { get; set; }

        public CandidateProfile Candidate { get; set; } = null!;
        public Job Job { get; set; } = null!;
        public Resume Resume { get; set; } = null!;
        public ICollection<SkillGap> SkillGaps { get; set; } = new List<SkillGap>();
        public ICollection<ApplicationStatusHistory> StatusHistory { get; set; } = new List<ApplicationStatusHistory>();
        public Feedback.Feedback? Feedback { get; set; }
    }
}