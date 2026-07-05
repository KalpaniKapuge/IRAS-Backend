// IRAS.Domain/Entities/Applications/SkillGap.cs
using IRAS.Domain.Enums;
using IRAS.Domain.Entities.Skills;

namespace IRAS.Domain.Entities.Applications
{
    public class SkillGap
    {
        public int GapId { get; set; }
        public int ApplicationId { get; set; }
        public int SkillId { get; set; }
        public ImportanceLevel Importance { get; set; }
        public string? Suggestion { get; set; }
        public DateTime DetectedAt { get; set; } = DateTime.UtcNow;

        public Application Application { get; set; } = null!;
        public Skill Skill { get; set; } = null!;
    }
}