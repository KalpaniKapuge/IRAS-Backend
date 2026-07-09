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
