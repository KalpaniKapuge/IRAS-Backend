// IRAS.Application/Modules/Cv/ICvPdfRenderer.cs
using IRAS.Application.Modules.Cv.DTOs;

namespace IRAS.Application.Modules.Cv
{
    // Fully resolved CV content, independent of the database — CvService builds this from
    // the candidate's live profile data plus the CvDocument's customization (summary,
    // section order, chosen/ordered items), and hands it to the renderer. Keeping this
    // separate from the EF entities means template code never touches the DbContext.
    // Reuses the same CvResolved*Dto shapes the API returns for the web preview (built by
    // the same resolution helper in CvService), rather than a parallel set of record types.
    public class RenderedCvData
    {
        public string FullName { get; set; } = null!;
        public string? Headline { get; set; }
        public string? Email { get; set; }
        public string? Phone { get; set; }
        public string? GithubUrl { get; set; }
        public string? LinkedInUrl { get; set; }
        public string? Summary { get; set; }
        public byte[]? PhotoBytes { get; set; }
        public List<string> SectionOrder { get; set; } = new();
        public List<CvResolvedEducationDto> Education { get; set; } = new();
        public List<CvResolvedExperienceDto> Experience { get; set; } = new();
        public List<CvResolvedCertificationDto> Certifications { get; set; } = new();
        public List<string> Skills { get; set; } = new();
        public List<CvResolvedLanguageDto> Languages { get; set; } = new();
        public List<CvResolvedProjectDto> Projects { get; set; } = new();
    }

    public interface ICvPdfRenderer
    {
        // TemplateName selects which visual layout to use ("Classic" | "Modern" | "Compact");
        // an unrecognized name falls back to "Classic" rather than throwing, since a template
        // becoming unavailable should never block a candidate from downloading their CV.
        byte[] Render(string templateName, RenderedCvData data);
    }
}
