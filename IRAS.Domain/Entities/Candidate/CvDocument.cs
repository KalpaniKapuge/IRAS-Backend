// IRAS.Domain/Entities/Candidate/CvDocument.cs
namespace IRAS.Domain.Entities.Candidate
{
    // A candidate's saved, customized CV. Content is never duplicated from the profile —
    // CvSectionItem rows just reference which existing Education/WorkExperience/
    // Certification/CandidateSkill entries to include and in what order, so editing a
    // candidate's profile automatically keeps every CV that references it up to date.
    public class CvDocument
    {
        public int CvId { get; set; }
        public int CandidateId { get; set; }
        public string Title { get; set; } = null!;
        public string TemplateName { get; set; } = null!;   // "Classic" | "Modern" | "Compact"
        public string? Summary { get; set; }

        // Comma-separated CvSectionType names in display order — a section type omitted
        // here is excluded from this CV entirely. Kept as a simple ordered string rather
        // than a child table since the section set is small and fixed (five values), unlike
        // the open-ended per-item selection below.
        public string SectionOrder { get; set; } = "Summary,Skills,Experience,Education,Certifications";

        // Comma-separated CvReferenceType names that have had UpdateSectionItemsAsync called
        // for them at least once. Distinguishes "never customized — include everything" from
        // "explicitly customized to an empty selection — include nothing", since both states
        // otherwise look identical as zero CvSectionItem rows.
        public string CustomizedReferenceTypes { get; set; } = "";

        public DateTime CreatedAt { get; set; } = DateTime.UtcNow;
        public DateTime UpdatedAt { get; set; } = DateTime.UtcNow;

        public CandidateProfile Candidate { get; set; } = null!;
        public ICollection<CvSectionItem> Items { get; set; } = new List<CvSectionItem>();
    }
}
