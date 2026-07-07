// IRAS.Application/Modules/SkillTaxonomy/DTOs/SkillDtos.cs
using System.ComponentModel.DataAnnotations;

namespace IRAS.Application.Modules.SkillTaxonomy.DTOs
{
    public class SkillDto
    {
        public int SkillId { get; set; }
        public string SkillName { get; set; } = null!;
        public string Category { get; set; } = null!;
        public string? Description { get; set; }
        public List<SkillAliasDto> Aliases { get; set; } = new();
    }

    public class SkillAliasDto
    {
        public int AliasId { get; set; }
        public string AliasText { get; set; } = null!;
        public string Source { get; set; } = null!;
    }

    public class CreateSkillRequest
    {
        [Required, StringLength(100)]
        public string SkillName { get; set; } = null!;

        [Required]
        public string Category { get; set; } = null!;

        [StringLength(500)]
        public string? Description { get; set; }

        // optional aliases created together with the skill
        public List<string> Aliases { get; set; } = new();
    }

    public class UpdateSkillRequest
    {
        [Required, StringLength(100)]
        public string SkillName { get; set; } = null!;

        [Required]
        public string Category { get; set; } = null!;

        [StringLength(500)]
        public string? Description { get; set; }
    }

    public class AddAliasRequest
    {
        [Required, StringLength(100)]
        public string AliasText { get; set; } = null!;
    }

    // Result of normalization lookup: "JS" -> JavaScript (skillId 12)
    public class SkillResolveResult
    {
        public bool Found { get; set; }
        public int? SkillId { get; set; }
        public string? SkillName { get; set; }
        public string MatchedBy { get; set; } = "none"; // "name" | "alias" | "none"
    }

    public class PagedResult<T>
    {
        public List<T> Items { get; set; } = new();
        public int TotalCount { get; set; }
        public int Page { get; set; }
        public int PageSize { get; set; }
    }
}