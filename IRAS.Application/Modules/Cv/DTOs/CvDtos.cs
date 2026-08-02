// IRAS.Application/Modules/Cv/DTOs/CvDtos.cs
using System.ComponentModel.DataAnnotations;

namespace IRAS.Application.Modules.Cv.DTOs
{
    public class CvSummaryDto
    {
        public int CvId { get; set; }
        public string Title { get; set; } = null!;
        public string TemplateName { get; set; } = null!;
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
        public List<string> SectionOrder { get; set; } = new();
        public List<CvItemDto> Education { get; set; } = new();
        public List<CvItemDto> Experience { get; set; } = new();
        public List<CvItemDto> Certifications { get; set; } = new();
        public List<CvItemDto> Skills { get; set; } = new();
        public DateTime CreatedAt { get; set; }
        public DateTime UpdatedAt { get; set; }
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
