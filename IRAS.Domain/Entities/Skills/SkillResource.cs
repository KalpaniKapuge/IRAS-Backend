// IRAS.Domain/Entities/Skills/SkillResource.cs
using IRAS.Domain.Enums;
using IRAS.Domain.Entities.Identity;

namespace IRAS.Domain.Entities.Skills
{
    public class SkillResource
    {
        public int ResourceId { get; set; }
        public int SkillId { get; set; }
        public string Title { get; set; } = null!;
        public string Url { get; set; } = null!;
        public SkillResourceType ResourceType { get; set; }
        public string? Provider { get; set; }
        public bool IsActive { get; set; } = true;
        public int CreatedBy { get; set; }
        public DateTime CreatedAt { get; set; } = DateTime.UtcNow;

        public Skill Skill { get; set; } = null!;
        public User CreatedByUser { get; set; } = null!;
    }
}
