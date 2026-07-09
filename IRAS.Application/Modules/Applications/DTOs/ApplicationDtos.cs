// IRAS.Application/Modules/Applications/DTOs/ApplicationDtos.cs
using System.ComponentModel.DataAnnotations;

namespace IRAS.Application.Modules.Applications.DTOs
{
    public class ApplyForJobRequest
    {
        [Required]
        public int JobId { get; set; }

        [Required]
        public int ResumeId { get; set; }
    }

    public class SkillGapDto
    {
        public int SkillId { get; set; }
        public string SkillName { get; set; } = null!;
        public string Importance { get; set; } = null!;
        public string? Suggestion { get; set; }
    }

    public class ApplicationDto
    {
        public int ApplicationId { get; set; }
        public int JobId { get; set; }
        public string JobTitle { get; set; } = null!;
        public string? CompanyName { get; set; }
        public string Status { get; set; } = null!;
        public decimal TotalScore { get; set; }
        public decimal SkillMatch { get; set; }
        public decimal ExperienceMatch { get; set; }
        public decimal EducationMatch { get; set; }
        public decimal SemanticSimilarity { get; set; }
        public DateTime AppliedAt { get; set; }
        public List<SkillGapDto> SkillGaps { get; set; } = new();
    }

    public class RankedApplicantDto
    {
        public int ApplicationId { get; set; }
        public int CandidateId { get; set; }
        public string CandidateName { get; set; } = null!;
        public string Status { get; set; } = null!;
        public decimal TotalScore { get; set; }
        public decimal SkillMatch { get; set; }
        public decimal ExperienceMatch { get; set; }
        public decimal EducationMatch { get; set; }
        public decimal SemanticSimilarity { get; set; }
        public DateTime AppliedAt { get; set; }
        public List<SkillGapDto> SkillGaps { get; set; } = new();
    }
}
