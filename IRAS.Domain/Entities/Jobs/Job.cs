// IRAS.Domain/Entities/Jobs/Job.cs
using IRAS.Domain.Enums;
using IRAS.Domain.Entities.Employer;

namespace IRAS.Domain.Entities.Jobs
{
    public class Job
    {
        public int JobId { get; set; }
        public int EmployerId { get; set; }
        public string Title { get; set; } = null!;
        public string SeniorityLevel { get; set; } = null!;
        public string? RequirementInput { get; set; }     // correction #3 — raw employer input
        public string? GeneratedJd { get; set; }
        public bool IsAiGenerated { get; set; }
        public int MinExpYears { get; set; }
        public EducationLevel EducationReq { get; set; }
        public EmploymentType EmploymentType { get; set; }
        public string? Location { get; set; }
        public JobStatus Status { get; set; } = JobStatus.Draft;
        public DateTime? PostedAt { get; set; }
        public DateTime? ClosingDate { get; set; }

        public EmployerProfile Employer { get; set; } = null!;
        public ICollection<JobRequiredSkill> RequiredSkills { get; set; } = new List<JobRequiredSkill>();
    }
}