// IRAS.Domain/Entities/Jobs/JobRequiredSkill.cs
using IRAS.Domain.Enums;
using IRAS.Domain.Entities.Skills;

namespace IRAS.Domain.Entities.Jobs
{
    public class JobRequiredSkill
    {
        public int JobId { get; set; }
        public int SkillId { get; set; }
        public ImportanceLevel Importance { get; set; }
        public decimal Weight { get; set; }
        public int MinYears { get; set; }

        public Job Job { get; set; } = null!;
        public Skill Skill { get; set; } = null!;
    }
}