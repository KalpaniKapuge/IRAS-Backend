// IRAS.Domain/Entities/Skills/Skill.cs
using IRAS.Domain.Enums;

namespace IRAS.Domain.Entities.Skills
{
    public class Skill
    {
        public int SkillId { get; set; }
        public string SkillName { get; set; } = null!;
        public SkillCategory Category { get; set; }
        public string? Description { get; set; }

        public ICollection<SkillAlias> Aliases { get; set; } = new List<SkillAlias>();
    }
}