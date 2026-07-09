// IRAS.Application/Modules/SkillGaps/DTOs/SkillGapDtos.cs
namespace IRAS.Application.Modules.SkillGaps.DTOs
{
    // One gap on one application — the detailed, per-application view.
    public class CandidateSkillGapDto
    {
        public int SkillId { get; set; }
        public string SkillName { get; set; } = null!;
        public string Importance { get; set; } = null!;
        public string? Suggestion { get; set; }
        public int JobId { get; set; }
        public string JobTitle { get; set; } = null!;
        public string? CompanyName { get; set; }
        public DateTime DetectedAt { get; set; }
    }

    // Same skill rolled up across every application it appeared as a gap in — answers
    // "what should I actually go learn" better than a flat per-application list does.
    public class SkillGapSummaryDto
    {
        public int SkillId { get; set; }
        public string SkillName { get; set; } = null!;
        public int MustHaveCount { get; set; }
        public int NiceToHaveCount { get; set; }
        public int TotalOccurrences { get; set; }
    }
}
