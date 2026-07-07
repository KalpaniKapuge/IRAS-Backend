// IRAS.Application/Modules/Jobs/DTOs/JobDtos.cs
using System.ComponentModel.DataAnnotations;

namespace IRAS.Application.Modules.Jobs.DTOs
{
    public class EmployerProfileDto
    {
        public int EmployerId { get; set; }
        public string CompanyName { get; set; } = null!;
        public string? Industry { get; set; }
        public string CompanySize { get; set; } = null!;
        public string? Website { get; set; }
        public string? Location { get; set; }
        public string? Description { get; set; }
    }

    public class UpdateEmployerProfileRequest
    {
        [Required, StringLength(150)]
        public string CompanyName { get; set; } = null!;

        [StringLength(100)]
        public string? Industry { get; set; }

        [Required]
        public string CompanySize { get; set; } = null!;

        [Url, StringLength(300)]
        public string? Website { get; set; }

        [StringLength(150)]
        public string? Location { get; set; }

        [StringLength(2000)]
        public string? Description { get; set; }
    }

    public class JobRequiredSkillDto
    {
        [Required]
        public int SkillId { get; set; }
        public string? SkillName { get; set; }          // filled on read

        [Required]
        public string Importance { get; set; } = null!; // MustHave | NiceToHave

        [Range(0, 1)]
        public decimal? Weight { get; set; }            // optional; defaulted by importance

        [Range(0, 30)]
        public int MinYears { get; set; }
    }

    public class CreateJobRequest
    {
        [Required, StringLength(150)]
        public string Title { get; set; } = null!;

        [Required, StringLength(50)]
        public string SeniorityLevel { get; set; } = null!;

        [Range(0, 30)]
        public int MinExpYears { get; set; }

        [Required]
        public string EducationReq { get; set; } = null!;

        [Required]
        public string EmploymentType { get; set; } = null!;

        [StringLength(150)]
        public string? Location { get; set; }

        public DateTime? ClosingDate { get; set; }

        [MinLength(1, ErrorMessage = "At least one required skill is needed.")]
        public List<JobRequiredSkillDto> RequiredSkills { get; set; } = new();
    }

    public class GenerateJdRequest
    {
        // free-text notes from the employer, e.g. "team of 5, fintech product, hybrid 2 days"
        [StringLength(2000)]
        public string? AdditionalNotes { get; set; }
    }

    public class GenerateJdResponse
    {
        public string GeneratedJd { get; set; } = null!;
        public bool IsAiGenerated { get; set; }
        public string GeneratorUsed { get; set; } = null!;   // "Template" | "LLM"
    }

    public class UpdateJdRequest
    {
        [Required, StringLength(20000)]
        public string JdText { get; set; } = null!;
    }

    public class JobDto
    {
        public int JobId { get; set; }
        public int EmployerId { get; set; }
        public string? CompanyName { get; set; }
        public string Title { get; set; } = null!;
        public string SeniorityLevel { get; set; } = null!;
        public string? RequirementInput { get; set; }
        public string? GeneratedJd { get; set; }
        public bool IsAiGenerated { get; set; }
        public int MinExpYears { get; set; }
        public string EducationReq { get; set; } = null!;
        public string EmploymentType { get; set; } = null!;
        public string? Location { get; set; }
        public string Status { get; set; } = null!;
        public DateTime? PostedAt { get; set; }
        public DateTime? ClosingDate { get; set; }
        public List<JobRequiredSkillDto> RequiredSkills { get; set; } = new();
    }

    public class JobSummaryDto
    {
        public int JobId { get; set; }
        public string Title { get; set; } = null!;
        public string? CompanyName { get; set; }
        public string SeniorityLevel { get; set; } = null!;
        public string EmploymentType { get; set; } = null!;
        public string? Location { get; set; }
        public string Status { get; set; } = null!;
        public DateTime? PostedAt { get; set; }
        public DateTime? ClosingDate { get; set; }
        public int RequiredSkillCount { get; set; }
    }
}