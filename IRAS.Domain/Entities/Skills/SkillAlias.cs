// IRAS.Domain/Entities/Skills/SkillAlias.cs
using IRAS.Domain.Enums;

namespace IRAS.Domain.Entities.Skills
{
    public class SkillAlias
    {
        public int AliasId { get; set; }
        public int SkillId { get; set; }
        public string AliasText { get; set; } = null!;   // e.g. "JS" -> JavaScript
        public AliasSource Source { get; set; }

        public Skill Skill { get; set; } = null!;
    }
}