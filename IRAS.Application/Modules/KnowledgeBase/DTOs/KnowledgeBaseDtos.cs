// IRAS.Application/Modules/KnowledgeBase/DTOs/KnowledgeBaseDtos.cs
using System.ComponentModel.DataAnnotations;

namespace IRAS.Application.Modules.KnowledgeBase.DTOs
{
    public class KnowledgeBaseDto
    {
        public int KbId { get; set; }
        public string Title { get; set; } = null!;
        public string Content { get; set; } = null!;
        public string Category { get; set; } = null!;
        public bool IsActive { get; set; }
        public DateTime UpdatedAt { get; set; }
    }

    public class UpsertKnowledgeBaseRequest
    {
        [Required, StringLength(200)]
        public string Title { get; set; } = null!;

        [Required, StringLength(2000)]
        public string Content { get; set; } = null!;

        [Required]
        public string Category { get; set; } = null!;   // FAQ | PolicyGuideline | SkillAdvice | PlatformHowTo

        public bool IsActive { get; set; } = true;
    }
}
