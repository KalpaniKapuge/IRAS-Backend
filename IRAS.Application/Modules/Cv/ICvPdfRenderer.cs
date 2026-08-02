// IRAS.Application/Modules/Cv/ICvPdfRenderer.cs
namespace IRAS.Application.Modules.Cv
{
    public record RenderedEducation(string Degree, string Institution, string? FieldOfStudy, int? StartYear, int? EndYear, string? Grade);
    public record RenderedExperience(string JobTitle, string CompanyName, DateTime StartDate, DateTime? EndDate, bool IsCurrent, string? Description);
    public record RenderedCertification(string Name, string? IssuingOrg, DateTime? IssueDate);

    // Fully resolved CV content, independent of the database — CvService builds this from
    // the candidate's live profile data plus the CvDocument's customization (summary,
    // section order, chosen/ordered items), and hands it to the renderer. Keeping this
    // separate from the EF entities means template code never touches the DbContext.
    public class RenderedCvData
    {
        public string FullName { get; set; } = null!;
        public string? Headline { get; set; }
        public string? Email { get; set; }
        public string? Phone { get; set; }
        public string? GithubUrl { get; set; }
        public string? LinkedInUrl { get; set; }
        public string? Summary { get; set; }
        public List<string> SectionOrder { get; set; } = new();
        public List<RenderedEducation> Education { get; set; } = new();
        public List<RenderedExperience> Experience { get; set; } = new();
        public List<RenderedCertification> Certifications { get; set; } = new();
        public List<string> Skills { get; set; } = new();
    }

    public interface ICvPdfRenderer
    {
        // TemplateName selects which visual layout to use ("Classic" | "Modern" | "Compact");
        // an unrecognized name falls back to "Classic" rather than throwing, since a template
        // becoming unavailable should never block a candidate from downloading their CV.
        byte[] Render(string templateName, RenderedCvData data);
    }
}
