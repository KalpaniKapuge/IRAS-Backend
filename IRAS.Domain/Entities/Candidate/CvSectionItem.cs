// IRAS.Domain/Entities/Candidate/CvSectionItem.cs
using IRAS.Domain.Enums;

namespace IRAS.Domain.Entities.Candidate
{
    // One chosen profile record (an Education, WorkExperience, Certification, or
    // CandidateSkill row) included in a specific CvDocument, with its display order within
    // that section. If a CvDocument has no CvSectionItem rows for a given ReferenceType,
    // CvService treats that as "include everything of that type" — so a freshly created CV
    // defaults to the full profile without needing rows seeded for every item up front.
    public class CvSectionItem
    {
        public int CvSectionItemId { get; set; }
        public int CvId { get; set; }
        public CvReferenceType ReferenceType { get; set; }
        public int ReferenceId { get; set; }
        public int OrderIndex { get; set; }

        public CvDocument Cv { get; set; } = null!;
    }
}
