// IRAS.Application/Modules/Candidates/DTOs/CandidateProfileDto.cs
using System.ComponentModel.DataAnnotations;

namespace IRAS.Application.Modules.Candidates.DTOs
{
    public class CandidateProfileDto
    {
        public int CandidateId { get; set; }
        public string FirstName { get; set; } = null!;
        public string LastName { get; set; } = null!;
        public string? Citizenship { get; set; }
        public string? Phone { get; set; }
        public string? Headline { get; set; }
        public decimal TotalExpYears { get; set; }
        public string EducationLevel { get; set; } = null!;
        public bool OptInMatching { get; set; }
        public List<EducationDto> Educations { get; set; } = new();
        public List<WorkExperienceDto> WorkExperiences { get; set; } = new();
        public List<CertificationDto> Certifications { get; set; } = new();
        public List<CandidateSkillDto> Skills { get; set; } = new();
    }

    public class UpdateCandidateProfileRequest
    {
        [Required, StringLength(100)]
        public string FirstName { get; set; } = null!;

        [Required, StringLength(100)]
        public string LastName { get; set; } = null!;

        [StringLength(100)]
        public string? Citizenship { get; set; }

        [Phone]
        public string? Phone { get; set; }

        [StringLength(200)]
        public string? Headline { get; set; }

        [Required]
        public string EducationLevel { get; set; } = null!;

        public bool OptInMatching { get; set; }
    }

    public class EducationDto
    {
        public int EducationId { get; set; }

        [Required, StringLength(150)]
        public string Degree { get; set; } = null!;

        [Required, StringLength(200)]
        public string Institution { get; set; } = null!;

        [StringLength(150)]
        public string? FieldOfStudy { get; set; }

        [Range(1950, 2100)]
        public int? StartYear { get; set; }

        [Range(1950, 2100)]
        public int? EndYear { get; set; }

        [StringLength(20)]
        public string? Grade { get; set; }
    }

    public class WorkExperienceDto
    {
        public int ExperienceId { get; set; }

        [Required, StringLength(150)]
        public string CompanyName { get; set; } = null!;

        [Required, StringLength(150)]
        public string JobTitle { get; set; } = null!;

        [Required]
        public DateTime StartDate { get; set; }

        public DateTime? EndDate { get; set; }
        public bool IsCurrent { get; set; }

        [StringLength(2000)]
        public string? Description { get; set; }
    }

    public class CertificationDto
    {
        public int CertificationId { get; set; }

        [Required, StringLength(200)]
        public string Name { get; set; } = null!;

        [StringLength(150)]
        public string? IssuingOrg { get; set; }

        public DateTime? IssueDate { get; set; }
        public DateTime? ExpiryDate { get; set; }
    }

    public class CandidateSkillDto
    {
        public int SkillId { get; set; }
        public string SkillName { get; set; } = null!;
        public string Proficiency { get; set; } = null!;
        public decimal YearsExp { get; set; }
        public string Source { get; set; } = null!;
        public bool IsVerified { get; set; }
    }

    public class UpsertCandidateSkillRequest
    {
        [Required]
        public int SkillId { get; set; }

        [Required]
        public string Proficiency { get; set; } = null!;

        [Range(0, 50)]
        public decimal YearsExp { get; set; }
    }
}