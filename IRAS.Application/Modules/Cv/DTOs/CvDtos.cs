// IRAS.Application/Modules/Cv/DTOs/CvDtos.cs
using System.ComponentModel.DataAnnotations;

namespace IRAS.Application.Modules.Cv.DTOs
{
    public class CvSummaryDto
    {
        public int CvId { get; set; }
        public string Title { get; set; } = null!;
        public string TemplateName { get; set; } = null!;
        public string? PhotoUrl { get; set; }
        public DateTime UpdatedAt { get; set; }
    }

    // One profile record (an education/experience/certification/skill row) as shown in the
    // CV editor: whether it's currently included on this CV, and its position if so.
    public class CvItemDto
    {
        public int ReferenceId { get; set; }
        public string Label { get; set; } = null!;
        public bool Included { get; set; }
        public int OrderIndex { get; set; }
    }

    public class CvDetailDto
    {
        public int CvId { get; set; }
        public string Title { get; set; } = null!;
        public string TemplateName { get; set; } = null!;
        public string? Summary { get; set; }
        public string? PhotoUrl { get; set; }

        // Pulled live from the candidate's profile (never stored on the CV itself) so a web
        // preview can render the same header the PDF does without a second round trip.
        public string FullName { get; set; } = null!;
        public string? Headline { get; set; }
        public string? Email { get; set; }
        public string? Phone { get; set; }
        public string? GithubUrl { get; set; }
        public string? LinkedInUrl { get; set; }

        public List<string> SectionOrder { get; set; } = new();
        public List<CvItemDto> Education { get; set; } = new();
        public List<CvItemDto> Experience { get; set; } = new();
        public List<CvItemDto> Certifications { get; set; } = new();
        public List<CvItemDto> Skills { get; set; } = new();
        public List<CvItemDto> Languages { get; set; } = new();
        public List<CvItemDto> Projects { get; set; } = new();

        // Fully resolved content — only the currently-included items, in final display
        // order, with full detail (dates, descriptions, etc). This is what a live preview
        // renders; the lists above are for the include/exclude/reorder checklist UI and
        // only carry a flat label. Same resolution the PDF renderer uses.
        public List<CvResolvedEducationDto> ResolvedEducation { get; set; } = new();
        public List<CvResolvedExperienceDto> ResolvedExperience { get; set; } = new();
        public List<CvResolvedCertificationDto> ResolvedCertifications { get; set; } = new();
        public List<string> ResolvedSkills { get; set; } = new();
        public List<CvResolvedLanguageDto> ResolvedLanguages { get; set; } = new();
        public List<CvResolvedProjectDto> ResolvedProjects { get; set; } = new();

        public DateTime CreatedAt { get; set; }
        public DateTime UpdatedAt { get; set; }
    }

    public class CvResolvedEducationDto
    {
        public string Degree { get; set; } = null!;
        public string Institution { get; set; } = null!;
        public string? FieldOfStudy { get; set; }
        public int? StartYear { get; set; }
        public int? EndYear { get; set; }
        public string? Grade { get; set; }
    }

    public class CvResolvedExperienceDto
    {
        public string JobTitle { get; set; } = null!;
        public string CompanyName { get; set; } = null!;
        public DateTime StartDate { get; set; }
        public DateTime? EndDate { get; set; }
        public bool IsCurrent { get; set; }
        public string? Description { get; set; }
    }

    public class CvResolvedCertificationDto
    {
        public string Name { get; set; } = null!;
        public string? IssuingOrg { get; set; }
        public DateTime? IssueDate { get; set; }
    }

    public class CvResolvedLanguageDto
    {
        public string LanguageName { get; set; } = null!;
        public string Proficiency { get; set; } = null!;
    }

    public class CvResolvedProjectDto
    {
        public string Title { get; set; } = null!;
        public string? Description { get; set; }
        public string? ProjectUrl { get; set; }
        public DateTime? StartDate { get; set; }
        public DateTime? EndDate { get; set; }
    }

    public class CreateCvRequest
    {
        [Required, StringLength(150)]
        public string Title { get; set; } = null!;

        [Required]
        public string TemplateName { get; set; } = null!;
    }

    public class UpdateCvRequest
    {
        [Required, StringLength(150)]
        public string Title { get; set; } = null!;

        [Required]
        public string TemplateName { get; set; } = null!;

        [StringLength(2000)]
        public string? Summary { get; set; }

        // Ordered list of CvSectionType names to include, in display order. A section type
        // left out of this list is excluded from the rendered CV entirely.
        [Required, MinLength(1)]
        public List<string> SectionOrder { get; set; } = new();
    }

    // Replaces item selection/order for exactly one reference type at a time (e.g. "these
    // 2 of my 3 work experiences, in this order") — kept separate from UpdateCvRequest since
    // it's edited independently in the UI (drag-and-drop per section) rather than as one form.
    public class UpdateCvSectionItemsRequest
    {
        [Required]
        public string ReferenceType { get; set; } = null!;   // Education | Experience | Certification | Skill

        [Required]
        public List<int> ReferenceIds { get; set; } = new();  // ordered; empty list = include none of this type
    }

    public class CvTemplateDto
    {
        public string Name { get; set; } = null!;
        public string Description { get; set; } = null!;
    }
}
