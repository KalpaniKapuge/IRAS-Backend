// IRAS.Application/Modules/SkillDevelopment/DTOs/SkillDevelopmentDtos.cs
using System.ComponentModel.DataAnnotations;

namespace IRAS.Application.Modules.SkillDevelopment.DTOs
{
    public class SkillResourceDto
    {
        public int ResourceId { get; set; }
        public int SkillId { get; set; }
        public string SkillName { get; set; } = null!;
        public string Title { get; set; } = null!;
        public string Url { get; set; } = null!;
        public string ResourceType { get; set; } = null!;
        public string? Provider { get; set; }
        public bool IsActive { get; set; }
        public DateTime CreatedAt { get; set; }
    }

    public class UpsertSkillResourceRequest
    {
        [Required]
        public int SkillId { get; set; }

        [Required, StringLength(200)]
        public string Title { get; set; } = null!;

        [Required, StringLength(500)]
        public string Url { get; set; } = null!;

        [Required]
        public string ResourceType { get; set; } = null!;   // Course | Tutorial | Project | Documentation

        [StringLength(100)]
        public string? Provider { get; set; }

        public bool IsActive { get; set; } = true;
    }

    // A skill the candidate has chosen to work on after seeing it flagged as a gap.
    public class TargetSkillDto
    {
        public int SkillId { get; set; }
        public string SkillName { get; set; } = null!;
        public string Status { get; set; } = null!;   // Learning | Completed
        public DateTime AddedAt { get; set; }
        public DateTime? CompletedAt { get; set; }
    }

    public class AddTargetSkillRequest
    {
        [Required]
        public int SkillId { get; set; }
    }
}
