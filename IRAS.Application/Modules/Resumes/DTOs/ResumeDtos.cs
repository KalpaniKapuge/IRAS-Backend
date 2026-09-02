// IRAS.Application/Modules/Resumes/DTOs/ResumeDtos.cs
using System.ComponentModel.DataAnnotations;

namespace IRAS.Application.Modules.Resumes.DTOs
{
    public class ResumeDto
    {
        public int ResumeId { get; set; }
        public string FileFormat { get; set; } = null!;
        public bool IsPrimary { get; set; }
        public string ParseStatus { get; set; } = null!;
        public string? ParseError { get; set; }
        public DateTime UploadedAt { get; set; }

        // Directly fetchable public URL (both LocalDiskFileStorage and SupabaseFileStorage
        // return one, not a bare filesystem/storage path) — lets the UI open/preview the
        // actual resume file, not just show its metadata.
        public string FileUrl { get; set; } = null!;

        // The original uploaded file name, or a CV-derived name for resumes generated from
        // the CV builder. Always prefer this over a generic "{FileFormat} Resume" label so
        // several resumes are actually distinguishable in a list.
        public string? FileName { get; set; }

        // Non-null only when this resume was generated from a CV-builder CV — lets the UI
        // show "My Software Engineer CV" instead of a generic "PDF Resume" label, and (via
        // SourceCvId) open the CV's *current* rendering live instead of this resume's frozen
        // snapshot file. Both null when it was a direct upload or when the source CV was
        // since deleted.
        public int? SourceCvId { get; set; }
        public string? SourceCvTitle { get; set; }
    }

    public class ParseResultDto
    {
        public int ResumeId { get; set; }
        public string ParseStatus { get; set; } = null!;
        public string? ParseError { get; set; }
        public List<SuggestedSkillDto> SuggestedSkills { get; set; } = new();
        public List<string> DetectedEmails { get; set; } = new();
        public List<string> DetectedPhones { get; set; } = new();
    }

    public class SuggestedSkillDto
    {
        public int SkillId { get; set; }
        public string SkillName { get; set; } = null!;
        public string MatchedText { get; set; } = null!;
        public int Occurrences { get; set; }
        public bool AlreadyOnProfile { get; set; }
    }

    public class ConfirmSkillsRequest
    {
        // Only skills the candidate ticked in the confirmation UI
        [MinLength(1, ErrorMessage = "Select at least one skill to confirm.")]
        public List<int> SkillIds { get; set; } = new();
    }
}
